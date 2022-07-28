using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Utilities.XmlDataProcessing;

namespace ArcenXE.Utilities.MessagesToMainThread
{ //needs refactoring and reordering
    public class SendEditedXmlTopNodeToList : IBGMessageToMainThread
    {
        public readonly List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();
        private readonly Dictionary<string, EditedXmlNode> mainDictTopNodes = MainWindow.Instance.TopNodesVis;

        public void ProcessMessageOnMainThread()
        {
            if ( mainDictTopNodes.Count > 0 )
                mainDictTopNodes.Clear();
            for ( int i = 0; i < Nodes.Count; i++ )
            {
                if ( Nodes[i] is EditedXmlNode node )
                    if ( node.NodeName != null )
                        mainDictTopNodes.TryAdd( node.NodeName.Value, node );
            }
            MainWindow.Instance.VisualizeTopNodesFromSelectedFile();
        }
    }

    public class SaveEditedXmlToList : IBGMessageToMainThread
    {
        public readonly List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();
        private readonly List<IEditedXmlNodeOrComment> mainXmlVis = MainWindow.Instance.CurrentXmlForVis;
        public void ProcessMessageOnMainThread()
        {
            if ( mainXmlVis.Count > 0 )
                mainXmlVis.Clear();
            foreach ( IEditedXmlNodeOrComment node in Nodes )
                mainXmlVis.Add( node );
        }
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
            //foreach ( string tableName in DataTableNames )
            //    MainWindow.Instance.DataTableNames.Add( tableName );
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

    public class SendFolderListToMain : IBGMessageToMainThread
    {
        public readonly List<string> FoldersPaths = new List<string>();
        private readonly List<string> mainList = MainWindow.Instance.FoldersPathsVis;
        public void ProcessMessageOnMainThread()
        {
            if ( mainList.Count > 0 )
                mainList.Clear();

            foreach ( string folder in FoldersPaths )
            {
                mainList.Add( folder );
                MainWindow.Instance.FillFolderList();
            }
        }
    }
}

