namespace Toggl.Giskard.Adapters.Calendar
{
    public struct AnchorData
    {
        public int AdapterPosition { get; }
        public int TopOffset { get; }
        public int LeftOffset { get; }
        public int Height { get; }
        public int Width { get; }

        public AnchorData(int adapterPosition, int topOffset, int leftOffset, int height, int width)
        {
            AdapterPosition = adapterPosition;
            TopOffset = topOffset;
            LeftOffset = leftOffset;
            Height = height;
            Width = width;
        }
    }
}
