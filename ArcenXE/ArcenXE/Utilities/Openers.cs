﻿using System.Xml;
using ArcenXE.Utilities.MessagesToMainThread;

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
            defaultPath = @"C:\Users\Daniel\ArcenDev\Arcology5\GameData\Configuration";
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

        public static void LoadVisFolderList( string folderPath )
        {
            SendFolderListToMain message = new SendFolderListToMain();
            string[] folderPaths = Directory.GetDirectories( folderPath );
            foreach ( string path in folderPaths )
                message.FoldersPaths.Add( path );
            MainWindow.Instance.MessagesToFrontEnd.Enqueue( message );
            //Explorer explorer = new Explorer();
            //explorer.Show();
        }

        public static XmlDocument? GenericXmlFileLoader( string fileName )
        {
            XmlDocument doc = new XmlDocument
            {
                PreserveWhitespace = false
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