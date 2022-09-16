using System.Xml;
using ArcenXE.Utilities.MessagesToMainThread;
using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE.Utilities
{
    public static class Openers
    {
        /// <summary>
        /// incomplete
        /// </summary>
        public static void OpenFileDialog() //todo
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = "Xml Files (*.xml)|*.xml|All files (*.*)|*.*",
                RestoreDirectory = true
            };

            DialogResult dialogResult = openFileDialog.ShowDialog();
            switch ( dialogResult )
            {
                //check if file is xml
                case DialogResult.OK:
                    GenericXmlFileLoader(openFileDialog.FileName);
                    break;
                default:
                    ArcenDebugging.LogSingleLine( dialogResult.ToString(), Verbosity.DoNotShow );
                    break;
            }
        }

        /// <summary>
        /// Thread-safe
        /// </summary>
        public static string? OpenFolderDialog( string defaultPath = @"c:\",  string description = "" )
        {
            defaultPath = ProgramPermanentSettings.MainPath;
            // todo: first read the project folder from xmlproj, or default to c:\\ (maybe not, it creates a circular dependency)
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description = description,
                InitialDirectory = defaultPath
            };
            
            if ( folderBrowserDialog.ShowDialog() == DialogResult.OK )
                return folderBrowserDialog.SelectedPath;
            else
                return null;
        }

        public static string? OpenFolderDialogToSelectRootFolder( string defaultPath = @"c:\", string description = "" )
        {
            //defaultPath = ProgramPermanentSettings.MainPath + "/.."; //go one directory up

            // todo: first read the project folder from xmlproj, or default to c:\\ (maybe not, it creates a circular dependency)
            if ( defaultPath != @"c:\" )
            {
                MetadataStorage.ClearAllMetadata();
                MetadataLoader.LoadAllMetadatas( defaultPath );
                return defaultPath;
            }
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog
            {
                Description = description,
                InitialDirectory = defaultPath
            };

            if ( folderBrowserDialog.ShowDialog() == DialogResult.OK )
            {
                MetadataStorage.ClearAllMetadata();
                MetadataLoader.LoadAllMetadatas( folderBrowserDialog.SelectedPath );             
                return folderBrowserDialog.SelectedPath;
            }
            else
                return null;
        }

        /*/// <summary>
        /// Thread-safe
        /// </summary>
        public static void LoadVisFolderList( string folderPath )
        {
            CopyFolderPathsAndFillVisMessage message = new CopyFolderPathsAndFillVisMessage();
            string[] folderPaths = Directory.GetDirectories( folderPath );
            foreach ( string path in folderPaths )
                message.FoldersPaths.Add( path );
            MainWindow.Instance.MessagesToFrontEnd.Enqueue( message );
            //Explorer explorer = new Explorer();
            //explorer.Show();
        }*/

        public static XmlDocument? GenericXmlFileLoader( string fileName, bool preserveWhitespace = false )
        {
            XmlDocument doc = new XmlDocument
            {
                PreserveWhitespace = preserveWhitespace //try preserving it, but refactor all the parsers to accept whitespaces                
            };
            try
            {
                doc.Load( fileName );
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogErrorWithStack( e );
                return null;
            }
            return doc;
        }
    }
}
