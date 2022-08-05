using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE.Utilities
{
    public static class XmlRootFolders
    {
        //private static readonly string pathToBaseGameData = ProgramPermanentSettings.ApplicationPath.Path + @"\GameData\Configuration";

        private static readonly Dictionary<string, XmlDataTable> xmlDataTables = new Dictionary<string, XmlDataTable>();
        private static readonly List<string> xmlDataTableNames = new List<string>();

        public static XmlDataTable? GetXmlDataTableByName( string tableAndFolderName )
        {
            if ( xmlDataTables.Count == 0 )
                throw new Exception( "Called GetXmlDataTableByName before calling ResetXmlFoldersAndFilesToSearch" );
            if ( xmlDataTables.TryGetValue( tableAndFolderName, out XmlDataTable? xmlDataTable ) )
                return xmlDataTable;
            else
            {
                ArcenDebugging.LogSingleLine( $"Tried to find table with '{tableAndFolderName}', but no table was found ", Verbosity.ShowAsError );
                return null;
            }
        }

        public static List<string> GetXmlDataTableNames()
        {
            return xmlDataTableNames;
        }

        public static void ResetXmlFoldersAndFilesToSearch( string pathToRootFolder )
        {
            foreach ( KeyValuePair<string, XmlDataTable> kv in xmlDataTables )
            {
                kv.Value.Files.Clear();
            }
            string[] folderPaths = Directory.GetDirectories( pathToRootFolder );
            foreach ( string folderPath in folderPaths )
                FillDataTable( folderPath );

            //todo: DLCs

            //todo: Mods            
        }

        private static void FillDataTable( string folderPath )
        {
            string[] xmlFilesPath = Directory.GetFiles( folderPath, "*.xml" );
            string folderAndTableName = Path.GetFileNameWithoutExtension( folderPath );

            if ( !MetadataStorage.AllMetadatas.TryGetValue( folderAndTableName, out MetadataDocument? metaDoc ) )
                return; // metaDoc may have not loaded yet on bg threads

            foreach ( string xmlFilePath in xmlFilesPath )
            {
                if ( !xmlDataTables.TryGetValue( folderAndTableName, out XmlDataTable? table ) )
                {
                    table = new XmlDataTable( metaDoc );
                    xmlDataTables[folderAndTableName] = table;
                    xmlDataTableNames.Add( folderAndTableName );
                }

                table.Files.Add( new XmlDataTableFile( xmlFilePath ) );
            }
        }
    }

    public class XmlDataTable
    {
        public readonly MetadataDocument MetaDoc;
        public readonly List<XmlDataTableFile> Files = new List<XmlDataTableFile>();

        public XmlDataTable( MetadataDocument metaDoc )
        {
            this.MetaDoc = metaDoc;
        }
    }

    public class XmlDataTableFile
    {
        public readonly string FileNameWithoutExtension = string.Empty;
        public readonly string FullFilePath = string.Empty;

        public XmlDataTableFile( string fullFilePath )
        {
            this.FullFilePath = fullFilePath;
            this.FileNameWithoutExtension = Path.GetFileNameWithoutExtension( fullFilePath );
        }

        public override string ToString()
        {
            return this.FileNameWithoutExtension;
        }
    }
}
