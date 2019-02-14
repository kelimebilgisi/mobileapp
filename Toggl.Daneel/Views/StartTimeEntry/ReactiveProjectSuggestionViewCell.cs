using System;
using Foundation;
using Toggl.Daneel.Cells;
using Toggl.Daneel.Extensions;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Multivac;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Multivac.Extensions;
using UIKit;
using System.Reactive.Disposables;

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

        public CompositeDisposable DisposeBag { get; private set; } = new CompositeDisposable();

        public IObservable<ProjectSuggestion> ToggleTaskSuggestions
            => ToggleTasksButton.Rx().Tap().SelectValue((ProjectSuggestion)Item);

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
        }

        public override void PrepareForReuse()
        {
            DisposeBag.Dispose();
            DisposeBag = new CompositeDisposable();
        }

        protected override void UpdateView()
        {
            var projectSuggestion = (ProjectSuggestion)Item;

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

        private string taskAmoutLabelForCount(int count)
        {
            if (count == 0)
                return "";

            return $"{count} Task{(count == 1 ? "" : "s")}";
        }
    }
}

