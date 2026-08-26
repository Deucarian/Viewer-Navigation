using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    /// <summary>
    /// Explicit reference-placement operations shared by viewer composition roots.
    /// These operations never change object visibility or navigation state.
    /// </summary>
    public static class ViewerNavigationReferenceCentering
    {
        public static ViewerNavigationReferenceCenteringResult
            CenterMeshRendererBoundsAtWorldOrigin(
                Transform referenceRoot,
                bool includeInactive = true)
        {
            if (referenceRoot == null)
            {
                return ViewerNavigationReferenceCenteringResult.NotApplied(
                    "Reference root is missing.");
            }

            if (!ViewerNavigationMeshBoundsCalculator.TryCalculate(
                    referenceRoot,
                    includeInactive,
                    out Bounds worldBoundsBefore,
                    out int rendererCount,
                    out int inactiveRendererCount))
            {
                return ViewerNavigationReferenceCenteringResult.NotApplied(
                    "Reference root has no MeshRenderers.");
            }

            Vector3 worldPositionBefore = referenceRoot.position;
            Vector3 appliedWorldOffset = -worldBoundsBefore.center;
            referenceRoot.position = worldPositionBefore + appliedWorldOffset;

            if (!ViewerNavigationMeshBoundsCalculator.TryCalculate(
                    referenceRoot,
                    includeInactive,
                    out Bounds worldBoundsAfter,
                    out _,
                    out _))
            {
                referenceRoot.position = worldPositionBefore;
                return ViewerNavigationReferenceCenteringResult.NotApplied(
                    "Reference bounds could not be measured after centering.");
            }

            return ViewerNavigationReferenceCenteringResult.CreateApplied(
                rendererCount,
                inactiveRendererCount,
                worldBoundsBefore,
                worldBoundsAfter,
                worldPositionBefore,
                referenceRoot.position,
                referenceRoot.localPosition,
                appliedWorldOffset);
        }
    }
}
