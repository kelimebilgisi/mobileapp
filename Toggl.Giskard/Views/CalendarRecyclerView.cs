using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using Android.Content;
using Android.Graphics;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Toggl.Giskard.Extensions;
using Toggl.Multivac;
using Color = Android.Graphics.Color;

namespace Toggl.Giskard.Views
{
    [Register("toggl.giskard.views.CalendarRecyclerView")]
    public class CalendarRecyclerView : RecyclerView
    {
        private const int hoursPerDay = 24;

        private static readonly string twelveHoursFormat = Foundation.Resources.TwelveHoursFormat;
        private static readonly string twentyFourHoursFormat = Foundation.Resources.TwentyFourHoursFormat;

        private TimeFormat timeOfDayFormat = TimeFormat.TwelveHoursFormat;
        private bool hasTwoColumns;

        private float hoursX;
        private float timeSliceHeight;
        private float timeSliceStartX;
        private float timeSlicesTopPadding;
        private float verticalLineLeftMargin;
        private float middleLineX;
        private float hoursDistanceFromTimeLine;
        private ImmutableArray<string> hours = ImmutableArray<string>.Empty;
        private ImmutableArray<float> timeLinesYs = ImmutableArray<float>.Empty;
        private ImmutableArray<float> hoursYs = ImmutableArray<float>.Empty;

        private readonly Paint hoursLabelPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = Color.ParseColor("#757575"),
            TextAlign = Paint.Align.Right
        };

        private readonly Paint linesPaint = new Paint(PaintFlags.AntiAlias)
        {
            Color = Color.ParseColor("#19000000")
        };

        #region Constructors
        protected CalendarRecyclerView(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CalendarRecyclerView(Context context) : base(context)
        {
            init();
        }

        public CalendarRecyclerView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            init();
        }

        public CalendarRecyclerView(Context context, IAttributeSet attrs, int defStyle) : base(context, attrs, defStyle)
        {
            init();
        }

        private void init()
        {
            hoursLabelPaint.TextSize = 12.SpToPixels(Context);
            linesPaint.StrokeWidth = 1.DpToPixels(Context);
            timeSliceHeight = 56.DpToPixels(Context);
            timeSliceStartX = 60.DpToPixels(Context);
            timeSlicesTopPadding = 0;
            verticalLineLeftMargin = 68.DpToPixels(Context);
            hoursDistanceFromTimeLine = 12.DpToPixels(Context);
            hoursX = timeSliceStartX - hoursDistanceFromTimeLine;
        }

        #endregion

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            base.OnLayout(changed, l, t, r, b);
            timeLinesYs = createTimeLinesYPositions();
            hours = createHours();
            hoursYs = timeLinesYs.Select(lineY => lineY + hoursLabelPaint.Descent()).ToImmutableArray();
            middleLineX = verticalLineLeftMargin + (Width - verticalLineLeftMargin) / 2f;
        }

        private ImmutableArray<string> createHours()
        {
            DateTime date = new DateTime();
            return Enumerable.Range(0, hoursPerDay)
                .Select(hour => date.AddHours(hour))
                .Select(formatHour)
                .ToImmutableArray();
        }


        private ImmutableArray<float> createTimeLinesYPositions()
         => Enumerable.Range(0, hoursPerDay)
             .Select(line => line * timeSliceHeight + timeSlicesTopPadding)
             .ToImmutableArray();

        public override void OnDraw(Canvas canvas)
        {
            base.OnDraw(canvas);

            var offset = ComputeVerticalScrollOffset();

            canvas.DrawLine(verticalLineLeftMargin, 0f, verticalLineLeftMargin, Height, linesPaint);
            if (hasTwoColumns)
            {
                canvas.DrawLine(middleLineX, 0f, middleLineX, Height, linesPaint);
            }

            for (var hour = 0; hour < timeLinesYs.Length; hour++)
            {
                canvas.DrawLine(timeSliceStartX, timeLinesYs[hour] - offset, Width, timeLinesYs[hour] - offset, linesPaint);
                canvas.DrawText(hours[hour], hoursX, hoursYs[hour] - offset, hoursLabelPaint);
            }
        }

        public void SetHasTwoColumns(bool hasTwoColumns)
        {
            if (this.hasTwoColumns == hasTwoColumns) return;
            this.hasTwoColumns = hasTwoColumns;
            Invalidate();
        }

        public void SetHourFormat(TimeFormat timeFormat)
        {
            timeOfDayFormat = timeFormat;
            Invalidate();
        }

        private string formatHour(DateTime hour)
            => hour.ToString(fixedHoursFormat(), CultureInfo.InvariantCulture);

        private string fixedHoursFormat()
            => timeOfDayFormat.IsTwentyFourHoursFormat ? twentyFourHoursFormat : twelveHoursFormat;
    }
}
