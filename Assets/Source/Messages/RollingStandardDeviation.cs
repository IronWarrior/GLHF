// TODO: Update with rolling window standard deviation for performance.
public class RollingStandardDeviation
{
    private int count;
    private int index;

    private readonly float[] values;

    public RollingStandardDeviation(int window)
    {
        values = new float[window];
    }

    public void Insert(float f)
    {
        count++;

        if (count > values.Length)
            count = values.Length;

        values[index] = f;

        index = (index + 1) % values.Length;
    }

    public float CalculateStandardDeviation()
    {
        if (count == 0)
            return 0;

        float mean = 0;

        for (int i = 0; i < count; i++)
        {
            mean += values[i];
        }

        mean /= count;

        float sum = 0;

        for (int i = 0; i < count; i++)
        {
            sum += (float)System.Math.Pow(values[i] - mean, 2);
        }

        sum /= count;

        return (float)System.Math.Sqrt(sum);
    }
}
