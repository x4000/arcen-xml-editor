using System.Xml;
using System.IO;
using ArcenXE.Utilities.MessagesToMainThread;

namespace ArcenXE.Utilities.MetadataProcessing
{
    #region FolderTable
    public class DataTable
    {
        public string Name { get; set; } = string.Empty;
        public List<AttributeData_Base> AttributeData { get; private set; } = new List<AttributeData_Base>();
        //later: public List<SubNode> SubNodes { get; private set; } = new List<SubNode>();
    }
    #endregion

    public static class MetadataLoader
    {
        private static int numberOfMetaDatasStillLoading = 0;
        public static int NumberOfMetaDatasStillLoading { get; }

        public static void LoadAllDataTables( string FolderRoot )
        {
            string[] sharedFiles = Directory.GetFiles( FolderRoot, "*.metadata" );
            if ( sharedFiles.Length != 1 )
                //complain if 0, or more than 1
                if ( sharedFiles.Length < 1 )
                    ArcenDebugging.LogSingleLine( "Missing shared metadata file in the root folder! Please add one!", Verbosity.DoNotShow );
                else if ( sharedFiles.Length > 1 )
                    ArcenDebugging.LogSingleLine( "There's more than one shared metadata file in the root folder! Please remove the extra ones!", Verbosity.DoNotShow );

            string sharedMetaDataFile = sharedFiles[0];

            //now find the actual data tables
            string[] directories = Directory.GetDirectories( FolderRoot );
            ListOfTablesIsReady informMainListOfTables = new ListOfTablesIsReady();
            ListOfXmlPathsIsReady listOfXmlPathsMessage = new ListOfXmlPathsIsReady();
            foreach ( string dir in directories )
            {
                string[] metaDataFiles = Directory.GetFiles( dir, "*.metadata" );
                if ( metaDataFiles.Length == 0 )
                    continue; //must not be a data table, I guess.

                string dataTableNamePath = Path.GetFileName( dir );
                //ArcenDebugging.LogSingleLine( "dir: " + dir, Verbosity.DoNotShow );
                informMainListOfTables.DataTableNames.Add( dataTableNamePath );


                string[] xmlFilesPaths = Directory.GetFiles( dir, "*.xml" );
                foreach ( string xmlFilePath in xmlFilesPaths )
                {
                    listOfXmlPathsMessage.XmlPathsList.Add( xmlFilePath );
                    //ArcenDebugging.LogSingleLine( "dir: " + dir + "\n xmlPath: " + xmlFilePath, Verbosity.DoNotShow );
                }

                if ( metaDataFiles.Length > 1 )
                    //complain about that, but continue
                    ArcenDebugging.LogSingleLine( $" There's more than one metadata file in the {dir} folder! Please remove the extra ones!" +
                        $" The program will continue the execution.", Verbosity.DoNotShow );

                LoadMetadata( metaDataFiles[0], sharedMetaDataFile );
            }
            MainWindow.Instance.MessagesToFrontEnd.Enqueue( informMainListOfTables );
            MainWindow.Instance.MessagesToFrontEnd.Enqueue( listOfXmlPathsMessage );
        }

        public static void LoadMetadata( string FileName, string sharedMetaDataFile )
        {
            //not inside Task.Run to ensure it's actually incremented immediately and avoid subtle bugs
            Interlocked.Increment( ref numberOfMetaDatasStillLoading );

            Task.Run( () =>
            {
                try
                {
                    MetadataDocument metaDoc = new MetadataDocument();
                    metaDoc.ParseDocument( FileName, sharedMetaDataFile );

                    //message for passing MetadataDocument back to main thread or fill the dictionary??
                    MainWindow.Instance.MessagesToFrontEnd.Enqueue( new SaveMetadataToDictionary( metaDoc ) );
                }
                catch ( Exception e )
                {
                    ArcenDebugging.LogErrorWithStack( e );
                }

                Interlocked.Decrement( ref numberOfMetaDatasStillLoading );
            } );
        }
    }
}