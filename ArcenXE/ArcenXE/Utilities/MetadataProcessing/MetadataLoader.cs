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
#pragma warning disable CA2211
        public static int NumberOfMetaDatasStillLoading = 0;
#pragma warning restore CA2211

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
            foreach ( string dir in directories )
            {
                string[] metaDataFiles = Directory.GetFiles( dir, "*.metadata" );
                if ( metaDataFiles.Length == 0 )
                    continue; //must not be a data table, I guess.

                string dataTableNamePath = Path.GetFileName( dir );
                //todo: write dataTableName to the list of tables globally, including the ui listbox
                informMainListOfTables.DataTableNames.Add( dataTableNamePath );

                if ( metaDataFiles.Length > 1 )
                    //complain about that, but continue
                    ArcenDebugging.LogSingleLine( $" There's more than one metadata file in the {dir} folder! Please remove the extra ones!" +
                        $" The program will continue the execution.", Verbosity.DoNotShow );

                LoadMetadata( metaDataFiles[0], sharedMetaDataFile );
            }
            MainWindow.Instance.MessagesToFrontEnd.Enqueue( informMainListOfTables );
        }

        public static void LoadMetadata( string FileName, string sharedMetaDataFile )
        {
            //not inside Task.Run to ensure it's actually incremented immediately and avoid subtle bugs
            Interlocked.Increment( ref NumberOfMetaDatasStillLoading );

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

                Interlocked.Decrement( ref NumberOfMetaDatasStillLoading );
            } );
        }
    }
}