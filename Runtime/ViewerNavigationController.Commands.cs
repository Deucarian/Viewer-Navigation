namespace Deucarian.ViewerNavigation
{
    public sealed partial class ViewerNavigationController
    {
        /// <summary>
        /// Applies one transport-neutral host request to this authoritative
        /// navigation state owner.
        /// </summary>
        public bool TryExecuteCommand(
            ViewerNavigationCommand command,
            out string message)
        {
            return ViewerNavigationCommandExecutor.TryExecute(
                this,
                command,
                out message);
        }
    }
}
