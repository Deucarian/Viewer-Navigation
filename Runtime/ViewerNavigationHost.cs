using System;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    /// <summary>Simple requests through the viewer's existing authoritative controller.</summary>
    [DisallowMultipleComponent]
    public sealed class ViewerNavigationHost : MonoBehaviour
    {
        [SerializeField] private ViewerNavigationController controller;
        private readonly CancellationTokenSource lifetime = new CancellationTokenSource();
        private bool destroyed;

        public void Configure(ViewerNavigationController value)
        {
            if (destroyed) throw new ObjectDisposedException(nameof(ViewerNavigationHost));
            if (controller != null) throw new InvalidOperationException("ViewerNavigationHost '" + name + "' already has a controller. Configure it once.");
            controller = value != null ? value : throw new ArgumentNullException(nameof(value));
        }
        public bool SetMode(ViewerNavigationMode mode)
        {
            if (mode != ViewerNavigationMode.Orbit && mode != ViewerNavigationMode.Fly)
                throw new ArgumentOutOfRangeException(nameof(mode), "Select Orbit or Fly from ViewerNavigationMode.");
            return Controller.SetNavigationMode(mode);
        }
        public async Task<CameraMoveResult> ReturnToOriginAsync(CancellationToken cancellationToken = default)
        {
            var owner = Controller;
            using (var cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token, cancellationToken))
                return await owner.ReturnToOriginAsync(cancellationToken: cancellation.Token);
        }
        private ViewerNavigationController Controller
        {
            get
            {
                if (destroyed) throw new ObjectDisposedException(nameof(ViewerNavigationHost));
                if (controller == null || !controller.IsInitialized || controller.Camera == null)
                    throw new InvalidOperationException("ViewerNavigationHost '" + name + "' needs an initialized ViewerNavigationController. Assign the viewer's configured controller in the Inspector or during startup.");
                return controller;
            }
        }
        private void OnDestroy() { destroyed = true; lifetime.Cancel(); lifetime.Dispose(); controller = null; }
    }
}
