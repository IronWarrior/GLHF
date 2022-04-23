namespace GLHF
{
    /// <summary>
    /// Calculates a multiplier that can be applied to a delta time accumulator
    /// to speed up or slow down the simulation based on some calculated error.
    /// </summary>
    [System.Serializable]
    public class JitterTimescale
    {
        public float fastForwardTimescale = 1.05f;
        public float slowDownTimescale = 0.95f;

        public float errorThreshold = 0.025f;

        public float CalculateTimescale(float error)
        {
            float timescale;

            if (error > errorThreshold)
            {
                timescale = fastForwardTimescale;
            }
            else if (error < -errorThreshold)
            {
                timescale = slowDownTimescale;
            }
            else
            {
                timescale = 1;
            }

            return timescale;
        }
    }
}
