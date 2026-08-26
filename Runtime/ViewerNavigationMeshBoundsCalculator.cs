using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    internal static class ViewerNavigationMeshBoundsCalculator
    {
        internal static bool TryCalculate(
            Transform root,
            bool includeInactive,
            out Bounds worldBounds,
            out int rendererCount,
            out int inactiveRendererCount)
        {
            worldBounds = default;
            rendererCount = 0;
            inactiveRendererCount = 0;
            if (root == null)
            {
                return false;
            }

            MeshRenderer[] renderers =
                root.GetComponentsInChildren<MeshRenderer>(includeInactive);
            bool hasBounds = false;
            for (int index = 0; index < renderers.Length; index++)
            {
                MeshRenderer renderer = renderers[index];
                if (renderer == null)
                {
                    continue;
                }

                rendererCount++;
                if (!renderer.gameObject.activeInHierarchy)
                {
                    inactiveRendererCount++;
                }

                if (!hasBounds)
                {
                    worldBounds = renderer.bounds;
                    hasBounds = true;
                }
                else
                {
                    worldBounds.Encapsulate(renderer.bounds);
                }
            }

            return hasBounds;
        }
    }
}
