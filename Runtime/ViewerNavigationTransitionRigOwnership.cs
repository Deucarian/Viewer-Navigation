using Deucarian.CameraNavigation.InputSystemIntegration;

namespace Deucarian.ViewerNavigation
{
    /// <summary>Temporarily releases passive navigation's camera writer during an explicit move.</summary>
    internal sealed class ViewerNavigationTransitionRigOwnership
    {
        private bool suspended;
        private bool enabledBeforeTransition;

        public void Update(DeucarianInputSystemCameraNavigationRig rig, bool transitioning)
        {
            if (rig == null) return;
            if (transitioning && !suspended)
            {
                enabledBeforeTransition = rig.enabled;
                suspended = true;
                rig.enabled = false;
            }
            else if (!transitioning && suspended)
            {
                suspended = false;
                rig.enabled = enabledBeforeTransition;
            }
        }
    }
}
