using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Deucarian.ViewerNavigation
{
    /// <summary>
    /// Blocks navigation input when Unity UI is handling user intent.
    /// </summary>
    public sealed class ViewerNavigationUiInputBlocker : IViewerNavigationInputBlocker
    {
        public bool IsPointerInputBlocked(Vector2 screenPosition)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null && eventSystem.IsPointerOverGameObject())
            {
                return true;
            }

            UIDocument[] documents =
                Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int index = 0; index < documents.Length; index++)
            {
                VisualElement root = documents[index]?.rootVisualElement;
                if (root?.panel == null)
                {
                    continue;
                }

                Vector2 panelPosition = RuntimePanelUtils.ScreenToPanel(
                    root.panel,
                    new Vector2(
                        screenPosition.x,
                        Screen.height - screenPosition.y));
                VisualElement picked = root.panel.Pick(panelPosition);
                if (picked != null &&
                    picked != root.panel.visualTree &&
                    picked.pickingMode != PickingMode.Ignore)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsKeyboardInputBlocked()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem != null &&
                eventSystem.currentSelectedGameObject != null)
            {
                return true;
            }

            UIDocument[] documents =
                Object.FindObjectsByType<UIDocument>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int index = 0; index < documents.Length; index++)
            {
                VisualElement focusedElement = documents[index]
                    ?.rootVisualElement
                    ?.panel
                    ?.focusController
                    ?.focusedElement as VisualElement;
                if (IsKeyboardInteractiveElement(focusedElement))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsKeyboardInteractiveElement(VisualElement element)
        {
            for (VisualElement current = element;
                 current != null;
                 current = current.parent)
            {
                if (current.focusable && current.enabledInHierarchy)
                {
                    return true;
                }

                string typeName = current.GetType().Name;
                if (typeName.Contains("TextField") ||
                    typeName.Contains("Popup") ||
                    typeName.Contains("Dropdown") ||
                    typeName.Contains("Field"))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
