using System;
using System.Collections.Generic;
using System.Linq;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Toggl.Foundation.Helper;
using Toggl.Giskard.Extensions;
using Toggl.Giskard.ViewHolders;

namespace Toggl.Giskard.Adapters.Calendar
{
    public class CalendarAdapter : RecyclerView.Adapter
    {
        private readonly int screenWidth;
        private const int AnchorViewType = 1;
        private const int AnchoredViewType = 2;

        private readonly int anchorCount = Constants.HoursPerDay;

        private IReadOnlyList<Anchor> anchors;

        {
        public CalendarAdapter(Context context, int screenWidth)
        {
            this.screenWidth = screenWidth;
            anchors = Enumerable.Range(0, anchorCount).Select(_ => new Anchor(56.DpToPixels(context))).ToArray();
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
                case AnchorViewType:
                    return new AnchorViewHolder(new View(parent.Context));
                case AnchoredViewType:
                    return new CalendarEntryViewHolder(LayoutInflater.From(parent.Context).Inflate(Resource.Layout.CalendarEntryCell, parent, false));
                default:
                    throw new InvalidOperationException($"Invalid view type {viewType}");
            }
        }

        public override int GetItemViewType(int position)
        {
            if (position < anchorCount)
                return AnchorViewType;

            return AnchoredViewType;
        }

        public override int ItemCount => anchorCount;
    }
}
