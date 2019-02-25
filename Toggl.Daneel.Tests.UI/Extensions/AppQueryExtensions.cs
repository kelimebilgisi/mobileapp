using System.Linq;
using NUnit.Framework;
using Xamarin.UITest;

namespace Toggl.Tests.UI.Extensions
{
    public static class AppQueryExtensions
    {
        private static readonly string defaultCloseButtonAccessibilityId = "Close";

        public static void NavigateBack(this IApp app)
        {
            var closeButton = app.Query(defaultCloseButtonAccessibilityId).FirstOrDefault();
            if (closeButton != null)
            {
                app.Tap(defaultCloseButtonAccessibilityId);
            }
            else
            {
                app.Back();
            }
        }

        public static void WaitForElementWithText(this IApp app, string element, string text)
        {
            app.WaitFor(() => app.findElementWithText(element, text), $"Could not find element {element} with text {text}");
            Assert.IsTrue(app.findElementWithText(element, text));
        }

        private static bool findElementWithText(this IApp app, string element, string text)
        {
            return app.Query(x => x.Marked(element).Text(text)).FirstOrDefault() != null
                   || app.Query(x => x.Marked(element).Descendant().Text(text)).FirstOrDefault() != null;
        }
    }
}
