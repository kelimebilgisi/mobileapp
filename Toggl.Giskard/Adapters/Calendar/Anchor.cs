using System;

namespace Toggl.Giskard.Adapters.Calendar
{
    public class Anchor : Java.Lang.Object
    {
        private readonly int height;
        private readonly AnchorData[] anchoredData;

        public AnchorData[] AnchoredData => anchoredData;

        public int Height => height;

        public Anchor(int height)
        {
            this.height = height;
            anchoredData = Array.Empty<AnchorData>();
        }

    }
}
