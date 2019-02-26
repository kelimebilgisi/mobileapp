using System;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Java.Interop;
using Object = Java.Lang.Object;

namespace Toggl.Giskard.Views.Calendar
{
    public partial class CalendarLayoutManager
    {
        public class AnchorSavedState : View.BaseSavedState
        {
            public int TopAnchorPosition { get; set; }
            public int AnchorOffset { get; set; }
            public bool AnchorShouldLayoutFromEnd { get; set; }

            protected AnchorSavedState(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
            {
            }

            public AnchorSavedState(IParcelable superState) : base(superState)
            {

            }

            public AnchorSavedState(Parcel source) : base(source)
            {
                TopAnchorPosition = source.ReadInt();
                AnchorOffset = source.ReadInt();
                AnchorShouldLayoutFromEnd = source.ReadInt() == 1;
            }

            public AnchorSavedState(AnchorSavedState other, IParcelable supersTate) : base(supersTate)
            {
                TopAnchorPosition = other.TopAnchorPosition;
                AnchorOffset = other.AnchorOffset;
                AnchorShouldLayoutFromEnd = other.AnchorShouldLayoutFromEnd;
            }

            public override void WriteToParcel(Parcel dest, ParcelableWriteFlags flags)
            {
                base.WriteToParcel(dest, flags);
                dest.WriteInt(TopAnchorPosition);
                dest.WriteInt(AnchorOffset);
                dest.WriteInt(AnchorShouldLayoutFromEnd ? 1 : 0);
            }

            public bool HasValidAnchor() => TopAnchorPosition >= 0 && TopAnchorPosition < anchorCount;

            public void InvalidateAnchor()
            {
                TopAnchorPosition = RecyclerView.NoPosition;
            }

            [ExportField("CREATOR")]
            public static SaveStateCreator InitCreator()
            {
                return new SaveStateCreator();
            }

            public class SaveStateCreator : Object, IParcelableCreator
            {
                public Object CreateFromParcel(Parcel source)
                {
                    return new AnchorSavedState(source);
                }

                public Object[] NewArray(int size)
                {
                    return new AnchorSavedState[size];
                }
            }
        }
    }
}
