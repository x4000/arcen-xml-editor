using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Utilities.XmlDataProcessing;

namespace ArcenXE.Utilities.MessagesToMainThread
{
    public class SendEditedXmlTopNodeToList : IBGMessageToMainThread
    {
        public readonly List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();
        private readonly Dictionary<string, EditedXmlNode> mainDict = MainWindow.Instance.TopNodesVis;

        public void ProcessMessageOnMainThread()
        {
            if ( mainDict.Count > 0 )
                mainDict.Clear();
            for ( int i = 0; i < Nodes.Count; i++ )
            {
                if ( Nodes[i] is EditedXmlNode node )
                    if ( node.NodeName != null )
                        mainDict.TryAdd( node.NodeName.Value, node );
            }
            MainWindow.Instance.VisualizeTopNodesFromSelectedFile();
        }
    }

    public class SaveEditedXmlToList : IBGMessageToMainThread
    {
        public readonly List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();
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
            foreach ( string tableName in DataTableNames )
                MainWindow.Instance.DataTableNames.Add( tableName );
        }
    }

    public class ListOfXmlPathsIsReady : IBGMessageToMainThread
    {
        public readonly List<string> XmlPathsList = new List<string>();
        private readonly List<string> mainList = MainWindow.Instance.XmlPaths;
        public void ProcessMessageOnMainThread()
        {
            if ( mainList.Count > 0 )
                mainList.Clear();
            foreach ( string path in XmlPathsList )
                mainList.Add( path );
            MainWindow.Instance.FillFileList();
        }
    }
}

