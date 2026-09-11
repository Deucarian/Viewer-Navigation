using UnityEngine;
using System.Threading;
using System.Threading.Tasks;
using Deucarian.CameraNavigation;
namespace Deucarian.ViewerNavigation.Samples.SimpleUsage
{
    public sealed class SimpleUsageExample : MonoBehaviour
    {
        [SerializeField] private ViewerNavigationHost navigation;
        [SerializeField] private ViewerNavigationMode mode = ViewerNavigationMode.Orbit;
        public bool ApplyMode() => navigation.SetMode(mode);
        public Task<CameraMoveResult> HomeAsync(CancellationToken cancellationToken = default) =>
            navigation.ReturnToOriginAsync(cancellationToken);
    }
}
