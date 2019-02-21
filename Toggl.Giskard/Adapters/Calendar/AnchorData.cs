namespace Toggl.Giskard.Adapters.Calendar
{
    public struct AnchorData
    {
        public int adapterPosition;
        public int topOffset;
        public int leftOffset;
        public int height;
        public int width;

        public AnchorData(int adapterPosition, int topOffset, int leftOffset, int height, int width)
        {
            this.adapterPosition = adapterPosition;
            this.topOffset = topOffset;
            this.leftOffset = leftOffset;
            this.height = height;
            this.width = width;
        }
    }
}
