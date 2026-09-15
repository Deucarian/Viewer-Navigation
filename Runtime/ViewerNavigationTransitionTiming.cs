namespace Deucarian.ViewerNavigation
{
    /// <summary>Single timing source for every automatic viewer camera transition.</summary>
    public static class ViewerNavigationTransitionTiming
    {
        // 1.5 times the previous one-second media focus speed.
        public const float DurationSeconds = 2f / 3f;
    }
}
