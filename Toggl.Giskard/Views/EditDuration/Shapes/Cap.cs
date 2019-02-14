using System;
using Android.Graphics;

namespace Toggl.Giskard.Views.EditDuration.Shapes
{
    public sealed class Cap
    {
        private const int noFlags = 0;
        private readonly Paint capPaint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint capBorderPaint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint iconPaint = new Paint(PaintFlags.AntiAlias);
        private readonly Paint arcPaint = new Paint(PaintFlags.AntiAlias);
        private readonly float radius;
        private readonly float capInnerSquareSide;
        private readonly float capBorderStrokeWidth;
        private readonly float arcRadius;
        private readonly float shadowWidth;
        private readonly Bitmap iconBitmap;
        private readonly Bitmap shadowBitmap;

        private readonly Paint shadowPaint = new Paint(noFlags)
        {
            Color = Color.ParseColor("#66000000")
        };

        private PointF position;
        private bool showOnlyBackground;

        public Cap(float radius,
            float arcWidth,
            Color capColor,
            Color capBorderColor,
            Color foregroundColor,
            float capBorderStrokeWidth,
            Bitmap icon,
            Color iconColor,
            float shadowWidth)
        {
            this.capBorderStrokeWidth = capBorderStrokeWidth;
            this.shadowWidth = shadowWidth;
            this.radius  = radius;
            capInnerSquareSide = (float) Math.Sqrt((radius - shadowWidth) * (radius - shadowWidth) * 2) * 0.5f;
            arcRadius = arcWidth / 2f;
            arcPaint.Color = foregroundColor;
            capPaint.Color = capColor;
            capBorderPaint.SetStyle(Paint.Style.Stroke);
            capBorderPaint.StrokeWidth = capBorderStrokeWidth;
            capBorderPaint.Color = capBorderColor;
            iconBitmap = icon;
            iconPaint.SetColorFilter(new PorterDuffColorFilter(iconColor, PorterDuff.Mode.SrcIn));
            shadowPaint.SetMaskFilter(new BlurMaskFilter(shadowWidth, BlurMaskFilter.Blur.Normal));
            shadowPaint.SetStyle(Paint.Style.Fill);
            shadowBitmap = Bitmap.CreateBitmap((int) (radius * 2f), (int) (radius * 2f), Bitmap.Config.Argb8888);
            var shadowCanvas = new Canvas(shadowBitmap);
            shadowCanvas.DrawCircle(radius, radius, radius - shadowWidth, shadowPaint);
        }

        public void SetShowOnlyBackground(bool shouldOnlyShowBackground)
        {
            showOnlyBackground = shouldOnlyShowBackground;
        }

        public void SetForegroundColor(Color color)
        {
            arcPaint.Color = color;
        }

        public void SetPosition(PointF newPosition)
        {
            position = newPosition;
        }

        public void OnDraw(Canvas canvas)
        {
            if (position == null) return;

            if (showOnlyBackground)
            {
                canvas.DrawCircle(position.X, position.Y, arcRadius, arcPaint);
                return;
            }

            var innerSquareLeft = position.X - capInnerSquareSide;
            var innerSquareTop = position.Y - capInnerSquareSide;
            canvas.DrawBitmap(shadowBitmap, position.X - radius, position.Y - radius, shadowPaint);
            canvas.DrawCircle(position.X, position.Y, radius - shadowWidth - capBorderStrokeWidth / 4f, capPaint);
            canvas.DrawCircle(position.X, position.Y, radius - shadowWidth, capBorderPaint);
            canvas.DrawBitmap(iconBitmap,
                innerSquareLeft + (capInnerSquareSide * 2f - iconBitmap.Width) / 2f,
                innerSquareTop + (capInnerSquareSide * 2f - iconBitmap.Height) / 2f, iconPaint);
        }
    }
}
