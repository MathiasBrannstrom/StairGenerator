namespace StairGenerator
{
    public enum StairType
    {
        LinearStairwell
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