namespace ArcenXE.Utilities
{
    public static class ProgramPermanentSettings
    {
        public readonly static PathStorage FolderPathContainingSharedMetaData = new PathStorage();
        public readonly static PathStorage MainFolderPathForVis = new PathStorage( @"C:\Users\Daniel\ArcenDev\Arcology5\GameData\Configuration",
                                                                                  "Select the folder containing all the files you want to work on" );
    }

    public class PathStorage
    {
        private string? path = null;
        public string? Path
        {
            get
            {
                if ( path == null )
                    return SelectAndStorePath();
                else
                    return path;

            }
            private set => path = value;
        }
        private string? descriptionFolderDialog = string.Empty; //temporary

        internal protected PathStorage() { }
        internal protected PathStorage( string descriptionForFolderDialog ) //temporary
        {
            descriptionFolderDialog = descriptionForFolderDialog;
        }
        internal protected PathStorage( string mPath, string descriptionForFolderDialog ) //temporary
        {
            path = mPath;
            descriptionFolderDialog = descriptionForFolderDialog;
        }

        private string? SelectAndStorePath()
        {
            if ( descriptionFolderDialog == null ) //temporary
                descriptionFolderDialog = string.Empty;

            string? path = Openers.OpenFolderDialog( descriptionFolderDialog );
            if ( path != null )
            {
                Path = path;
                return path;
            }
            else
                return null;
        }
    }
}
