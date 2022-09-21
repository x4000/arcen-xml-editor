using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE.Utilities.MessagesToMainThread
{
    public class CopyEditedXmlAndFillVis_Message : IBGMessageToMainThread
    {
        public readonly List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();
        private readonly Dictionary<uint, IEditedXmlNodeOrComment> mainXmlVis = MainWindow.Instance.CurrentXmlTopNodesForVis;
        public void ProcessMessageOnMainThread()
        {
            bool rootOnly = false;
            if ( mainXmlVis.Count > 0 )
                mainXmlVis.Clear();
            ArcenDebugging.LogSingleLine( $"Nodes.Count: {Nodes.Count}", Verbosity.DoNotShow );
            foreach ( IEditedXmlNodeOrComment nodeOrComment in Nodes )
                if ( nodeOrComment.UID != 0 )
                {
                    mainXmlVis.Add( nodeOrComment.UID, nodeOrComment );
                    if ( !nodeOrComment.IsComment )
                        if ( ((EditedXmlNode)nodeOrComment).IsRootOnly )
                            rootOnly = true;
                }

            MainWindow.Instance.FillTopNodesList();
            if ( rootOnly )
                MainWindow.Instance.SelectedTopNodeIndex = 1;
        }
    }

    public class CopyMetadataDocumentMessage : IBGMessageToMainThread
    {
        private readonly MetadataDocument metadataDocument;
        public CopyMetadataDocumentMessage( MetadataDocument metaDoc )
        {
            this.metadataDocument = metaDoc;
        }
        public void ProcessMessageOnMainThread() => MetadataStorage.AddMetadataDocument( this.metadataDocument.MetadataFolder, this.metadataDocument );
    }

    /*public class CopyEditedXmlTopNodesAndFillVisMessage : IBGMessageToMainThread
    {
        public readonly List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();
        //private readonly Dictionary<string, IEditedXmlNodeOrComment> mainDictTopNodes = MainWindow.Instance.CurrentXmlTopNodesForVis;

        public void ProcessMessageOnMainThread()
        {
            bool rootOnly = false;
            //if ( mainDictTopNodes.Count > 0 )
            //    mainDictTopNodes.Clear();
            //int commentNumber = 1;
            //foreach ( IEditedXmlNodeOrComment? node in Nodes )
            //{
            //    if ( node.IsComment )
            //    {
            //        //mainDictTopNodes.Add( "Comment " + commentNumber, node ); //remove this
            //        mainDictTopNodes.Add( node.UID, node );
            //        commentNumber++;
            //    }
            //    else
            //    {
            //        EditedXmlAttribute? att = ((EditedXmlNode)node).NodeCentralID;
            //        if ( att != null && att.ValueOnDisk != null )
            //            mainDictTopNodes.Add( att.ValueOnDisk, node );
            //        if ( ((EditedXmlNode)node).IsRootOnly )
            //            rootOnly = true;
            //    }
            //}
            foreach ( IEditedXmlNodeOrComment? node in Nodes )
                if ( !node.IsComment )
                    if ( ((EditedXmlNode)node).IsRootOnly )
                        rootOnly = true;

            //MainWindow.Instance.FillTopNodesList();
            //if ( rootOnly )
            //{
            //    MainWindow.Instance.SelectedTopNodeIndex = 0;
            //    MainWindow.Instance.CallXmlVisualizer();
            //}
        }
    }*/
}

