using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE.Utilities
{
    public static class XmlRootFolders
    {
        private static readonly Dictionary<string, XmlDataTable> xmlDataTables = new Dictionary<string, XmlDataTable>(); // Dictionary<FolderName, XmlDatTable>
        private static readonly List<string> xmlDataTableNames = new List<string>(); // Basically a list of folders' names containing XML files

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
            // Get all the subfolders that might contain XML files
            string[] folderPaths = Directory.GetDirectories( pathToRootFolder );
            foreach ( string folderPath in folderPaths )
                FillDataTable( folderPath );

            //todo: DLCs

            //todo: Mods            
        }

        private static void FillDataTable( string folderPath )
        {
            // Get all XML files in a specific subfolder
            string[] xmlFilesPath = Directory.GetFiles( folderPath, "*.xml" );
            string folderAndTableName = Path.GetFileNameWithoutExtension( folderPath );
            MetadataDocument? metaDoc = MetadataStorage.GetMetadataDocumentByName( folderAndTableName );
            if ( metaDoc == null )
                return; // metaDoc may have not loaded yet on bg threads

            foreach ( string xmlFilePath in xmlFilesPath )
            {
                // Add XML file's data and name to the DataTables if it's not present
                if ( !xmlDataTables.TryGetValue( folderAndTableName, out XmlDataTable? table ) )
                {
                    table = new XmlDataTable( metaDoc );
                    xmlDataTables[folderAndTableName] = table;
                    xmlDataTableNames.Add( folderAndTableName );
                }
                // Add XML file's path to the DataTables' files list
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
        private readonly string FileNameWithoutExtension = string.Empty;
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
