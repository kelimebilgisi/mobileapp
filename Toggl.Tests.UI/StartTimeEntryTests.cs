using NUnit.Framework;
using Xamarin.UITest;
using static Toggl.Tests.UI.Extensions.MainExtensions;
using static Toggl.Tests.UI.Extensions.StartTimeEntryExtensions;

namespace Toggl.Tests.UI
{
    [TestFixture]
    public sealed class StartTimeEntryTests
    {
        private const string validEmail = "susancalvin@psychohistorian.museum";

        private IApp app;

        [SetUp]
        public void BeforeEachTest()
        {
            app = Configuration.GetApp();

            app.WaitForStartTimeEntryScreen();
        }

        [Test]
        public void TappingTheDoneButtonCreatesANewTimeEntry()
        {
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForElement(Main.StopTimeEntryButton);
        }

        [Test]
        public void TappingTheDoneButtonCreatesANewTimeEntryWhoseDescriptionMatchesWhatWasTypedInTheDescriptionField()
        {
            var description = "UI testing the Toggl App";

            app.EnterText(description);
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForElement(query => query.Marked(description));
        }

        [Test]
        public void TappingTheCloseButtonShowsConfirmationDialog()
        {
            var description = "UI testing the Toggl App";

            app.EnterText(description);
            app.Tap(StartTimeEntry.CloseButton);

            app.WaitForElement(StartTimeEntry.DialogDiscard);
        }

        [Test]
        public void TappingDiscardWhenClosingShouldNotStartATimeEntry()
        {
            var description = "UI testing the Toggl App";

            app.EnterText(description);
            app.Tap(StartTimeEntry.CloseButton);
            app.Tap(StartTimeEntry.DialogDiscard);

            app.WaitForNoElement(Main.StopTimeEntryButton);
        }

        [Test]
        public void TappingDoneAfterTryingToDiscardAndEditingTheDescriptionDoesNotChangeTheCurrentDescription()
        {
            var description = "UI testing the Toggl App";
            var edit = " - edit";

            app.EnterText(description);
            app.Tap(StartTimeEntry.CloseButton);
            app.Tap(StartTimeEntry.DialogCancel);

            app.EnterText(edit);
            app.Tap(StartTimeEntry.DoneButton);

            app.WaitForElement(query => query.Marked(description + edit));
        }

        [Test]
        public void AddingAProjectBeforeSavingPersistsTheProject()
        {
            const string description = "Field Research ";
            app.EnterText(description);

            var projectName = "Meme Production";
            app.CreateProjectInStartView(projectName);

            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            app.PullToRefresh();

            app.WaitForElement(e => e.All().Property("text").Contains(projectName));
        }

        [Test]
        public void AddingAClientBeforeSavingPersistsTheClient()
        {
            const string description = "Field Research ";
            app.EnterText(description);

            var projectName = "Meme Production";
            var clientName = "The World Wide Web";
            app.CreateProjectInStartView(projectName, clientName);

            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            app.PullToRefresh();

            app.WaitForElement(clientName);
        }

        [Test]
        public void CreatingATimeEntryWithASingleTagWorks()
        {
            const string description = "Working from home ";
            app.EnterText(description);

            const string tag = "Tests";
            app.AddTagInStartView(tag);

            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            //Open the edit view
            app.OpenEditView();

            //Open the tags view. We need to tap it twice because of the onboarding tooltip
            app.WaitForElement(EditTimeEntry.EditTags);
            app.Tap(EditTimeEntry.EditTags);
            app.Tap(EditTimeEntry.EditTags);

            app.WaitForElement(tag);
        }

        [Test]
        public void CreatingATimeEntryWithASingleTagThatAlreadyExistsWorks()
        {
            const string description = "Working from home ";
            const string secondDescription = "Working from home again ";
            const string tag = "Tests";

            // Create the time entry so the tag already exists when we select it
            app.EnterText(description);
            app.AddTagInStartView(tag);
            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            //Actual test starts here
            app.Tap(Main.StartTimeEntryButton);
            app.WaitForElement(StartTimeEntry.DoneButton);
            app.EnterText(secondDescription);
            app.AddTagInStartView(tag, shouldCreateTag: false);
            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            //Open the edit view
            app.OpenEditView();

            //Open the tags view. We need to tap it twice because of the onboarding tooltip
            app.WaitForElement(EditTimeEntry.EditTags);
            app.Tap(EditTimeEntry.EditTags);
            app.Tap(EditTimeEntry.EditTags);

            app.WaitForElement(tag);
        }

        [Test]
        public void AddingMultipleTagsBeforeSavingPersistsTheTags()
        {
            const string description = "Delicious meal ";
            app.EnterText(description);

            var tags = new[] { "Tomato", "Kale", "Carrot", "Broccoli" };
            foreach (var tag in tags)
            {
                app.AddTagInStartView(tag);
            }

            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            app.PullToRefresh();

            //Open the edit view
            app.OpenEditView();

            //Open the tags view. We need to tap it twice because of the onboarding tooltip
            app.WaitForElement(EditTimeEntry.EditTags);
            app.Tap(EditTimeEntry.EditTags);
            app.Tap(EditTimeEntry.EditTags);

            foreach (var tag in tags)
            {
                app.WaitForElement(tag);
            }
        }

        [Test]
        public void SelectingExistingProjectWorks()
        {
            var projectName = "Random project name";

            // Create a time entry with a new project
            app.CreateProjectInStartView(projectName);
            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            // Delete the time entry
            app.OpenEditView();
            app.Tap(EditTimeEntry.DeleteButton);
            var deleteActionButton = app.Query(e => e.All().Property("text").Contains("Delete"))[3];
            app.TapCoordinates(deleteActionButton.Rect.CenterX, deleteActionButton.Rect.CenterY);

            // Create a new entry to with the project from before
            app.Tap(Main.StartTimeEntryButton);
            app.EnterText($"@");
            app.Tap(projectName);
            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            app.PullToRefresh();

            app.WaitForElement(e => e.All().Property("text").Contains(projectName));
        }

        [Test]
        public void SelectingNoProjectWorks()
        {
            var projectName = "Random project name";

            // Create a time entry with project
            app.CreateProjectInStartView(projectName);
            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            // Delete the time entry
            app.OpenEditView();
            app.Tap(EditTimeEntry.DeleteButton);
            var deleteActionButton = app.Query(e => e.All().Property("text").Contains("Delete"))[3];
            app.TapCoordinates(deleteActionButton.Rect.CenterX, deleteActionButton.Rect.CenterY);

            // Create a time entrty by selecting no project
            app.Tap(Main.StartTimeEntryButton);
            app.EnterText($"@");
            app.Tap(q => q.Class("StartTimeEntryProjectsViewCell").Child(0));
            app.Tap(StartTimeEntry.DoneButton);
            app.Tap(Main.StopTimeEntryButton);

            app.WaitForNoElement(e => e.All().Property("text").Contains(projectName));
        }
    }
}
