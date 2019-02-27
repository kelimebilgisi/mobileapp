using Xamarin.UITest;
using Xamarin.UITest.Android;

namespace Toggl.Tests.UI
{
    public static class Configuration
    {
        public static AndroidApp GetApp()
            => ConfigureApp
                .Android
                .EnableLocalScreenshots()
                .StartApp();
    }
}
