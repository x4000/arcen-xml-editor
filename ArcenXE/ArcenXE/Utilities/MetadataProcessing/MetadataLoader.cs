using System.Xml;
using System.IO;
using ArcenXE.Utilities.MessagesToMainThread;

namespace ArcenXE.Utilities.MetadataProcessing
{
    public static class MetadataLoader
    {
        private static int numberOfMetaDatasStillLoading = 0;
        public static int NumberOfMetaDatasStillLoading { get; }

        public static int NumberOfMetadataLoadingStarts = 0;

        public static void LoadAllMetadatas( string folderPathContainingAllMetadata ) // load all metadata with the shared
        {
            NumberOfMetadataLoadingStarts++;
            string[] sharedFiles = Directory.GetFiles( folderPathContainingAllMetadata, "*.metadata" );
            if ( sharedFiles.Length != 1 )
            {
                //complain if 0, or more than 1
                if ( sharedFiles.Length < 1 )
                    ArcenDebugging.LogSingleLine( "WARNING: Missing shared metadata file in the root folder! Please add one!", Verbosity.ShowAsError );
                else if ( sharedFiles.Length > 1 )
                    ArcenDebugging.LogSingleLine( "WARNING: There's more than one shared metadata file in the root folder! The extra ones will be ignored!", Verbosity.DoNotShow );
            }
            
            //now find the actual metadata tables
            string[] directories = Directory.GetDirectories( folderPathContainingAllMetadata );
            foreach ( string dir in directories )
            {
                string[] metaDataFiles = Directory.GetFiles( dir, "*.metadata" );
                if ( metaDataFiles.Length == 0 )
                {
                    ArcenDebugging.LogSingleLine( $"INFO: Metadata file missing in the {dir} folder. The program will ignore the XML files in this folder.",
                                                 Verbosity.DoNotShow );
                    continue; //must not be a data folder, I guess.
                }

                //complain about that, but continue
                if ( metaDataFiles.Length > 1 )                    
                    ArcenDebugging.LogSingleLine( $"WARNING: There's more than one metadata file in the {dir} folder! Please remove the extra ones!" +
                        $" The program will continue the execution and ignore the extra files.", Verbosity.DoNotShow );
                
                LoadMetadata( metaDataFiles[0], sharedFiles[0] );
            }
            
        }

        private static void LoadMetadata( string fileName, string sharedMetaDataFile )
        {
            //not inside Task.Run to ensure it's actually incremented immediately and avoid subtle bugs
            Interlocked.Increment( ref numberOfMetaDatasStillLoading );            
            //ArcenDebugging.LogSingleLine( " Starting LoadMetadata", Verbosity.DoNotShow );
            Task.Run( () =>
            {
                try
                {
                    MetadataDocument metaDoc = new MetadataDocument();
                    metaDoc.ParseDocument( fileName, sharedMetaDataFile );
                    MainWindow.Instance.MessagesToFrontEnd.Enqueue( new CopyMetadataDocumentMessage( metaDoc ) );
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