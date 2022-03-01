// Knuth numerically stable standard deviation,
// sourced from: https://gist.github.com/musically-ut/1502045/106af3cf8bd4db0c8581218759040b058da778d3
public class RollingStandardDeviation
{
    private long count;

    private float previousMean, currentMean;
    private float previousS, currentS;
    private float currentVariance;

    public void Insert(float f)
    {
        count++;

        if (count == 1)
        {
            // Set the very first values.
            currentMean = f;
            currentS = 0;
            currentVariance = currentS;
        }
        else
        {
            // Save the previous values.
            previousMean = currentMean;
            previousS = currentS;

            // Update the current values.
            currentMean = previousMean + (f - previousMean) / count;
            currentS = previousS + (f - previousMean) * (f - currentMean);
            currentVariance = currentS / (count - 1);
        }
    }

    public float CalculateStandardDeviation()
    {
        return (float)System.Math.Sqrt(currentVariance);
    }
}
