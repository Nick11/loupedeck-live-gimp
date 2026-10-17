namespace Loupedeck.GimpPlugin
{
    using System;

    // macOS: bundle org.gimp.gimp, executable "gimp". Windows: bin\gimp-<major>.exe (stable), gimp-<major>.<minor>.exe (dev builds,
    // including the expected next version 3.4).
    public class GimpApplication : ClientApplication
    {
        protected override String[] GetProcessNames() => new[] { "gimp", "gimp-3", "gimp-3.0", "gimp-3.2", "gimp-3.4" };

        protected override String[] GetBundleNames() => new[] { "org.gimp.gimp" };
    }
}
