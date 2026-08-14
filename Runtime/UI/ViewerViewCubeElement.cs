using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation.UI
{
    public sealed class ViewerViewCubeElement : VisualElement
    {
        public const string ElementName = "DeucarianViewerViewCube";
        private const float ElementSize = 112f;
        private const float FaceSize = 32f;
        private const float ProjectionRadius = 31f;

        private readonly Dictionary<ViewerViewFace, Button> buttons =
            new Dictionary<ViewerViewFace, Button>();
        private readonly List<FaceDepth> depths = new List<FaceDepth>(6);
        private ViewerViewFace activeFace = ViewerViewFace.Front;
        private Color surfaceColor = new Color(0.12f, 0.17f, 0.22f, 0.94f);
        private Color textColor = new Color(0.82f, 0.88f, 0.92f, 1f);
        private Color accentColor = new Color(0.10f, 0.72f, 0.74f, 0.98f);

        public event Action<ViewerViewFace> FaceSelected;
        public ViewerViewFace ActiveFace => activeFace;

        public ViewerViewCubeElement()
        {
            name = ElementName;
            pickingMode = PickingMode.Position;
            focusable = false;
            style.position = Position.Absolute;
            style.width = ElementSize;
            style.height = ElementSize;
            style.minWidth = ElementSize;
            style.minHeight = ElementSize;
            style.borderTopLeftRadius = 20f;
            style.borderTopRightRadius = 20f;
            style.borderBottomLeftRadius = 20f;
            style.borderBottomRightRadius = 20f;
            style.backgroundColor = new Color(0.055f, 0.075f, 0.1f, 0.82f);

            Array faces = Enum.GetValues(typeof(ViewerViewFace));
            foreach (ViewerViewFace face in faces)
            {
                Button button = CreateFaceButton(face);
                buttons.Add(face, button);
                Add(button);
            }

            UpdateOrientation(Quaternion.identity);
        }

        public void UpdateOrientation(Quaternion cameraRotation)
        {
            Quaternion inverseCamera = Quaternion.Inverse(cameraRotation);
            Vector3 viewDirectionFromTarget = -(cameraRotation * Vector3.forward);
            float bestDot = float.NegativeInfinity;
            ViewerViewFace bestFace = activeFace;
            depths.Clear();

            foreach (KeyValuePair<ViewerViewFace, Button> pair in buttons)
            {
                Vector3 worldDirection =
                    ViewerViewFacePolicy.GetDirectionFromTargetToCamera(pair.Key);
                Vector3 local = inverseCamera * worldDirection;
                float left = ElementSize * 0.5f + local.x * ProjectionRadius - FaceSize * 0.5f;
                float top = ElementSize * 0.5f - local.y * ProjectionRadius - FaceSize * 0.5f;
                pair.Value.style.left = left;
                pair.Value.style.top = top;
                pair.Value.style.opacity = Mathf.Lerp(0.38f, 1f, (local.z + 1f) * 0.5f);
                depths.Add(new FaceDepth(pair.Value, local.z));

                float dot = Vector3.Dot(viewDirectionFromTarget.normalized, worldDirection);
                if (dot > bestDot)
                {
                    bestDot = dot;
                    bestFace = pair.Key;
                }
            }

            depths.Sort((left, right) => left.Depth.CompareTo(right.Depth));
            for (int i = 0; i < depths.Count; i++)
            {
                depths[i].Button.BringToFront();
            }

            SetActiveFace(bestFace);
        }

        public void SetActiveFace(ViewerViewFace face)
        {
            activeFace = face;
            foreach (KeyValuePair<ViewerViewFace, Button> pair in buttons)
            {
                bool selected = pair.Key == face;
                pair.Value.style.backgroundColor = selected
                    ? accentColor
                    : surfaceColor;
                pair.Value.style.color = textColor;
            }
        }

        public void SelectFace(ViewerViewFace face)
        {
            if (!buttons.ContainsKey(face))
            {
                return;
            }

            SetActiveFace(face);
            FaceSelected?.Invoke(face);
        }

        public void ApplyPalette(Color surface, Color text, Color accent)
        {
            surfaceColor = new Color(surface.r, surface.g, surface.b, 0.96f);
            textColor = text;
            accentColor = accent;
            style.backgroundColor = new Color(surface.r, surface.g, surface.b, 0.86f);
            SetActiveFace(activeFace);
        }

        public Button GetFaceButton(ViewerViewFace face)
        {
            return buttons.TryGetValue(face, out Button button) ? button : null;
        }

        private Button CreateFaceButton(ViewerViewFace face)
        {
            Button button = new Button
            {
                name = face + "Face",
                text = ViewerViewFacePolicy.GetLabel(face),
                tooltip = "View " + face.ToString().ToLowerInvariant() + " face",
                userData = face,
                focusable = true,
                pickingMode = PickingMode.Position
            };
            button.style.position = Position.Absolute;
            button.style.width = FaceSize;
            button.style.height = FaceSize;
            button.style.minWidth = FaceSize;
            button.style.minHeight = FaceSize;
            button.style.paddingLeft = 0f;
            button.style.paddingRight = 0f;
            button.style.paddingTop = 0f;
            button.style.paddingBottom = 0f;
            button.style.fontSize = face == ViewerViewFace.Top || face == ViewerViewFace.Bottom
                ? 8f
                : 11f;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            button.style.borderTopLeftRadius = 8f;
            button.style.borderTopRightRadius = 8f;
            button.style.borderBottomLeftRadius = 8f;
            button.style.borderBottomRightRadius = 8f;
            button.RegisterCallback<PointerDownEvent>(StopPointerEvent);
            button.RegisterCallback<PointerUpEvent>(StopPointerEvent);
            button.RegisterCallback<ClickEvent>(OnFaceClicked);
            return button;
        }

        private static void StopPointerEvent(EventBase evt)
        {
            evt.StopImmediatePropagation();
        }

        private void OnFaceClicked(ClickEvent evt)
        {
            if (evt.currentTarget is Button button &&
                button.userData is ViewerViewFace face)
            {
                SelectFace(face);
            }

            evt.StopImmediatePropagation();
        }

        private readonly struct FaceDepth
        {
            public FaceDepth(Button button, float depth)
            {
                Button = button;
                Depth = depth;
            }

            public Button Button { get; }
            public float Depth { get; }
        }
    }
}
