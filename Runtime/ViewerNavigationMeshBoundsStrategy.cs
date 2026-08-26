using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    /// <summary>
    /// Mesh-renderer based reference bounds policy shared by production navigation
    /// consumers.
    /// </summary>
    public sealed class ViewerNavigationMeshBoundsStrategy :
        IDeucarianFramingBoundsStrategy<GameObject>
    {
        public bool TryGetBounds(GameObject source, out Bounds bounds)
        {
            return ViewerNavigationMeshBoundsCalculator.TryCalculate(
                source != null ? source.transform : null,
                true,
                out bounds,
                out _,
                out _);
        }
    }
}
