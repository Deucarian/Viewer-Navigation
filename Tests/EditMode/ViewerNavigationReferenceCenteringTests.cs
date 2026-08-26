using NUnit.Framework;
using UnityEngine;

namespace Deucarian.ViewerNavigation.Tests
{
    public sealed class ViewerNavigationReferenceCenteringTests
    {
        private const float Tolerance = 0.0001f;

        [Test]
        public void CentersAsymmetricOffOriginMeshBoundsUsingSharedPolicy()
        {
            GameObject root = new GameObject("Reference");
            try
            {
                root.transform.position = new Vector3(4f, 2f, -3f);
                CreateCube(
                    root.transform,
                    "Large",
                    new Vector3(12f, 1f, -4f),
                    new Vector3(5f, 2f, 3f));
                CreateCube(
                    root.transform,
                    "Small",
                    new Vector3(-3f, 4f, 7f),
                    new Vector3(1f, 4f, 2f));
                var strategy = new ViewerNavigationMeshBoundsStrategy();
                Assert.That(strategy.TryGetBounds(root, out Bounds before), Is.True);

                ViewerNavigationReferenceCenteringResult result =
                    ViewerNavigationReferenceCentering
                        .CenterMeshRendererBoundsAtWorldOrigin(root.transform);

                Assert.That(result.Applied, Is.True, result.Message);
                Assert.That(result.RendererCount, Is.EqualTo(2));
                Assert.That(result.InactiveRendererCount, Is.Zero);
                AssertBoundsApproximately(before, result.WorldBoundsBefore);
                AssertVectorApproximately(-before.center, result.AppliedWorldOffset);
                Assert.That(result.IsCenteredAtWorldOrigin, Is.True);
                AssertVectorApproximately(Vector3.zero, result.WorldBoundsAfter.center);
                Assert.That(strategy.TryGetBounds(root, out Bounds after), Is.True);
                AssertBoundsApproximately(after, result.WorldBoundsAfter);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void IncludesInactiveMeshRenderersWithoutActivatingThem()
        {
            GameObject root = new GameObject("Reference");
            try
            {
                GameObject active = CreateCube(
                    root.transform,
                    "Active",
                    Vector3.zero,
                    Vector3.one);
                GameObject inactive = CreateCube(
                    root.transform,
                    "Inactive",
                    new Vector3(10f, 0f, 0f),
                    Vector3.one);
                inactive.SetActive(false);

                ViewerNavigationReferenceCenteringResult result =
                    ViewerNavigationReferenceCentering
                        .CenterMeshRendererBoundsAtWorldOrigin(
                            root.transform,
                            includeInactive: true);

                Assert.That(result.Applied, Is.True, result.Message);
                Assert.That(result.RendererCount, Is.EqualTo(2));
                Assert.That(result.InactiveRendererCount, Is.EqualTo(1));
                AssertVectorApproximately(
                    new Vector3(5f, 0f, 0f),
                    result.WorldBoundsBefore.center);
                Assert.That(active.activeSelf, Is.True);
                Assert.That(inactive.activeSelf, Is.False);
                Assert.That(inactive.activeInHierarchy, Is.False);
                Assert.That(result.IsCenteredAtWorldOrigin, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void MissingRootOrMeshRendererDoesNotMutateReference()
        {
            ViewerNavigationReferenceCenteringResult missing =
                ViewerNavigationReferenceCentering
                    .CenterMeshRendererBoundsAtWorldOrigin(null);
            Assert.That(missing.Applied, Is.False);
            Assert.That(missing.Message, Is.Not.Empty);

            GameObject root = new GameObject("Renderer-Free Reference");
            try
            {
                root.transform.SetPositionAndRotation(
                    new Vector3(7f, -2f, 11f),
                    Quaternion.Euler(13f, 29f, -4f));
                Vector3 position = root.transform.position;
                Quaternion rotation = root.transform.rotation;
                Vector3 scale = root.transform.localScale;

                ViewerNavigationReferenceCenteringResult result =
                    ViewerNavigationReferenceCentering
                        .CenterMeshRendererBoundsAtWorldOrigin(root.transform);

                Assert.That(result.Applied, Is.False);
                Assert.That(result.Message, Is.Not.Empty);
                Assert.That(root.transform.position, Is.EqualTo(position));
                Assert.That(root.transform.rotation, Is.EqualTo(rotation));
                Assert.That(root.transform.localScale, Is.EqualTo(scale));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ReportsFinalLocalPositionUnderNontrivialParentTransform()
        {
            GameObject parent = new GameObject("Parent");
            GameObject root = new GameObject("Reference");
            try
            {
                parent.transform.SetPositionAndRotation(
                    new Vector3(8f, -3f, 5f),
                    Quaternion.Euler(17f, 41f, -9f));
                parent.transform.localScale = new Vector3(2f, 3f, 0.5f);
                root.transform.SetParent(parent.transform, false);
                root.transform.localPosition = new Vector3(4f, -2f, 6f);
                root.transform.localRotation = Quaternion.Euler(7f, 23f, 5f);
                root.transform.localScale = new Vector3(1.25f, 0.75f, 2f);
                CreateCube(
                    root.transform,
                    "Offset Mesh",
                    new Vector3(9f, 3f, -5f),
                    new Vector3(3f, 5f, 2f));

                Vector3 worldPositionBefore = root.transform.position;
                Quaternion localRotationBefore = root.transform.localRotation;
                Vector3 localScaleBefore = root.transform.localScale;
                var strategy = new ViewerNavigationMeshBoundsStrategy();
                Assert.That(strategy.TryGetBounds(root, out Bounds before), Is.True);
                Vector3 expectedWorldPositionAfter =
                    worldPositionBefore - before.center;
                Vector3 expectedLocalPositionAfter =
                    parent.transform.InverseTransformPoint(expectedWorldPositionAfter);

                ViewerNavigationReferenceCenteringResult result =
                    ViewerNavigationReferenceCentering
                        .CenterMeshRendererBoundsAtWorldOrigin(root.transform);

                Assert.That(result.Applied, Is.True, result.Message);
                AssertVectorApproximately(
                    worldPositionBefore,
                    result.ReferenceRootWorldPositionBefore);
                AssertVectorApproximately(
                    expectedWorldPositionAfter,
                    result.ReferenceRootWorldPositionAfter);
                AssertVectorApproximately(
                    expectedLocalPositionAfter,
                    result.ReferenceRootLocalPositionAfter);
                AssertVectorApproximately(
                    result.ReferenceRootLocalPositionAfter,
                    root.transform.localPosition);
                AssertQuaternionApproximately(
                    localRotationBefore,
                    root.transform.localRotation);
                AssertVectorApproximately(localScaleBefore, root.transform.localScale);
                Assert.That(result.IsCenteredAtWorldOrigin, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(parent);
            }
        }

        private static GameObject CreateCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.SetParent(parent, false);
            cube.transform.localPosition = localPosition;
            cube.transform.localScale = localScale;
            return cube;
        }

        private static void AssertBoundsApproximately(
            Bounds expected,
            Bounds actual)
        {
            AssertVectorApproximately(expected.center, actual.center);
            AssertVectorApproximately(expected.size, actual.size);
        }

        private static void AssertVectorApproximately(
            Vector3 expected,
            Vector3 actual)
        {
            Assert.That(
                Vector3.Distance(expected, actual),
                Is.LessThan(Tolerance),
                "Expected " + expected + " but got " + actual + ".");
        }

        private static void AssertQuaternionApproximately(
            Quaternion expected,
            Quaternion actual)
        {
            Assert.That(
                Quaternion.Angle(expected, actual),
                Is.LessThan(Tolerance),
                "Expected " + expected.eulerAngles +
                " but got " + actual.eulerAngles + ".");
        }
    }
}
