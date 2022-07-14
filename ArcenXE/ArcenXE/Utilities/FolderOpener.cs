namespace ArcenXE.Utilities
{
    internal class FolderOpener
    {

        internal void OpenFolderWindow()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                InitialDirectory = "c:\\"
            };

            if ( folderBrowserDialog.ShowDialog() == DialogResult.OK )
            {
                MainWindow.Instance.FilesNames.AddRange( Directory.GetFiles( folderBrowserDialog.SelectedPath, "*.xml" ) );
                Explorer explorer = new Explorer();
                explorer.Show();
            }
        }
    }
}
