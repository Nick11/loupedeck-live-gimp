namespace Loupedeck.GimpPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;

    // What a device button or dial shows: group color, GIMP icon (file in icons/gimp, may be null) and a short label.
    internal interface IGimpTile
    {
        String Id { get; }

        String Group { get; }

        String Icon { get; }

        String Short { get; }
    }

    // Action table with lookup by parameter ID. IDs are stored in users' Loupedeck layouts: never change them after a release.
    internal sealed class GimpTable<T>
        where T : class, IGimpTile
    {
        private const Int32 MaxShortLength = 8;

        private readonly Dictionary<String, T> _byId;

        public GimpTable(params T[] items)
        {
            Debug.Assert(items.All(item => item.Short.Length <= MaxShortLength), "Short labels must fit the device");
            this.All = items;
            this._byId = items.ToDictionary(item => item.Id);
        }

        public IReadOnlyList<T> All { get; }

        public T Find(String id) => id != null && this._byId.TryGetValue(id, out var item) ? item : null;
    }
}
