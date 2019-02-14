using System;
using Android.Graphics;
using Toggl.Giskard.Extensions;
using static Toggl.Multivac.Math;
using Math = Java.Lang.Math;
using Point = Toggl.Multivac.Point;

namespace Toggl.Giskard.Views.EditDuration.Shapes
{
    public class Dot
    {
        private const int visibilityThresholdInDegrees = 15;
        private readonly Point pivotCenter;
        private readonly float distanceToPivot;
        private readonly float radius;
        private readonly Paint paint = new Paint(PaintFlags.AntiAlias);
        private PointF position;
        private bool hidden;

        public Dot(Point pivotCenter, float distanceToPivot, float radius, Color color)
        {
            this.pivotCenter = pivotCenter;
            this.distanceToPivot = distanceToPivot;
            this.radius = radius;
            paint.Color = color;
        }

        public void OnDraw(Canvas canvas)
        {
            if (hidden || position == null) return;

            canvas.DrawCircle(position.X, position.Y, radius, paint);
        }

        public void Update(double startTimeAngle, double endTimeAngle)
        {
            var startAngle = (float) startTimeAngle;
            var endAngle = (float) endTimeAngle;
            var diffAngle = endAngle - startAngle + (endAngle < startAngle ? FullCircle : 0);
            var diffInDegrees = (float) Math.ToDegrees(diffAngle);
            hidden = diffInDegrees < visibilityThresholdInDegrees;
            position = PointOnCircumference(pivotCenter, startAngle + diffAngle / 2f, distanceToPivot).ToPointF();
        }
    }
}
