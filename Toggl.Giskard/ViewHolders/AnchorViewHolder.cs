using System;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;

namespace Toggl.Giskard.ViewHolders
{
    public class AnchorViewHolder : RecyclerView.ViewHolder
    {
        public AnchorViewHolder(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        public AnchorViewHolder(View itemView) : base(itemView)
        {
        }
    }
}
