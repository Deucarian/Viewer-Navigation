using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    public sealed class ViewerNavigationReferenceCenteringResult
    {
        private ViewerNavigationReferenceCenteringResult(
            bool applied,
            int rendererCount,
            int inactiveRendererCount,
            Bounds worldBoundsBefore,
            Bounds worldBoundsAfter,
            Vector3 referenceRootWorldPositionBefore,
            Vector3 referenceRootWorldPositionAfter,
            Vector3 referenceRootLocalPositionAfter,
            Vector3 appliedWorldOffset,
            string message)
        {
            Applied = applied;
            RendererCount = rendererCount;
            InactiveRendererCount = inactiveRendererCount;
            WorldBoundsBefore = worldBoundsBefore;
            WorldBoundsAfter = worldBoundsAfter;
            ReferenceRootWorldPositionBefore = referenceRootWorldPositionBefore;
            ReferenceRootWorldPositionAfter = referenceRootWorldPositionAfter;
            ReferenceRootLocalPositionAfter = referenceRootLocalPositionAfter;
            AppliedWorldOffset = appliedWorldOffset;
            Message = message ?? string.Empty;
        }

        public bool Applied { get; }
        public int RendererCount { get; }
        public int InactiveRendererCount { get; }
        public Bounds WorldBoundsBefore { get; }
        public Bounds WorldBoundsAfter { get; }
        public Vector3 ReferenceRootWorldPositionBefore { get; }
        public Vector3 ReferenceRootWorldPositionAfter { get; }
        public Vector3 ReferenceRootLocalPositionAfter { get; }
        public Vector3 AppliedWorldOffset { get; }
        public string Message { get; }
        public bool IsCenteredAtWorldOrigin =>
            Applied && WorldBoundsAfter.center.sqrMagnitude <= 0.0001f;

        internal static ViewerNavigationReferenceCenteringResult CreateApplied(
            int rendererCount,
            int inactiveRendererCount,
            Bounds worldBoundsBefore,
            Bounds worldBoundsAfter,
            Vector3 referenceRootWorldPositionBefore,
            Vector3 referenceRootWorldPositionAfter,
            Vector3 referenceRootLocalPositionAfter,
            Vector3 appliedWorldOffset)
        {
            return new ViewerNavigationReferenceCenteringResult(
                true,
                rendererCount,
                inactiveRendererCount,
                worldBoundsBefore,
                worldBoundsAfter,
                referenceRootWorldPositionBefore,
                referenceRootWorldPositionAfter,
                referenceRootLocalPositionAfter,
                appliedWorldOffset,
                "Centered reference MeshRenderer bounds on the world origin.");
        }

        internal static ViewerNavigationReferenceCenteringResult NotApplied(
            string message)
        {
            return new ViewerNavigationReferenceCenteringResult(
                false,
                0,
                0,
                default,
                default,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                Vector3.zero,
                message);
        }
    }
}
