using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Utilities.XmlDataProcessing;

namespace ArcenXE.Utilities.MessagesToMainThread
{ //needs refactoring and reordering
    public class CopyEditedXmlMessage : IBGMessageToMainThread
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

    public class CopyMetadataDocumentMessage : IBGMessageToMainThread
    {
        private readonly MetadataDocument metadataDocument;
        public CopyMetadataDocumentMessage( MetadataDocument metaDoc )
        {
            this.metadataDocument = metaDoc;
        }
        public void ProcessMessageOnMainThread() => MetadataStorage.AllMetadatas.Add( this.metadataDocument.MetadataFolder, this.metadataDocument );
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

    /*public class CopyFolderPathsAndFillVisMessage : IBGMessageToMainThread //currently unused, might be useful, after refactoring, if OpenFolderDialogToSelectRootFolder() has to be made threadsafe
    {
        public readonly List<string> FoldersPaths = new List<string>();
        private readonly List<string> mainList = XmlRootFolders.XmlFolders;
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
    }*/

    /*public class CopyXmlPathsAndFillVisMessage : IBGMessageToMainThread
    {
        public readonly List<string> XmlPathsList = new List<string>();
        private readonly List<string> mainList = MainWindow.Instance.XmlPathsVis;
        public void ProcessMessageOnMainThread()
        {
            if ( mainList.Count > 0 )
                mainList.Clear();
            foreach ( string path in XmlPathsList )
                mainList.Add( path );
            //MainWindow.Instance.FillFileList(); // to be moveed elsewhere
        }
    }*/

    public class CopyEditedXmlTopNodesAndFillVisMessage : IBGMessageToMainThread
    {
        public readonly List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();
        private readonly Dictionary<string, IEditedXmlNodeOrComment> mainDictTopNodes = MainWindow.Instance.TopNodesVis;

        public void ProcessMessageOnMainThread()
        {
            if ( mainDictTopNodes.Count > 0 )
                mainDictTopNodes.Clear();
            /*for ( int i = 0; i < Nodes.Count; i++ )
            {
                if ( Nodes[i] is EditedXmlNode node )
                    if ( node.NodeName != null )
                        mainDictTopNodes.TryAdd( node.NodeName.Value, node );
            }*/
            int commentNumber = 1;
            foreach ( IEditedXmlNodeOrComment? node in Nodes )
            {
                if ( node.IsComment )
                {
                    mainDictTopNodes.Add( "Comment " + commentNumber, node );
                    commentNumber++;
                }
                else
                {
                    EditedXmlAttribute? att = ((EditedXmlNode)node).NodeCentralID;
                    if ( att != null )
                        mainDictTopNodes.Add( att.Value, node );
                }
            }
            MainWindow.Instance.FillTopNodesList();
        }
    }
}

