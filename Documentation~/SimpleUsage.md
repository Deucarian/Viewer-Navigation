# Simple usage

Copy the reference example into your project, add SimpleUsageExample and assign its scoped host references. Its serialized definition fields use the same typed keys as code.

Initialize ViewerNavigationController using the existing bootstrap and configure the host with that controller. Define reference bounds/origin on the controller before returning home. Modes already use an enum and serialize as a dropdown, so there is no string-key layer. The controller retains input gates, transitions, diagnostics and cancellation.

Definitions are authored once in SampleDefinitions.cs where applicable; the caller never invents an ID. Replace the sample set with your project's central definitions. A selected key proves its identity and payload type; startup still needs to bind that definition in the correct scope. Missing configuration reports how to fix it. Dynamic targets and choices are issued by their owner instead of selected from a definition dropdown.
```csharp
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
```
