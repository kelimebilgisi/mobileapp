using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using MvvmCross.Plugin.Color.Platforms.Android;
using Toggl.Foundation.Analytics;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.Helper;
using Toggl.Giskard.Views.EditDuration.Shapes;
using Toggl.Multivac;
using Toggl.Multivac.Extensions;
using static Toggl.Multivac.Math;
using FoundationColor = Toggl.Foundation.MvvmCross.Helper.Color;
using Math = System.Math;
using Color = Android.Graphics.Color;
namespace Toggl.Giskard.Views.EditDuration
{
    [Register("toggl.giskard.views.WheelForegroundView")]
    public class WheelForegroundView : View
    {
        private readonly Color capBackgroundColor = Color.White;
        private readonly Color capBorderColor = Color.ParseColor("#cecece");
        private readonly Color capIconColor = Color.ParseColor("#328fff");
        private float radius;
        private float wheelHandleDotIndicatorRadius;
        private float wheelHandleDotIndicatorDistanceToCenter;
        private float extendedRadiusMultiplier = 1.5f;
        private float arcWidth;
        private float capWidth;
        private float capBorderStrokeWidth;
        private float capShadowWidth;
        private int capIconSize;
        private int vibrationDurationInMilliseconds = 5;
        private int vibrationAmplitude = 5;
        private PointF startTimePosition;
        private PointF endTimePosition;
        private PointF center;
        private RectF bounds;
        private Vibrator hapticFeedbackProvider;

        private DateTimeOffset startTime;
        private DateTimeOffset endTime;
        private bool isRunning;

        private double startTimeAngle;
        private double endTimeAngle;

        private double endPointsRadius;
        private bool isDragging;
        private WheelUpdateType updateType;
        private double editBothAtOnceStartTimeAngleOffset;

        private int numberOfFullLoops => (int) ((EndTime - StartTime).TotalMinutes / MinutesInAnHour);
        private bool isFullCircle => numberOfFullLoops >= 1;

        private Color backgroundColor
            => FoundationColor.EditDuration.Wheel.Rainbow.GetPingPongIndexedItem(numberOfFullLoops).ToNativeColor();

        private Color foregroundColor
            => FoundationColor.EditDuration.Wheel.Rainbow.GetPingPongIndexedItem(numberOfFullLoops + 1).ToNativeColor();

        private readonly Subject<EditTimeSource> timeEditedSubject = new Subject<EditTimeSource>();
        private readonly Subject<DateTimeOffset> startTimeSubject = new Subject<DateTimeOffset>();
        private readonly Subject<DateTimeOffset> endTimeSubject = new Subject<DateTimeOffset>();

        private Wheel fullWheel;
        private Arc arc;
        private Cap endCap;
        private Cap startCap;
        private Dot wheelHandleDotIndicator;

        public IObservable<EditTimeSource> TimeEdited
            => timeEditedSubject.AsObservable();

        public IObservable<DateTimeOffset> StartTimeObservable => startTimeSubject.AsObservable();

        public IObservable<DateTimeOffset> EndTimeObservable => endTimeSubject.AsObservable();

        public DateTimeOffset MinimumStartTime { get; set; }

        public DateTimeOffset MaximumStartTime { get; set; }

        public DateTimeOffset MinimumEndTime { get; set; }

        public DateTimeOffset MaximumEndTime { get; set; }

        public DateTimeOffset StartTime
        {
            get => startTime;
            set
            {
                if (startTime == value) return;

                startTime = value.Clamp(MinimumStartTime, MaximumStartTime);

                if (center == null) return;

                startTimeAngle = startTime.LocalDateTime.TimeOfDay.ToAngleOnTheDial().ToPositiveAngle();
                startTimePosition = PointOnCircumference(center.ToPoint(), startTimeAngle, endPointsRadius).ToPointF();
                arc?.Update(startTimeAngle, endTimeAngle);
                wheelHandleDotIndicator?.Update(startTimeAngle, endTimeAngle);
                startTimeSubject.OnNext(startTime);
                Invalidate();
            }
        }

        public DateTimeOffset EndTime
        {
            get => endTime;
            set
            {
                if (endTime == value) return;

                endTime = value.Clamp(MinimumEndTime, MaximumEndTime);

                if (center == null) return;

                endTimeAngle = endTime.LocalDateTime.TimeOfDay.ToAngleOnTheDial().ToPositiveAngle();
                endTimePosition = PointOnCircumference(center.ToPoint(), endTimeAngle, endPointsRadius).ToPointF();
                arc?.Update(startTimeAngle, endTimeAngle);
                wheelHandleDotIndicator?.Update(startTimeAngle, endTimeAngle);
                endTimeSubject.OnNext(endTime);
                Invalidate();
            }
        }

        public bool IsRunning
        {
            get => isRunning;
            set
            {
                if (isRunning == value) return;
                isRunning = value;
                Invalidate();
            }
        }

        #region Constructors

        protected WheelForegroundView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public WheelForegroundView(Context context) : base(context)
        {
            init();
        }

        public WheelForegroundView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            init();
        }

        public WheelForegroundView(Context context, IAttributeSet attrs, int defStyleAttr) : base(context, attrs, defStyleAttr)
        {
            init();
        }

        public WheelForegroundView(Context context, IAttributeSet attrs, int defStyleAttr, int defStyleRes) : base(context, attrs, defStyleAttr, defStyleRes)
        {
            init();
        }

        private void init()
        {
            MinimumStartTime = DateTimeOffset.MinValue;
            MaximumStartTime = DateTimeOffset.Now;
            MinimumEndTime = DateTimeOffset.Now;
            MaximumEndTime = DateTimeOffset.MaxValue;
            startTime = DateTimeOffset.Now;
            endTime = DateTimeOffset.Now;
            arcWidth = 8.DpToPixels(Context);
            capWidth = 28.DpToPixels(Context);
            capIconSize = 18.DpToPixels(Context);
            capShadowWidth = 2.DpToPixels(Context);
            capBorderStrokeWidth = 1.DpToPixels(Context);
            wheelHandleDotIndicatorRadius = 2.DpToPixels(Context);
            hapticFeedbackProvider = (Vibrator) Context.GetSystemService(Context.VibratorService);
        }

        #endregion

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);
            radius = Width * 0.5f;
            center = new PointF(radius, radius);
            bounds = new RectF(capWidth, capWidth, Width - capWidth, Width - capWidth);
            endPointsRadius = radius - capWidth;
            wheelHandleDotIndicatorDistanceToCenter = radius - capWidth / 2f;
            startTimeAngle = startTime.LocalDateTime.TimeOfDay.ToAngleOnTheDial().ToPositiveAngle();
            startTimePosition = PointOnCircumference(center.ToPoint(), startTimeAngle, endPointsRadius).ToPointF();
            endTimeAngle = endTime.LocalDateTime.TimeOfDay.ToAngleOnTheDial().ToPositiveAngle();
            endTimePosition = PointOnCircumference(center.ToPoint(), endTimeAngle, endPointsRadius).ToPointF();
            setupDrawingDelegates();
            arc.Update(startTimeAngle, endTimeAngle);
            wheelHandleDotIndicator.Update(startTimeAngle, endTimeAngle);
            Invalidate();
        }

        protected override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);
            updateUIElements();
            fullWheel.OnDraw(canvas);
            arc.OnDraw(canvas);
            wheelHandleDotIndicator.OnDraw(canvas);
            endCap.OnDraw(canvas);
            startCap.OnDraw(canvas);
        }

        private void setupDrawingDelegates()
        {
            fullWheel = new Wheel(bounds, arcWidth, backgroundColor);
            arc = new Arc(bounds, arcWidth, foregroundColor);
            var endCapBitmap = Context.GetVectorDrawable(Resource.Drawable.ic_stop).ToBitmap(capIconSize, capIconSize);
            var startCapBitmap = Context.GetVectorDrawable(Resource.Drawable.ic_play).ToBitmap(capIconSize, capIconSize);
            endCap = createCapWithIcon(endCapBitmap);
            startCap = createCapWithIcon(startCapBitmap);
            wheelHandleDotIndicator = new Dot(center.ToPoint(), wheelHandleDotIndicatorDistanceToCenter, wheelHandleDotIndicatorRadius, capIconColor);
        }

        private Cap createCapWithIcon(Bitmap iconBitmap)
        {
            return new Cap(radius: capWidth / 2f,
                arcWidth: arcWidth,
                capColor: capBackgroundColor,
                capBorderColor: capBorderColor,
                foregroundColor: foregroundColor,
                capBorderStrokeWidth: capBorderStrokeWidth,
                icon: iconBitmap,
                iconColor: capIconColor,
                shadowWidth: capShadowWidth);
        }


        private void updateUIElements()
        {
            startCap.SetPosition(startTimePosition);
            startCap.SetForegroundColor(foregroundColor);
            endCap.SetPosition(endTimePosition);
            endCap.SetForegroundColor(foregroundColor);
            endCap.SetShowOnlyBackground(IsRunning);

            fullWheel.SetFillColor(backgroundColor);
            fullWheel.SetHidden(!isFullCircle);

            arc.SetFillColor(foregroundColor);
        }

        #region Touch interaction

        public override bool OnTouchEvent(MotionEvent motionEvent)
        {
            switch (motionEvent.Action)
            {
                case MotionEventActions.Down:
                    touchesBegan(motionEvent.ToPointF());
                    return true;
                case MotionEventActions.Up:
                    touchesEnded();
                    return true;
                case MotionEventActions.Move:
                    touchesMoved(motionEvent.ToPointF());
                    return true;
                case MotionEventActions.Cancel:
                    touchesCancelled();
                    return base.OnTouchEvent(motionEvent);
            }

            return base.OnTouchEvent(motionEvent);
        }

        private void touchesBegan(PointF position)
        {
            if (isValid(position))
            {
                isDragging = true;
            }
        }

        private void touchesMoved(PointF position)
        {
            if (isDragging == false) return;

            double previousAngle;
            switch (updateType)
            {
                case WheelUpdateType.EditStartTime:
                    previousAngle = startTimeAngle;
                    break;
                case WheelUpdateType.EditEndTime:
                    previousAngle = endTimeAngle;
                    break;
                default:
                    previousAngle = startTimeAngle + editBothAtOnceStartTimeAngleOffset;
                    break;
            }

            var currentAngle = AngleBetween(position.ToPoint(), center.ToPoint());

            var angleChange = currentAngle - previousAngle;
            while (angleChange < -Math.PI) angleChange += FullCircle;
            while (angleChange > Math.PI) angleChange -= FullCircle;

            var timeChange = angleChange.AngleToTime();

            updateEditedTime(timeChange);
        }

        private void touchesCancelled()
        {
            finishTouchEditing();
        }

        private void touchesEnded()
        {
            finishTouchEditing();
            switch (updateType)
            {
                case WheelUpdateType.EditStartTime:
                    timeEditedSubject.OnNext(EditTimeSource.WheelStartTime);
                    break;
                case WheelUpdateType.EditEndTime:
                    timeEditedSubject.OnNext(EditTimeSource.WheelEndTime);
                    break;
                default:
                    timeEditedSubject.OnNext(EditTimeSource.WheelBothTimes);
                    break;
            }
        }

        private bool isValid(PointF position)
        {
            var intention = determineTapIntention(position);
            if (!intention.HasValue)
                return false;

            updateType = intention.Value;
            if (updateType == WheelUpdateType.EditBothAtOnce)
            {
                editBothAtOnceStartTimeAngleOffset =
                    AngleBetween(position.ToPoint(), center.ToPoint()) - startTimeAngle;
            }

            return true;

        }

        private WheelUpdateType? determineTapIntention(PointF position)
        {
            if (touchesStartCap(position))
            {
                return WheelUpdateType.EditStartTime;
            }

            if (!IsRunning && touchesEndCap(position))
            {
                return WheelUpdateType.EditEndTime;
            }

            if (touchesStartCap(position, extendedRadius: true))
            {
                return WheelUpdateType.EditStartTime;
            }

            if (!IsRunning && touchesEndCap(position, extendedRadius: true))
            {
                return WheelUpdateType.EditEndTime;
            }

            if (!IsRunning && isOnTheWheelBetweenStartAndStop(position))
            {
                return WheelUpdateType.EditBothAtOnce;
            }

            return null;
        }

        private bool touchesStartCap(PointF position, bool extendedRadius = false)
            => isCloseEnough(position, startTimePosition, calculateCapRadius(extendedRadius));

        private bool touchesEndCap(PointF position, bool extendedRadius = false)
            => isCloseEnough(position, endTimePosition, calculateCapRadius(extendedRadius));

        private float calculateCapRadius(bool extendedRadius)
            => (extendedRadius ? extendedRadiusMultiplier : 1) * (capWidth / 2);

        private static bool isCloseEnough(PointF tapPosition, PointF endPoint, float radius)
            => DistanceSq(tapPosition.ToPoint(), endPoint.ToPoint()) <= radius * radius;

        private bool isOnTheWheelBetweenStartAndStop(PointF point)
        {
            var distanceFromCenterSq = DistanceSq(center.ToPoint(), point.ToPoint());

            if (distanceFromCenterSq < capWidth * capWidth
                || distanceFromCenterSq > radius * radius)
            {
                return false;
            }

            var angle = AngleBetween(point.ToPoint(), center.ToPoint());
            return isFullCircle || angle.IsBetween(startTimeAngle, endTimeAngle);
        }

        private void updateEditedTime(TimeSpan diff)
        {
            var giveFeedback = false;
            var duration = EndTime - StartTime;

            if (updateType == WheelUpdateType.EditStartTime
                || updateType == WheelUpdateType.EditBothAtOnce)
            {
                var nextStartTime = (StartTime + diff).RoundToClosestMinute();
                giveFeedback = nextStartTime != StartTime;
                StartTime = nextStartTime;
            }

            if (updateType == WheelUpdateType.EditEndTime)
            {
                var nextEndTime = (EndTime + diff).RoundToClosestMinute();
                giveFeedback = nextEndTime != EndTime;
                EndTime = nextEndTime;
            }

            if (updateType == WheelUpdateType.EditBothAtOnce)
            {
                EndTime = StartTime + duration;
            }

            if (giveFeedback)
            {
                vibrate();
            }
        }

        private void vibrate()
        {
            hapticFeedbackProvider?.ActivateVibration(vibrationDurationInMilliseconds, vibrationAmplitude);
        }

        private void finishTouchEditing()
        {
            isDragging = false;
        }

        #endregion

        private enum WheelUpdateType
        {
            EditStartTime,
            EditEndTime,
            EditBothAtOnce
        }
    }
}
