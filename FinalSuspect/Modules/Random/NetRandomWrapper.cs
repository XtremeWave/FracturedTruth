namespace FinalSuspect.Modules.Random;

public class NetRandomWrapper(System.Random instance) : IRandom
{
    public System.Random wrapping = instance;

    public NetRandomWrapper() : this(new System.Random())
    {
    }

    public NetRandomWrapper(int seed) : this(new System.Random(seed))
    {
    }

    public int Next(int minValue, int maxValue)
    {
        return wrapping.Next(minValue, maxValue);
    }

    public int Next(int maxValue)
    {
        return wrapping.Next(maxValue);
    }

    public int Next()
    {
        return wrapping.Next();
    }
}