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

    public int Next(int minValue, int maxValue) => wrapping.Next(minValue, maxValue);
    public int Next(int maxValue) => wrapping.Next(maxValue);
    public int Next() => wrapping.Next();
}