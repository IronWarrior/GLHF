namespace GLHF
{
    /// <summary>
    /// Calculates a multiplier that can be applied to a delta time accumulator
    /// to speed up or slow down the simulation based on some calculated error.
    /// </summary>
    [System.Serializable]
    public class JitterTimescale
    {
        // TODO: Should prolly just make a simple stepped function class for this stuff.
        public float fastForwardTimescale = 1.05f;
        public float slowDownTimescale = 0.95f;

        public float errorThreshold = 0.025f;

        public float stopThreshold = 0.1f;

        public float CalculateTimescale(float error)
        {
            float timescale;

            if (error > errorThreshold)
            {
                timescale = fastForwardTimescale;
            }
            else if (error < -stopThreshold)
            {
                timescale = 0.5f;
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
