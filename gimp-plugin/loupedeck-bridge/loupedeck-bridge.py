#!/usr/bin/env python3
# SPDX-License-Identifier: MIT
"""Loupedeck bridge for GIMP 3.

A persistent plug-in that GIMP starts at launch. It lets the Loupedeck plugin read and set GIMP context
values (brush size, opacity, ...) over TCP on 127.0.0.1. It does not change any GIMP setting or file.

Security (local IPC, same user = trust boundary):
- Listens on 127.0.0.1 only.
- On every start it writes a fresh random token to a file only the user can read (see token_path()).
  A client must send it as first line; anything else (e.g. a browser's HTTP request) closes the connection.
- Nothing is sent before authentication; lines are bounded, connections limited, auth must come within 2 s.

Protocol (ASCII lines ending in "\n", at most MAX_LINE bytes):
  C: lgb1 <token>
  then per request:  "<name> ?|reset|<signed steps>"  ->  "<name> <value>"  (value in display units)
"""

import hmac
import os
import secrets
import struct
import sys
import tempfile

import gi
gi.require_version('Gimp', '3.0')
gi.require_version('Gegl', '0.4')
gi.require_version('Babl', '0.1')
from gi.repository import Babl, Gegl, Gimp, GLib, Gio

PROC_NAME = 'extension-loupedeck-bridge'
HOST = '127.0.0.1'
PORT = 47862
PROTOCOL = 'lgb1'

MAX_LINE = 128
MAX_CLIENTS = 2
HANDSHAKE_TIMEOUT_S = 2
MAX_STEPS = 1000
TOKEN_BYTES = 32


def token_path():
    if sys.platform == 'win32':
        base = os.environ['LOCALAPPDATA']
    else:
        base = os.path.expanduser('~/Library/Application Support')
    return os.path.join(base, 'loupedeck-gimp-bridge', 'token')


def write_token():
    """Write a fresh token atomically (temp file + rename) into a user-private directory and return it.

    On Windows the directory inherits the user-only ACL of %LOCALAPPDATA%; on macOS/Linux it is 0700."""
    path = token_path()
    directory = os.path.dirname(path)
    os.makedirs(directory, mode=0o700, exist_ok=True)
    if os.name == 'posix':
        info = os.lstat(directory)
        if os.path.islink(directory) or info.st_uid != os.getuid() or info.st_mode & 0o077:
            raise PermissionError(f'{directory} must be a private directory of the current user')

    token = secrets.token_hex(TOKEN_BYTES)
    fd, temp = tempfile.mkstemp(dir=directory)  # created with mode 0600
    try:
        with os.fdopen(fd, 'w') as file:
            file.write(token)
        os.replace(temp, path)
    except BaseException:
        os.unlink(temp)
        raise
    return token


def clamp(value, low, high):
    return min(max(value, low), high)


class Value:
    """A GIMP context value: getter/setter in GIMP units, step size, range and scale to display units."""

    def __init__(self, getter, setter, step, low, high, scale=1.0, wrap=False, reset=None):
        self.getter, self.setter = getter, setter
        self.step, self.low, self.high = step, low, high
        self.scale, self.wrap, self.reset_func = scale, wrap, reset

    def can_reset(self):
        return self.reset_func is not None

    def read(self):
        return self.getter() * self.scale

    def change(self, steps):
        target = self.getter() + steps * self.step
        if self.wrap:
            target = (target - self.low) % (self.high - self.low) + self.low
        self.setter(clamp(target, self.low, self.high))

    def reset(self):
        self.reset_func()


class BrushSize(Value):
    """Relative steps feel the same for 3 px and 3000 px brushes; small brushes still move >= 1 px per step."""

    FACTOR = 1.05

    def __init__(self):
        super().__init__(Gimp.context_get_brush_size, Gimp.context_set_brush_size, 1.0, 1.0, 10000.0,
                         reset=Gimp.context_set_brush_default_size)

    def change(self, steps):
        size = self.getter()
        target = size * self.FACTOR ** steps
        if abs(target - size) < abs(steps):
            target = size + steps
        self.setter(clamp(round(target), self.low, self.high))


class ForegroundHsv(Value):
    """One channel of the foreground color in GIMP's HSV space (all channels 0..1).

    Grey and black have no hue (and black no saturation), so the HSV last set here is reused as long as
    GIMP's foreground is still that color; turning saturation or value to 0 and back keeps the hue."""

    FORMAT = 'HSVA float'
    last = None  # (foreground as RGBA bytes, HSVA list) after our last change, shared by all channels

    def __init__(self, channel, step, scale, wrap=False):
        self.channel = channel
        super().__init__(self.get_channel, self.set_channel, step, 0.0, 1.0, scale=scale, wrap=wrap)

    @staticmethod
    def rgba(color):
        return color.get_bytes(Babl.format('RGBA float')).get_data()

    def hsva(self):
        color = Gimp.context_get_foreground()
        if ForegroundHsv.last and ForegroundHsv.last[0] == self.rgba(color):
            return list(ForegroundHsv.last[1])
        return list(struct.unpack('ffff', color.get_bytes(Babl.format(self.FORMAT)).get_data()))

    def get_channel(self):
        return self.hsva()[self.channel]

    def set_channel(self, value):
        hsva = self.hsva()
        hsva[self.channel] = value
        color = Gegl.Color.new('black')
        color.set_bytes(Babl.format(self.FORMAT), GLib.Bytes.new(struct.pack('ffff', *hsva)))
        Gimp.context_set_foreground(color)
        ForegroundHsv.last = (self.rgba(Gimp.context_get_foreground()), hsva)


# Steps per dial tick: size 5 %, opacity 1 %, hardness/spacing/force 1 %, angle 1°, aspect 0.1, hue 1°, sat/value 1 %.
VALUES = {
    'size': BrushSize(),
    'opacity': Value(Gimp.context_get_opacity, Gimp.context_set_opacity, 1.0, 0.0, 100.0,
                     reset=lambda: Gimp.context_set_opacity(100.0)),
    'hardness': Value(Gimp.context_get_brush_hardness, Gimp.context_set_brush_hardness, 0.01, 0.0, 1.0,
                      scale=100.0, reset=Gimp.context_set_brush_default_hardness),
    'angle': Value(Gimp.context_get_brush_angle, Gimp.context_set_brush_angle, 1.0, -180.0, 180.0,
                   wrap=True, reset=lambda: Gimp.context_set_brush_angle(0.0)),
    'aspect': Value(Gimp.context_get_brush_aspect_ratio, Gimp.context_set_brush_aspect_ratio, 0.1, -20.0, 20.0,
                    reset=lambda: Gimp.context_set_brush_aspect_ratio(0.0)),
    'spacing': Value(Gimp.context_get_brush_spacing, Gimp.context_set_brush_spacing, 0.01, 0.01, 50.0,
                     scale=100.0, reset=Gimp.context_set_brush_default_spacing),
    'force': Value(Gimp.context_get_brush_force, Gimp.context_set_brush_force, 0.01, 0.0, 1.0,
                   scale=100.0, reset=lambda: Gimp.context_set_brush_force(0.5)),
    'fg-hue': ForegroundHsv(0, 1.0 / 360, 360.0, wrap=True),
    'fg-saturation': ForegroundHsv(1, 0.01, 100.0),
    'fg-value': ForegroundHsv(2, 0.01, 100.0),
}


def handle(line):
    """Run one request; None for anything invalid (the caller then closes the connection)."""
    parts = line.split(' ')
    if len(parts) != 2 or parts[0] not in VALUES:
        return None

    name, arg = parts
    value = VALUES[name]
    if arg == 'reset':
        if not value.can_reset():
            return None
        value.reset()
    elif arg != '?':
        if not (arg[:1] in '+-' and arg[1:].isdigit() and 0 < int(arg[1:]) <= MAX_STEPS):
            return None
        value.change(int(arg))

    return f'{name} {value.read():.1f}'


class Client:
    """One connection, handled on GIMP's main loop: read a line, answer it, read the next."""

    connected = set()  # keeps the objects alive while their async reads are pending

    def __init__(self, connection, token):
        self.connection = connection
        self.expected_auth = f'{PROTOCOL} {token}'
        self.input = connection.get_input_stream()
        self.output = connection.get_output_stream()
        self.buffer = b''
        self.authenticated = False
        Client.connected.add(self)
        self.timeout = GLib.timeout_add_seconds(HANDSHAKE_TIMEOUT_S, self.on_auth_timeout)
        self.next_line()

    def on_auth_timeout(self):
        self.timeout = 0
        if not self.authenticated:
            self.close()
        return GLib.SOURCE_REMOVE

    def next_line(self):
        line, newline, rest = self.buffer.partition(b'\n')
        if newline:
            self.buffer = rest
            self.on_line(line)
        elif len(self.buffer) > MAX_LINE:
            self.close()
        else:
            self.input.read_bytes_async(MAX_LINE + 1, GLib.PRIORITY_DEFAULT, None, self.on_read)

    def on_read(self, stream, result):
        try:
            data = stream.read_bytes_finish(result).get_data()
        except GLib.Error:
            data = b''
        if not data:
            self.close()
            return
        self.buffer += data
        self.next_line()

    def on_line(self, raw):
        try:
            line = raw.decode('ascii') if len(raw) <= MAX_LINE else None
        except UnicodeDecodeError:
            line = None

        if line is None:
            reply = None
        elif not self.authenticated:
            self.authenticated = hmac.compare_digest(line, self.expected_auth)
            reply = '' if self.authenticated else None
        else:
            reply = self.handle_request(line)

        if reply is None:
            self.close()
            return
        if reply:
            try:
                # Blocking write is fine: only an authenticated client gets replies, one per request.
                self.output.write_all((reply + '\n').encode('ascii'), None)
            except GLib.Error:
                self.close()
                return
        self.next_line()

    def handle_request(self, line):
        try:
            return handle(line)
        except Exception as e:  # a failing GIMP call must not stop the extension
            print(f'{PROC_NAME}: request failed: {e}', file=sys.stderr)
            return None

    def close(self):
        if self not in Client.connected:
            return
        Client.connected.discard(self)
        if self.timeout:
            GLib.source_remove(self.timeout)
            self.timeout = 0
        try:
            self.connection.close(None)
        except GLib.Error:
            pass


class LoupedeckBridge(Gimp.PlugIn):

    def do_query_procedures(self):
        return [PROC_NAME]

    def do_create_procedure(self, name):
        procedure = Gimp.Procedure.new(self, name, Gimp.PDBProcType.PERSISTENT, self.run, None)
        procedure.set_documentation('Loupedeck bridge',
                                    'Lets the Loupedeck plugin read and set brush and color values.',
                                    name)
        procedure.set_attribution('nsc', 'nsc', '2026')
        return procedure

    def run(self, procedure, config, data):
        # GIMP's startup waits for this.
        procedure.persistent_ready()
        self.persistent_enable()

        self.service = Gio.SocketService.new()
        try:
            address = Gio.InetSocketAddress.new_from_string(HOST, PORT)
            self.service.add_address(address, Gio.SocketType.STREAM, Gio.SocketProtocol.TCP, None)
            # Only after the bind succeeded: a second GIMP instance must not replace the running one's token.
            self.token = write_token()
        except (GLib.Error, OSError) as e:
            message = getattr(e, 'message', None) or str(e)
            print(f'{PROC_NAME}: inactive: {message}', file=sys.stderr)
            return procedure.new_return_values(Gimp.PDBStatusType.EXECUTION_ERROR, GLib.Error(message))

        self.service.connect('incoming', self.on_incoming)
        self.service.start()
        GLib.MainLoop().run()

        return procedure.new_return_values(Gimp.PDBStatusType.SUCCESS, GLib.Error())

    def on_incoming(self, service, connection, source):
        if len(Client.connected) >= MAX_CLIENTS:
            connection.close(None)
        else:
            Client(connection, self.token)
        return True


Gimp.main(LoupedeckBridge.__gtype__, sys.argv)
