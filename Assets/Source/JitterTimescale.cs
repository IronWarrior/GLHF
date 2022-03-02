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
        public float timescaleMinimum = 0.7f;
        public float timescaleMaximum = 1.2f;

        public float CalculateTimescale(float deltaTime, int currentBufferSize, float rttStandardDeviation)
        {
            float targetBufferSize = TargetBufferSize(deltaTime, rttStandardDeviation);

            if (targetBufferSize < 0.0001f)
            {
                return 1;
            }
            else
            {

                float bufferPercent = currentBufferSize / targetBufferSize;
                float timescale = UnityEngine.Mathf.Clamp(bufferPercent, timescaleMinimum, timescaleMaximum);

                return timescale;
            }
        }

        public float TargetBufferSize(float deltaTime, float rttStandardDeviation)
        {
            return (float)System.Math.Round(rttStandardDeviation / deltaTime);
        }
    }
}
