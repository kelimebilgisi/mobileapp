using System;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Toggl.Giskard.ViewHolders
{
    /// <summary>
    /// Represents a view holder for all cell types that have only one textual piece of UI information
    /// </summary>
    /// <remarks>
    /// The inflated cell has to provide a single UI element with android:id="@+id/Text".
    /// </remarks>
    public class StringViewHolder : BaseRecyclerViewHolder<string>
    {
        private TextView textView;

        public static StringViewHolder Create(View itemView)
            => new StringViewHolder(itemView);

        public StringViewHolder(View itemView) : base(itemView)
        {
        }

        public StringViewHolder(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        protected override void InitializeViews()
        {
            textView = ItemView.FindViewById<TextView>(Resource.Id.Text);
        }

        protected override void UpdateView()
        {
            textView.Text = Item;
        }
    }
}
