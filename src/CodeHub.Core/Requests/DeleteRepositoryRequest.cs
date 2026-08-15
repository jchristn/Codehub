namespace CodeHub.Core.Requests
{
    /// <summary>
    /// Request to delete a repository from disk and remove it from CodeHub.
    /// </summary>
    public class DeleteRepositoryRequest
    {
        #region Public-Members

        /// <summary>
        /// True to send the directory to the Recycle Bin (undoable); false to delete permanently.
        /// </summary>
        public bool Recycle { get; set; } = false;

        #endregion
    }
}
