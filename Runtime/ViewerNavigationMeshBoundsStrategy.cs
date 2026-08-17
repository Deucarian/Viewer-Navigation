using Deucarian.CameraNavigation;
using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    /// <summary>
    /// Mesh-renderer based reference bounds policy shared by production navigation consumers.
    /// </summary>
    public sealed class DeucarianMeshBoundsStrategy :
        IDeucarianFramingBoundsStrategy<GameObject>
    {
        public bool TryGetBounds(GameObject source, out Bounds bounds)
        {
            bounds = default;
            if (source == null)
            {
                return false;
            }

            MeshRenderer[] renderers =
                source.GetComponentsInChildren<MeshRenderer>(true);
            bool hasBounds = false;
            for (int index = 0; index < renderers.Length; index++)
            {
                MeshRenderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }
    }
}
