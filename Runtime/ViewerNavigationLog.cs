using Deucarian.Logging;

namespace Deucarian.ViewerNavigation
{
    internal static class ViewerNavigationLog
    {
        public static readonly DLog Navigation =
            DLog.For("ViewerNavigation.Navigation");
        public static readonly DLog Input =
            DLog.For("ViewerNavigation.Input");
        public static readonly DLog UI =
            DLog.For("ViewerNavigation.UI");
    }
}
