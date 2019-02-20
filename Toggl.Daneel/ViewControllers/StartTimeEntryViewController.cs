using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using MvvmCross.Binding;
using MvvmCross.Binding.BindingContext;
using MvvmCross.Commands;
using MvvmCross.Platforms.Ios.Binding;
using MvvmCross.Plugin.Color.Platforms.Ios;
using MvvmCross.Plugin.Visibility;
using Toggl.Daneel.Autocomplete;
using Toggl.Daneel.Extensions;
using Toggl.Daneel.Extensions.Reactive;
using Toggl.Daneel.Presentation.Attributes;
using Toggl.Daneel.ViewSources;
using Toggl.Foundation;
using Toggl.Foundation.Autocomplete;
using Toggl.Foundation.Autocomplete.Span;
using Toggl.Foundation.Autocomplete.Suggestions;
using Toggl.Foundation.MvvmCross.Combiners;
using Toggl.Foundation.MvvmCross.Converters;
using Toggl.Foundation.MvvmCross.Extensions;
using Toggl.Foundation.MvvmCross.Helper;
using Toggl.Foundation.MvvmCross.Onboarding.CreationView;
using Toggl.Foundation.MvvmCross.Onboarding.StartTimeEntryView;
using Toggl.Foundation.MvvmCross.ViewModels;
using Toggl.Multivac.Extensions;
using UIKit;

namespace Toggl.Daneel.ViewControllers
{
    [ModalCardPresentation]
    public sealed partial class StartTimeEntryViewController : KeyboardAwareViewController<StartTimeEntryViewModel>, IDismissableViewController
    {
        private bool isUpdatingDescriptionField;

        private UIImage greyCheckmarkButtonImage;
        private UIImage greenCheckmarkButtonImage;

        private CompositeDisposable disposeBag = new CompositeDisposable();

        private IDisposable descriptionDisposable;
        private IDisposable addProjectOrTagOnboardingDisposable;
        private IDisposable disabledConfirmationButtonOnboardingDisposable;

        private ISubject<bool> isDescriptionEmptySubject = new BehaviorSubject<bool>(true);

        private IUITextInputDelegate emptyInputDelegate = new EmptyInputDelegate();

        public StartTimeEntryViewController()
            : base(nameof(StartTimeEntryViewController))
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing) return;

            descriptionDisposable?.Dispose();
            descriptionDisposable = null;

            addProjectOrTagOnboardingDisposable?.Dispose();
            addProjectOrTagOnboardingDisposable = null;

            disabledConfirmationButtonOnboardingDisposable?.Dispose();
            disabledConfirmationButtonOnboardingDisposable = null;

            TimeInput.LostFocus -= onTimeInputLostFocus;
        }

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            AddProjectBubbleLabel.Text = Resources.AddProjectBubbleText;

            prepareViews();
            prepareOnboarding();

            var source = new StartTimeEntryTableViewSource(SuggestionsTableView);
            SuggestionsTableView.Source = source;

            source.Rx().ModelSelected()
                .Subscribe(ViewModel.SelectSuggestion.Inputs)
                .DisposedBy(disposeBag);

            ViewModel.Suggestions
                .Subscribe(SuggestionsTableView.Rx().ReloadSections(source))
                .DisposedBy(disposeBag);

            source.ToggleTasks
                .Subscribe(ViewModel.ToggleTasks.Inputs)
                .DisposedBy(disposeBag);

            TimeInput.Rx().Duration()
                .Subscribe(ViewModel.SetRunningTime.Inputs)
                .DisposedBy(disposeBag);

            //Text

            ViewModel.DisplayedTime
                .Subscribe(TimeLabel.Rx().Text())
                .DisposedBy(disposeBag);

            Placeholder.Text = ViewModel.PlaceholderText;

            // Buttons
            UIColor booleanToColor(bool b) => b
                ? Color.StartTimeEntry.ActiveButton.ToNativeColor()
                : Color.StartTimeEntry.InactiveButton.ToNativeColor();

            ViewModel.IsBillable
                .Select(booleanToColor)
                .Subscribe(BillableButton.Rx().TintColor())
                .DisposedBy(disposeBag);

            ViewModel.IsSuggestingTags
                .Select(booleanToColor)
                .Subscribe(TagsButton.Rx().TintColor())
                .DisposedBy(disposeBag);

            ViewModel.IsSuggestingProjects
                .Select(booleanToColor)
                .Subscribe(ProjectsButton.Rx().TintColor())
                .DisposedBy(disposeBag);

            //Visibility
            ViewModel.IsBillableAvailable
                .Select(b => b ? (nfloat)42 : 0)
                .Subscribe(BillableButtonWidthConstraint.Rx().Constant())
                .DisposedBy(disposeBag);

            // Actions
            CloseButton.Rx()
                .BindAction(ViewModel.Back)
                .DisposedBy(disposeBag);

            DoneButton.Rx()
                .BindAction(ViewModel.Done)
                .DisposedBy(disposeBag);

            BillableButton.Rx()
                .BindAction(ViewModel.ToggleBillable)
                .DisposedBy(disposeBag);

            StartDateButton.Rx()
                .BindAction(ViewModel.SetStartDate)
                .DisposedBy(disposeBag);

            DateTimeButton.Rx()
                .BindAction(ViewModel.ChangeTime)
                .DisposedBy(disposeBag);

            TagsButton.Rx()
                .BindAction(ViewModel.ToggleTagSuggestions)
                .DisposedBy(disposeBag);

            ProjectsButton.Rx()
                .BindAction(ViewModel.ToggleProjectSuggestions)
                .DisposedBy(disposeBag);

            // Reactive
            ViewModel.TextFieldInfo
                .DistinctUntilChanged()
                .Subscribe(onTextFieldInfo)
                .DisposedBy(DisposeBag);

            DescriptionTextView.Rx().AttributedText()
                .Select(attributedString => attributedString.Length == 0)
                .Subscribe(isDescriptionEmptySubject)
                .DisposedBy(DisposeBag);

            Observable.CombineLatest(
                    DescriptionTextView.Rx().AttributedText().SelectUnit(),
                    DescriptionTextView.Rx().CursorPosition().SelectUnit()
                )
                .Select(_ => DescriptionTextView.AttributedText) // Programatically changing the text doesn't send an event, that's why we do this, to get the last version of the text
                .SubscribeOn(ThreadPoolScheduler.Instance)
                .Do(updatePlaceholder)
                .Select(text => text.AsImmutableSpans((int)DescriptionTextView.SelectedRange.Location))
                .ObserveOn(SynchronizationContext.Current)
                .Subscribe(async info => await ViewModel.OnTextFieldInfoFromView(info))
                .DisposedBy(DisposeBag);

            source.TableRenderCallback = () =>
            {
                ViewModel.StopSuggestionsRenderingStopwatch();
            };
        }

        public async Task<bool> Dismiss()
        {
            return await ViewModel.Close();
        }

        private void onTextFieldInfo(TextFieldInfo textFieldInfo)
        {
            var (attributedText, cursorPosition) = textFieldInfo.AsAttributedTextAndCursorPosition();
            if (DescriptionTextView.AttributedText.GetHashCode() == attributedText.GetHashCode())
                return;

            DescriptionTextView.InputDelegate = emptyInputDelegate; //This line is needed for when the user selects from suggestion and the iOS autocorrect is ready to add text at the same time. Without this line both will happen.
            DescriptionTextView.AttributedText = attributedText;
            var positionToSet =
                DescriptionTextView.GetPosition(DescriptionTextView.BeginningOfDocument, cursorPosition);
            DescriptionTextView.SelectedTextRange = DescriptionTextView.GetTextRange(positionToSet, positionToSet);

            updatePlaceholder();
        }

        private void switchTimeLabelAndInput()
        {
            TimeLabel.Hidden = !TimeLabel.Hidden;
            TimeInput.Hidden = !TimeInput.Hidden;

            TimeLabelTrailingConstraint.Active = !TimeLabel.Hidden;
            TimeInputTrailingConstraint.Active = !TimeInput.Hidden;
        }

        private void updatePlaceholder(NSAttributedString text = null)
        {
            Placeholder.UpdateVisibility(DescriptionTextView);
        }

        protected override void KeyboardWillShow(object sender, UIKeyboardEventArgs e)
        {
            BottomDistanceConstraint.Constant = e.FrameEnd.Height;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        protected override void KeyboardWillHide(object sender, UIKeyboardEventArgs e)
        {
            BottomDistanceConstraint.Constant = 0;
            UIView.Animate(Animation.Timings.EnterTiming, () => View.LayoutIfNeeded());
        }

        public override void TouchesBegan(NSSet touches, UIEvent evt)
        {
            base.TouchesBegan(touches, evt);

            if (TimeInput.IsEditing)
            {
                TimeInput.EndEditing(true);
                DescriptionTextView.BecomeFirstResponder();
            }
        }

        private void prepareViews()
        {
            //This is needed for the ImageView.TintColor bindings to work
            foreach (var button in getButtons())
            {
                button.SetImage(
                    button.ImageForState(UIControlState.Normal)
                          .ImageWithRenderingMode(UIImageRenderingMode.AlwaysTemplate),
                    UIControlState.Normal
                );
                button.TintColor = Color.StartTimeEntry.InactiveButton.ToNativeColor();
            }

            TimeInput.TintColor = Color.StartTimeEntry.Cursor.ToNativeColor();

            DescriptionTextView.TintColor = Color.StartTimeEntry.Cursor.ToNativeColor();
            DescriptionTextView.BecomeFirstResponder();

            Placeholder.ConfigureWith(DescriptionTextView);
            Placeholder.Text = Resources.StartTimeEntryPlaceholder;

            prepareTimeViews();
        }

        private void prepareTimeViews()
        {
            var tapRecognizer = new UITapGestureRecognizer(() =>
            {
                if (!TimeLabel.Hidden)
                    ViewModel.DurationTapped.Execute();

                switchTimeLabelAndInput();

                if (!TimeInput.Hidden)
                {
                    TimeInput.FormattedDuration = TimeLabel.Text;
                    TimeInput.BecomeFirstResponder();
                }
            });

            TimeLabel.UserInteractionEnabled = true;
            TimeLabel.AddGestureRecognizer(tapRecognizer);

            TimeInput.LostFocus += onTimeInputLostFocus;
        }

        private void onTimeInputLostFocus(object sender, EventArgs e)
            => switchTimeLabelAndInput();

        private IEnumerable<UIButton> getButtons()
        {
            yield return TagsButton;
            yield return ProjectsButton;
            yield return BillableButton;
            yield return StartDateButton;
            yield return DateTimeButton;
            yield return DateTimeButton;
        }

        private void toggleTaskSuggestions(ProjectSuggestion parameter)
        {
            var offset = SuggestionsTableView.ContentOffset;
            var frameHeight = SuggestionsTableView.Frame.Height;

            ViewModel.ToggleTasks.Execute(parameter);

            SuggestionsTableView.CorrectOffset(offset, frameHeight);
        }

        private void prepareOnboarding()
        {
            prepareAddProjectOnboardingStep();
            prepareDisableConfirmationButtonOnboardingStep();
        }

        private void prepareAddProjectOnboardingStep()
        {
            var onboardingStorage = ViewModel.OnboardingStorage;
            var addProjectOrtagOnboardingStep = new AddProjectOrTagOnboardingStep(
                onboardingStorage,
                ViewModel.DataSource
            );

            addProjectOrTagOnboardingDisposable = addProjectOrtagOnboardingStep
                .ManageDismissableTooltip(AddProjectOnboardingBubble, onboardingStorage);
        }

        private void prepareDisableConfirmationButtonOnboardingStep()
        {
            greyCheckmarkButtonImage = UIImage.FromBundle("icCheckGrey");
            greenCheckmarkButtonImage = UIImage.FromBundle("doneGreen");

            var disabledConfirmationButtonOnboardingStep
                = new DisabledConfirmationButtonOnboardingStep(
                    ViewModel.OnboardingStorage,
                    isDescriptionEmptySubject.AsObservable());

            disabledConfirmationButtonOnboardingDisposable
                = disabledConfirmationButtonOnboardingStep
                    .ShouldBeVisible
                    .Subscribe(visible => InvokeOnMainThread(() =>
                    {
                        var image = visible ? greyCheckmarkButtonImage : greenCheckmarkButtonImage;
                        DoneButton.SetImage(image, UIControlState.Normal);
                    }));
        }
    }
}

