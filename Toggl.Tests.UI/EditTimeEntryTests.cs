using NUnit.Framework;
using Toggl.Tests.UI.Extensions;
using Xamarin.UITest;

namespace Toggl.Tests.UI
{
    [TestFixture]
    public sealed class EditTimeEntryTests
    {
        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForMainScreen();
        }

        [Test]
        public void UpdatesTheCorrectCellWhenTheDescriptionIsChanged()
        {
            var initialDescription = "This is a time entry";
            var appendedText = " (and stuff)";
            var newDescription = initialDescription + appendedText;

            app.StartTimeEntryWithDescription(initialDescription);
            app.StopTimeEntry();

            app.TapOnTimeEntryWithDescription(initialDescription);

            app.WaitForElement(EditTimeEntry.EditDescription);
            app.Tap(EditTimeEntry.EditDescription);
            app.EnterText(appendedText);
            app.DismissKeyboard();

            app.Tap(EditTimeEntry.Confirm);
            //On Android the confirm button needs to be tapped twice.
            //Once to finish editing the description and once more to close the edit view
            if (app.Query(EditTimeEntry.Confirm).Length > 0)
                app.Tap(EditTimeEntry.Confirm);
            app.WaitForNoElement(EditTimeEntry.Confirm);

            app.WaitForNoElement(x => x.Text(initialDescription));
            app.WaitForElement(x => x.Text(newDescription));
        }
    }
}
