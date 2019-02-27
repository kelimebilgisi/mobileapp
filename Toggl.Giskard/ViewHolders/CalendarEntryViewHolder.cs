using System;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.ViewHolders
{
    public class CalendarEntryViewHolder : RecyclerView.ViewHolder
    {
        public TextView label;

        public CalendarEntryViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public CalendarEntryViewHolder(View itemView) : base(itemView)
        {
            label = itemView.FindViewById<TextView>(Resource.Id.EntryLabel);
        }
    }
}
