namespace dummy_api.Services;

public class RandomService
{
    Random rand = new Random(0);

    public long Next(int maxValue) => rand.NextInt64(maxValue);
    public void Reset(int seed)
    {
        rand = new Random(seed);
    }
}