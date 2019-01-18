using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Multivac;
using UIKit;

namespace Toggl.Daneel.Views.StartTimeEntry
{
    public sealed partial class ReactiveProjectSuggestionViewCell : BaseTableViewCell<AutocompleteSuggestion>
    {
        private const float selectedProjectBackgroundAlpha = 0.12f;

        private const int fadeViewTrailingConstraintWithTasks = 72;
        private const int fadeViewTrailingConstraintWithoutTasks = 16;

        public static readonly NSString Key = new NSString(nameof(ReactiveProjectSuggestionViewCell));
        public static readonly UINib Nib;

        public bool TopSeparatorHidden
        {
            get => TopSeparatorView.Hidden;
            set => TopSeparatorView.Hidden = value;
        }

        public bool BottomSeparatorHidden
        {
            get => BottomSeparatorView.Hidden;
            set => BottomSeparatorView.Hidden = value;
        }

        private ISubject<ProjectSuggestion> toggleTaskSuggestionsSubject = new Subject<ProjectSuggestion>();
        public IObservable<ProjectSuggestion> ToggleTaskSuggestions => toggleTaskSuggestionsSubject.AsObservable();

        static ReactiveProjectSuggestionViewCell()
        {
            Nib = UINib.FromName(nameof(ReactiveProjectSuggestionViewCell), NSBundle.MainBundle);
        }

        protected ReactiveProjectSuggestionViewCell(IntPtr handle) : base(handle)
        {
            // Note: this .ctor should not contain any initialization logic.
        }

        public override void AwakeFromNib()
        {
            base.AwakeFromNib();

            FadeView.FadeRight = true;
            ClientNameLabel.LineBreakMode = UILineBreakMode.TailTruncation;
            ProjectNameLabel.LineBreakMode = UILineBreakMode.TailTruncation;
            ToggleTasksButton.TouchUpInside += toggleTaskSuggestions;
        }

        protected override void UpdateView()
        {
            if (Item is ProjectSuggestion projectSuggestion)
            {
                //Text
                ProjectNameLabel.Text = projectSuggestion.ProjectName;
                ClientNameLabel.Text = projectSuggestion.ClientName;
                AmountOfTasksLabel.Text = taskAmoutLabelForCount(projectSuggestion.NumberOfTasks);

                //Color
                var nativeProjectColor = new Color(projectSuggestion.ProjectColor).ToNativeColor();
                ProjectNameLabel.TextColor = nativeProjectColor;
                ProjectDotView.BackgroundColor = nativeProjectColor;
                SelectedProjectView.BackgroundColor = projectSuggestion.Selected
                    ? nativeProjectColor.ColorWithAlpha(selectedProjectBackgroundAlpha)
                    : UIColor.Clear;

                //Visibility
                ToggleTaskImage.Hidden = !projectSuggestion.HasTasks;
                ToggleTasksButton.Hidden = !projectSuggestion.HasTasks;
                AmountOfTasksLabel.Hidden = !projectSuggestion.HasTasks;

                //Constraints
                FadeViewTrailingConstraint.Constant = projectSuggestion.HasTasks
                    ? fadeViewTrailingConstraintWithTasks
                    : fadeViewTrailingConstraintWithoutTasks;
            }
            else
            {
                throw new Exception($"Unexpected {nameof(Item)} type. It should have been of type {typeof(ProjectSuggestion)}, but it was {Item.GetType()}");
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            ToggleTasksButton.TouchUpInside -= toggleTaskSuggestions;
        }

        private string taskAmoutLabelForCount(int count)
        {
            if (count == 0)
                return "";

            return $"{count} Task{(count == 1 ? "" : "s")}";
        }

        private void toggleTaskSuggestions(object sender, EventArgs e)
        {
            if (Item is ProjectSuggestion projectSuggestion)
                toggleTaskSuggestionsSubject.OnNext(projectSuggestion);
        }
    }
}

