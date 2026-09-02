using UnityEngine;

namespace Deucarian.ViewerNavigation
{
    internal static class ViewerNavigationCommandExecutor
    {
        public static bool TryExecute(
            ViewerNavigationController target,
            ViewerNavigationCommand command,
            out string message)
        {
            message = null;
            if (command == null)
            {
                message = "Navigation command was empty.";
                return false;
            }

            bool hasSensitivity =
                command.TryGetGlobalSensitivity(out float sensitivity);
            if (hasSensitivity &&
                (float.IsNaN(sensitivity) || float.IsInfinity(sensitivity)))
            {
                message = "Navigation sensitivity must be a finite number.";
                return false;
            }

            bool handled = false;
            if (hasSensitivity)
            {
                target.SetGlobalSensitivity(sensitivity);
                handled = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Mode))
            {
                if (!TryParseMode(command.Mode, out ViewerNavigationMode mode))
                {
                    message = "Unsupported navigation mode: " + command.Mode;
                    return false;
                }

                target.SetNavigationMode(mode);
                handled = true;
            }

            if (!string.IsNullOrWhiteSpace(command.Action))
            {
                if (!TryExecuteAction(target, command.Action, out message))
                {
                    return false;
                }

                handled = true;
            }

            if (!string.IsNullOrWhiteSpace(command.View))
            {
                if (!TryNavigateToNamedView(target, command.View, out message))
                {
                    return false;
                }

                handled = true;
            }

            if (!handled)
            {
                message = "Navigation command did not include an action, mode, " +
                          "view, or sensitivity.";
                return false;
            }

            message = "Navigation command applied.";
            return true;
        }

        private static bool TryExecuteAction(
            ViewerNavigationController target,
            string action,
            out string message)
        {
            message = null;
            switch (Normalize(action))
            {
                case "returntoorigin":
                case "returnorigin":
                case "origin":
                case "reset":
                case "resetcamera":
                case "home":
                    target.ReturnToOrigin();
                    return true;
                case "topdown":
                case "top":
                case "topview":
                    target.SetTopDown(true);
                    return true;
                case "toggletopdown":
                case "toggletop":
                case "toggleviewtop":
                    target.ToggleTopDown();
                    return true;
                case "orbit":
                    target.SetNavigationMode(ViewerNavigationMode.Orbit);
                    return true;
                case "fly":
                    target.SetNavigationMode(ViewerNavigationMode.Fly);
                    return true;
                case "refreshdefaultpose":
                case "capturedefaultpose":
                    target.RefreshOrigin();
                    return true;
                default:
                    message = "Unsupported navigation action: " + action;
                    return false;
            }
        }

        private static bool TryNavigateToNamedView(
            ViewerNavigationController target,
            string view,
            out string message)
        {
            message = null;
            switch (Normalize(view))
            {
                case "top":
                case "topdown":
                    target.SetTopDown(true);
                    return true;
                case "bottom":
                    target.NavigateToDirection(Vector3.down);
                    return true;
                case "front":
                    target.NavigateToDirection(Vector3.back);
                    return true;
                case "back":
                    target.NavigateToDirection(Vector3.forward);
                    return true;
                case "left":
                    target.NavigateToDirection(Vector3.left);
                    return true;
                case "right":
                    target.NavigateToDirection(Vector3.right);
                    return true;
                case "frontlefttop":
                    target.NavigateToDirection(
                        (Vector3.back + Vector3.left + Vector3.up).normalized);
                    return true;
                case "frontrighttop":
                    target.NavigateToDirection(
                        (Vector3.back + Vector3.right + Vector3.up).normalized);
                    return true;
                case "backlefttop":
                    target.NavigateToDirection(
                        (Vector3.forward + Vector3.left + Vector3.up).normalized);
                    return true;
                case "backrighttop":
                    target.NavigateToDirection(
                        (Vector3.forward + Vector3.right + Vector3.up).normalized);
                    return true;
                default:
                    message = "Unsupported navigation view: " + view;
                    return false;
            }
        }

        private static bool TryParseMode(
            string text,
            out ViewerNavigationMode mode)
        {
            switch (Normalize(text))
            {
                case "orbit":
                    mode = ViewerNavigationMode.Orbit;
                    return true;
                case "fly":
                    mode = ViewerNavigationMode.Fly;
                    return true;
                default:
                    mode = default;
                    return false;
            }
        }

        private static string Normalize(string value)
        {
            return (value ?? string.Empty)
                .Trim()
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace(" ", string.Empty)
                .ToLowerInvariant();
        }
    }
}
