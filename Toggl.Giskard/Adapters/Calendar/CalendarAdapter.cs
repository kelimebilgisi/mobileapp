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
            switch (holder.ItemViewType)
            {
                case AnchorViewType:
                    holder.ItemView.Tag = anchors[position];
                    break;
            }
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            switch (viewType)
            {
                case AnchorViewType:
                    return new AnchorViewHolder(new View(parent.Context));
                case AnchoredViewType:
                    return null;
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
