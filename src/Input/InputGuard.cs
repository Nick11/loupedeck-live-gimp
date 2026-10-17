namespace Loupedeck.GimpPlugin
{
    using System;
    using System.Runtime.InteropServices;

    // OS checks before injecting keys: macOS permissions, and modifiers the user physically holds
    // (they would merge into the sent shortcut, e.g. held Shift turns Ctrl+Z into Ctrl+Shift+Z).
    internal static class InputGuard
    {
        private const String AccessibilityHint =
            "macOS blocks the keystrokes: allow LogiPluginService in System Settings → Privacy & Security → Accessibility.";

        private const String SecureInputHint =
            "Secure keyboard entry is on (password field or terminal), GIMP doesn't receive keystrokes.";

        // null when keys can be sent.
        public static String BlockReason() =>
            !OperatingSystem.IsMacOS() ? null
            : !Mac.AXIsProcessTrusted() ? AccessibilityHint
            : Mac.IsSecureEventInputEnabled() ? SecureInputHint
            : null;

        public static Boolean AnyModifierPressed() => OperatingSystem.IsMacOS() ? Mac.ModifierDown() : Windows.ModifierDown();

        private static class Mac
        {
            // kCGEventFlagMaskShift | Control | Alternate | Command
            private const UInt64 ModifierMask = 0x20000 | 0x40000 | 0x80000 | 0x100000;
            private const Int32 HidSystemState = 1;

            public static Boolean ModifierDown() => (CGEventSourceFlagsState(HidSystemState) & ModifierMask) != 0;

            [DllImport("/System/Library/Frameworks/ApplicationServices.framework/ApplicationServices")]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern Boolean AXIsProcessTrusted();

            [DllImport("/System/Library/Frameworks/Carbon.framework/Carbon")]
            [return: MarshalAs(UnmanagedType.I1)]
            public static extern Boolean IsSecureEventInputEnabled();

            [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
            private static extern UInt64 CGEventSourceFlagsState(Int32 stateId);
        }

        private static class Windows
        {
            // VK_SHIFT, VK_CONTROL, VK_MENU, VK_LWIN, VK_RWIN
            private static readonly Int32[] ModifierKeys = { 0x10, 0x11, 0x12, 0x5B, 0x5C };

            public static Boolean ModifierDown() => Array.Exists(ModifierKeys, key => (GetAsyncKeyState(key) & 0x8000) != 0);

            [DllImport("user32.dll")]
            private static extern Int16 GetAsyncKeyState(Int32 virtualKey);
        }
    }
}
