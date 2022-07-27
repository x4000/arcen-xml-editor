using System.Xml;

namespace ArcenXE.Utilities
{
    public static class FileOpeners
    {
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
