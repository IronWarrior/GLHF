namespace GLHF
{
    /// <summary>
    /// Calculates a multiplier that can be applied to a delta time accumulator
    /// to speed up or slow down the simulation based on how close the inputted current
    /// tick buffer size is to the goal buffer size.
    /// This is important since messages will not always arrive exactly delta time apart,
    /// so it is necessary to sometimes catch up or slow down to keep a stable simulation.
    /// </summary>
    [System.Serializable]
    public class JitterTimescale
    {
        public float CalculateTimescale(float deltaTime, int currentBufferSize, float rttStandardDeviation)
        {
            int targetBufferSize = TargetBufferSize(deltaTime, rttStandardDeviation);
            float t = (float)currentBufferSize / targetBufferSize;

            // TODO: This should be placed in a curve or function something.
            float timescale;

            if (t < 0.5f)
            {
                timescale = 0.7f;
            }
            else if (t < 1)
            {
                timescale = 0.9f;
            }
            else if (t > 1.05f)
            {
                timescale = 1.05f;
            }
            else
            {
                timescale = 1;
            }

            return timescale;
        }

        public int TargetBufferSize(float deltaTime, float rttStandardDeviation)
        {
            return (int)System.Math.Round(rttStandardDeviation / deltaTime);
        }
    }
}
