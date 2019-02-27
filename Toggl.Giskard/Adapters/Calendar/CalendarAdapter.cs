using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.Helper;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHolders;
using Color = Android.Graphics.Color;

namespace Toggl.Giskard.Adapters.Calendar
{
    public class CalendarAdapter : RecyclerView.Adapter
    {
        private readonly int screenWidth;
        private const int anchorViewType = 1;
        private const int anchoredViewType = 2;
        private const int anchorCount = Constants.HoursPerDay;

        private IReadOnlyList<Anchor> anchors;

        private IReadOnlyList<(string, Color, int)> items;

        public CalendarAdapter(Context context, int screenWidth)
        {
            this.screenWidth = screenWidth;
            createTestData(context);
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            if (holder is AnchorViewHolder)
            {
                holder.ItemView.Tag = anchors[position];
                return;
            }

            if (holder is CalendarEntryViewHolder calendarEntryViewHolder)
            {
                calendarEntryViewHolder.ItemView.Background.SetTint(items[position - anchorCount].Item2);
                calendarEntryViewHolder.label.Text = items[position - anchorCount].Item1;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            switch (viewType)
            {
                case anchorViewType:
                    return new AnchorViewHolder(new View(parent.Context));
                case anchoredViewType:
                    return new CalendarEntryViewHolder(LayoutInflater.From(parent.Context).Inflate(Resource.Layout.CalendarEntryCell, parent, false));
                default:
                    throw new InvalidOperationException($"Invalid view type {viewType}");
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position < anchorCount)
                return anchorViewType;

            return anchoredViewType;
        }

        public override int ItemCount => anchorCount + items.Count;

        private void createTestData(Context context)
        {
            items = new[]
            {
                ("Lorem", Color.Crimson, 24),
                ("Ipsum", Color.ParseColor("#328fff"), 25),
                ("Dolor", Color.ParseColor("#c56bff"), 26),
                ("愛は必要だ", Color.Peru, 27),
                ("Stroops time", Color.DimGray, 28),
                ("More Stroops", Color.Goldenrod, 29),
                ("Stuff", Color.SlateGray, 30),
                ("友達も必要だ", Color.Crimson, 31),
                ("心の時間", Color.ParseColor("#328fff"), 32),
                ("色々の会議", Color.ParseColor("#c56bff"), 32),
                ("トッグル会議", Color.Peru, 34),
                ("Even more stroops", Color.DimGray, 35),
                ("Meeting mates", Color.Goldenrod, 36),
                ("Stuff", Color.SlateGray, 37)
            };

            var availableSpace = screenWidth - 76.DpToPixels(context);
            var leftMargin = 72.DpToPixels(context);

            anchors = Enumerable.Range(0, anchorCount).Select(_ => new Anchor(56.DpToPixels(context))).ToArray();
            anchors[0].AnchoredData = new[]
            {
                new AnchorData(24, 0, leftMargin, 56.DpToPixels(context), availableSpace / 2),
                new AnchorData(    25, 10.DpToPixels(context), leftMargin + 4.DpToPixels(context) + availableSpace / 2, 80.DpToPixels(context), availableSpace / 2 - 4.DpToPixels(context))
            };

            anchors[1].AnchoredData = new[]
            {
                new AnchorData(25, -(anchors[0].Height - 10.DpToPixels(context)), leftMargin + 4.DpToPixels(context) + availableSpace / 2, 80.DpToPixels(context), availableSpace / 2 - 4.DpToPixels(context))
            };

            var width = availableSpace / 8 - 4.DpToPixels(context);
            anchors[2].AnchoredData = new[]
            {
                new AnchorData(26, 0, leftMargin + 4.DpToPixels(context), 56.DpToPixels(context), width),
                new AnchorData(27, 10.DpToPixels(context), leftMargin + 4.DpToPixels(context) + width, 56.DpToPixels(context), width),
                new AnchorData(28, 20.DpToPixels(context), leftMargin + 4.DpToPixels(context) + width * 2, 56.DpToPixels(context), width),
                new AnchorData(29, 30.DpToPixels(context), leftMargin + 4.DpToPixels(context) + width * 3, 56.DpToPixels(context), width),
                new AnchorData(30, 40.DpToPixels(context), leftMargin + 4.DpToPixels(context) + width * 4, 56.DpToPixels(context), width),
            };

            anchors[3].AnchoredData = new[]
            {
                new AnchorData(26,  -anchors[0].Height, leftMargin + 4.DpToPixels(context), 56.DpToPixels(context), width),
                new AnchorData(27, -(anchors[0].Height -10.DpToPixels(context)), leftMargin + 4.DpToPixels(context) + width, 56.DpToPixels(context), width),
                new AnchorData(28, -(anchors[0].Height - 20.DpToPixels(context)), leftMargin + 4.DpToPixels(context) + width * 2, 56.DpToPixels(context), width),
                new AnchorData(29, -(anchors[0].Height - 30.DpToPixels(context)), leftMargin + 4.DpToPixels(context) + width * 3, 56.DpToPixels(context), width),
                new AnchorData(30, -(anchors[0].Height - 40.DpToPixels(context)), leftMargin + 4.DpToPixels(context) + width * 4, 56.DpToPixels(context), width),
            };

            anchors[4].AnchoredData = new[]
            {
                new AnchorData(36, anchors[0].Height -20.DpToPixels(context), leftMargin, 200.DpToPixels(context), availableSpace)
            };

            anchors[5].AnchoredData = new[]
            {
                new AnchorData(36, -20.DpToPixels(context), leftMargin, 200.DpToPixels(context), availableSpace)
            };
            anchors[6].AnchoredData = new[]
            {
                new AnchorData(36,  -anchors[0].Height -20.DpToPixels(context), leftMargin, 200.DpToPixels(context), availableSpace)
            };
            anchors[7].AnchoredData = new[]
            {
                new AnchorData(36,  -anchors[0].Height * 2 -20.DpToPixels(context), leftMargin, 200.DpToPixels(context), availableSpace)
            };
            anchors[8].AnchoredData = new[]
            {
                new AnchorData(36,  -anchors[0].Height * 3 -20.DpToPixels(context), leftMargin, 200.DpToPixels(context), availableSpace)
            };

            anchors[10].AnchoredData = new[]
            {
                new AnchorData(36, 0, leftMargin, 200.DpToPixels(context), availableSpace / 2),
                new AnchorData(37, 10.DpToPixels(context), leftMargin + availableSpace / 2 + 4.DpToPixels(context), 80.DpToPixels(context), availableSpace / 2 - 4.DpToPixels(context))
            };
        }
    }
}
