using System;
using UnityEngine;
namespace Deucarian.ViewerNavigation
{
    public sealed class ViewerNavigationTrigger : MonoBehaviour
    {
        [SerializeField] private ViewerNavigationHost host;
        [SerializeField] private ViewerNavigationMode mode = ViewerNavigationMode.Orbit;
        private ViewerNavigationHost Host => host != null ? host : throw new InvalidOperationException("Assign a configured ViewerNavigationHost to this ViewerNavigationTrigger.");
        public void ApplyMode() => Host.SetMode(mode);
        public async void ReturnToOrigin() => await Host.ReturnToOriginAsync();
    }
}
