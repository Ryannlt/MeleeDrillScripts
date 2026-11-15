namespace MDS.Shared
{
    public static class RandomExtensions
    {
        // From a seeded System.Random, so a test can repeat the draw.
        public static float Between(this System.Random random, float from, float to) => from + (to - from) * (float)random.NextDouble();
    }
}
