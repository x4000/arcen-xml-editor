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

        public static void LoadDataTables( string folderRoot, string folderPathContainingSharedMetaData )
        {
            string[] sharedFiles = Directory.GetFiles( folderPathContainingSharedMetaData, "*.metadata" );
            if ( sharedFiles.Length != 1 )
            {
                //complain if 0, or more than 1
                if ( sharedFiles.Length < 1 )
                    ArcenDebugging.LogSingleLine( "Missing shared metadata file in the root folder! Please add one!", Verbosity.ShowAsError );
                else if ( sharedFiles.Length > 1 )
                    ArcenDebugging.LogSingleLine( "There's more than one shared metadata file in the root folder! The extra ones will be ignored!", Verbosity.DoNotShow );
            }

            string sharedMetaDataFile = sharedFiles[0];

            //now find the actual metadata tables
            //string[] directories = Directory.GetDirectories( folderRoot ); // code for lazy loading?
            //ListOfTablesIsReady informMainListOfTables = new ListOfTablesIsReady();
            ListOfXmlPathsIsReady listOfXmlPathsMessage = new ListOfXmlPathsIsReady();
            /*foreach ( string dir in directories )
            {
                string[] metaDataFiles = Directory.GetFiles( dir, "*.metadata" );
                if ( metaDataFiles.Length == 0 )
                    continue; //must not be a data table, I guess.

                string dataTableNamePath = Path.GetFileName( dir );
                //ArcenDebugging.LogSingleLine( "dir: " + dir, Verbosity.DoNotShow );
                informMainListOfTables.DataTableNames.Add( dataTableNamePath );

                #region Regular XML Files
                // this shouldn't be here, but it's convenient to have it here because Metadata files are loaded before the actual data xml files
                string[] xmlFilesPath = Directory.GetFiles( dir, "*.xml" );
                foreach ( string xmlFilePath in xmlFilesPath )
                {
                    listOfXmlPathsMessage.XmlPathsList.Add( xmlFilePath );
                    //ArcenDebugging.LogSingleLine( "dir: " + dir + "\n xmlPath: " + xmlFilePath, Verbosity.DoNotShow );
                }
                #endregion*/

            string[] metaDataFiles = Directory.GetFiles( folderRoot, "*.metadata" );
            if ( metaDataFiles.Length == 0 ) //must not be a data table, I guess
            {
                ArcenDebugging.LogSingleLine( $" Metadata file missing in the {folderRoot} folder! Please provide one! The program will ignore the XML files in this folder.", Verbosity.DoNotShow );
                return;
            }
            else if ( metaDataFiles.Length >= 1 )
            {
                #region Regular XML Files
                // this shouldn't be in here, but it's convenient to have it in here because Metadata files are loaded before the actual data xml files
                string[] xmlFilesPath = Directory.GetFiles( folderRoot, "*.xml" );
                foreach ( string xmlFilePath in xmlFilesPath )
                {
                    listOfXmlPathsMessage.XmlPathsList.Add( xmlFilePath );
                }
                #endregion
                if ( metaDataFiles.Length > 1 )
                    //complain about that, but continue
                    ArcenDebugging.LogSingleLine( $" There's more than one metadata file in the {folderRoot} folder! Please remove the extra ones!" +
                        $" The program will continue the execution.", Verbosity.DoNotShow );
            }

            LoadMetadata( metaDataFiles[0], sharedMetaDataFile );

            //MainWindow.Instance.MessagesToFrontEnd.Enqueue( informMainListOfTables );
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