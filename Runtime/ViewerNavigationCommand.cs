using System;

namespace Deucarian.ViewerNavigation
{
    /// <summary>
    /// Transport-neutral request for the authoritative viewer navigation
    /// controller. Command routing and wire parsing remain application-owned.
    /// </summary>
    [Serializable]
    public sealed class ViewerNavigationCommand
    {
        public string Action { get; set; }
        public string Mode { get; set; }
        public string View { get; set; }
        public float? Sensitivity { get; set; }
        public float? GlobalSensitivity { get; set; }

        public bool TryGetGlobalSensitivity(out float sensitivity)
        {
            if (GlobalSensitivity.HasValue)
            {
                sensitivity = GlobalSensitivity.Value;
                return true;
            }

            if (Sensitivity.HasValue)
            {
                sensitivity = Sensitivity.Value;
                return true;
            }

            sensitivity = default;
            return false;
        }
    }
}
