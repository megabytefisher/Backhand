using Backhand.Services;
using Backhand.Services.Sync;
using Splat;

namespace Backhand
{
    public static class AppBootstrapper
    {
        public static void Register()
        {
            Locator.CurrentMutable.RegisterConstant(new SyncService());
        }
    }
}