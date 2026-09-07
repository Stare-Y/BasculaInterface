using System.Runtime.CompilerServices;

namespace BasculaTerminalTest.Integration
{
    internal static class ModuleInit
    {
        /// <summary>
        /// Testcontainers' Ryuk reaper container doesn't work under rootless Podman; disabling it
        /// makes the suite portable across a dev box (Podman) and GitHub-hosted runners (Docker).
        /// Containers are still torn down explicitly by <see cref="BasculaApiFactory"/>.
        /// Must run before Testcontainers reads its configuration, hence a module initializer.
        /// </summary>
        [ModuleInitializer]
        internal static void DisableRyuk()
        {
            if (Environment.GetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED") is null)
            {
                Environment.SetEnvironmentVariable("TESTCONTAINERS_RYUK_DISABLED", "true");
            }
        }
    }
}
