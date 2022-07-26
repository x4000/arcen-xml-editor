using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Utilities.XmlDataProcessing;

namespace ArcenXE.Utilities.MessagesToMainThread
{
    public class SendEditedXmlTopNodeToList : IBGMessageToMainThread
    {
        public List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();

        public void ProcessMessageOnMainThread()
        {
            foreach ( EditedXmlNode node in Nodes )
            {
                if ( node.NodeName != null )
                    MainWindow.Instance.TopNodesList.Items.Add( node.NodeName.Value );
            }
        }
    }

    public class SaveEditedXmlToList : IBGMessageToMainThread
    {
        public List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();
        public void ProcessMessageOnMainThread() => MainWindow.Instance.CurrentXmlForVis.AddRange( this.Nodes );
    }

    public class SaveMetadataToDictionary : IBGMessageToMainThread
    {
        private readonly MetadataDocument metadataDocument;
        public SaveMetadataToDictionary( MetadataDocument metaDoc )
        {
            this.metadataDocument = metaDoc;
        }
        public void ProcessMessageOnMainThread() => MainWindow.Instance.metadataDocument = this.metadataDocument;
    }

    public class ListOfTablesIsReady : IBGMessageToMainThread
    {
        public readonly List<string> DataTableNames = new List<string>();
        public void ProcessMessageOnMainThread()
        {
            MainWindow.Instance.DataTableNames.AddRange( this.DataTableNames );
            MainWindow.Instance.FillFileList();
        }
    }
}
