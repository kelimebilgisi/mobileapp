using System;
using System.Linq;
using System.Globalization;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Text;
using Android.Text.Style;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using Java.Lang;
using Toggl.Foundation;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Views.EditDuration.Shapes;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive;
using Android.Content.Res;

namespace Toggl.Giskard.Views.EditDuration
{
    public static class WheelDurationInputExtensions
    {
        public static string AsDurationString(this TimeSpan value)
            => $"{(int)value.TotalHours}:{value.Minutes.ToString("D2", CultureInfo.InvariantCulture)}:{value.Seconds.ToString("D2", CultureInfo.InvariantCulture)}";
    }

    [Register("toggl.giskard.views.WheelDurationInput")]
    public partial class WheelDurationInput : EditText, ITextWatcher, View.IOnTouchListener
    {
        private Color fadedTextColor = Color.Gray;

        private TimeSpan originalDuration;
        private TimeSpan duration;
        private DurationFieldInfo input = DurationFieldInfo.Empty;
        private bool isEditing = false;

        private readonly ISubject<TimeSpan> durationSubject = new Subject<TimeSpan>();

        public IObservable<TimeSpan> Duration { get; private set; }

        public WheelDurationInput(IntPtr handle, JniHandleOwnership ownership)
            : base(handle, ownership)
        {
        }

        public WheelDurationInput(Context context) : base(context)
        {
            initialize();
        }

        public WheelDurationInput(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            initializeAttributeSet(context, attrs);
            initialize();
        }

        public WheelDurationInput(Context context, IAttributeSet attrs, int defStyleRes) : base(context, attrs, defStyleRes)
        {
            initializeAttributeSet(context, attrs, 0, defStyleRes);
            initialize();
        }

        public WheelDurationInput(Context context, IAttributeSet attrs, int defStyleAttrs, int defStyleRes) : base(context, attrs, defStyleAttrs, defStyleRes)
        {
            initializeAttributeSet(context, attrs, defStyleAttrs, defStyleRes);
            initialize();
        }

        private void initializeAttributeSet(Context context, IAttributeSet attrs, int defStyleAttrs = 0, int defStyleRes = 0)
        {
            var customsAttrs =
                 context.ObtainStyledAttributes(attrs, Resource.Styleable.WheelDurationInput, defStyleAttrs, defStyleRes);

            try
            {
                var colorStateList = customsAttrs.GetColorStateList(Resource.Styleable.WheelDurationInput_fadedTextColor);
                var fadedTextColorRGB = colorStateList.GetColorForState(GetDrawableState(), Color.Gray);
                fadedTextColor = new Color(fadedTextColorRGB);
            }
            finally
            {
                customsAttrs.Recycle();
            }
        }

        public void SetDuration(TimeSpan duration)
        {
            this.duration = duration;
            input = DurationFieldInfo.FromTimeSpan(duration);
            Text = input.ToTimeSpan().AsDurationString();
        }

        public override void OnEditorAction(ImeAction actionCode)
        {
            if (actionCode == ImeAction.Done || actionCode == ImeAction.Next)
                this.RemoveFocus();
        }

        public override bool OnKeyPreIme(Keycode keyCode, KeyEvent e)
        {
            if (keyCode == Keycode.Back)
            {
                this.RemoveFocus();
            }
            return base.OnKeyPreIme(keyCode, e);
        }

        protected override void OnSelectionChanged(int selStart, int selEnd)
        {
            moveCursorToEnd();
        }

        protected override void OnFocusChanged(bool gainFocus, FocusSearchDirection direction, Android.Graphics.Rect previouslyFocusedRect)
        {
            isEditing = gainFocus;

            if (gainFocus)
            {
                originalDuration = duration;
                input = DurationFieldInfo.Empty;
                Text = input.ToString();
                moveCursorToEnd();
            }
            else
            {
                var actualDuration = input.IsEmpty ? originalDuration : input.ToTimeSpan();
                Text = actualDuration.AsDurationString();
                durationSubject.OnNext(actualDuration);
                Focusable = false;
            }

            base.OnFocusChanged(gainFocus, direction, previouslyFocusedRect);
        }

        private void showCurrentDuration()
        {
            Text = duration.AsDurationString();
        }

        private void initialize()
        {
            Focusable = false;
            Duration = durationSubject.DistinctUntilChanged();

            AddTextChangedListener(this);
            SetOnTouchListener(this);

            TransformationMethod = null;

            SetFilters(
                new IInputFilter[] {
                    new InputFilter(onDigitEntered, onDeletionDetected)
                }
            );
        }

        private void onDeletionDetected()
        {
            var nextInput = input.Pop();
            updateDuration(nextInput);
        }

        private void onDigitEntered(int digit)
        {
            var nextInput = input.Push(digit);
            updateDuration(nextInput);
        }

        private void updateDuration(DurationFieldInfo nextInput)
        {
            if (nextInput.Equals(input))
                return;

            input = nextInput;
            duration = input.ToTimeSpan();
            Text = input.ToString();
        }

        private int getFormattingSplitPoint(string text)
        {
            var colonCount = text.Count(c => c == ':');

            // Text in edit mode always has one colon
            if (colonCount == 1)
                return text.TakeWhile(c => c == '0' || c == ':').Count();

            // Text in display mode always has two colons
            if (colonCount == 2)
                return text.LastIndexOf(':');

            return 0;
        }

        private void applyFormatting(string text, IEditable editable)
        {
            var splitPoint = getFormattingSplitPoint(text);

            editable.ClearSpans();

            if (isEditing)
            {
                editable.SetSpan(new TypefaceSpan("sans-serif-medium"), 0, editable.Length(), SpanTypes.InclusiveInclusive);
                editable.SetSpan(new ForegroundColorSpan(fadedTextColor), 0, splitPoint, SpanTypes.InclusiveInclusive);
            }
            else
            {
                editable.SetSpan(new TypefaceSpan("sans-serif-medium"), 0, splitPoint, SpanTypes.InclusiveInclusive);
            }
        }

        private void moveCursorToEnd()
        {
            if (Text.Length == SelectionEnd && Text.Length == SelectionStart)
                return;

            SetSelection(Text.Length);
        }

        void ITextWatcher.AfterTextChanged(IEditable editable)
        {
            var text = isEditing
                ? input.ToString()
                : editable.ToString();

            if (Text == text)
            {
                applyFormatting(text, editable);
                moveCursorToEnd();

                return;
            }

            Text = text;
        }

        void ITextWatcher.BeforeTextChanged(ICharSequence s, int start, int count, int after)
        {
        }

        void ITextWatcher.OnTextChanged(ICharSequence s, int start, int before, int count)
        {
            moveCursorToEnd();
        }

        public bool OnTouch(View v, MotionEvent e)
        {
            FocusableInTouchMode = true;
            return false;
        }
    }
}
