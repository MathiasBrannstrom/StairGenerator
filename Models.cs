namespace StairGenerator
{
    public enum StairType
    {
        Single,
        LinearStairwell,
        RectangularSpiral
    }

    public enum SpiralDirection
    {
        Forward = 0,  // +Z
        Left = 1,     // -X
        Backward = 2, // -Z
        Right = 3     // +X
    }

    public class StairLevel
    {
        public int StepCount { get; set; }
        
        public StairLevel(int stepCount = 8)
        {
            StepCount = stepCount;
        }
    }
}