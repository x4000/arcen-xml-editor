using System.Xml;
using ArcenXE.Utilities.MessagesToMainThread;

namespace ArcenXE.Utilities.XmlDataProcessing
{
    public static class XmlLoader
    {
        private static int numberOfDatasStillLoading = 0;
        public static int NumberOfDatasStillLoading { get; }
        public static void LoadXml( string fileName )
        {
            Interlocked.Increment( ref numberOfDatasStillLoading );
            //parse xml document into complex data structure (with other bg threads)
            Task.Run( () =>
            {
                XmlDocument? doc = Openers.GenericXmlFileLoader( fileName );
                SaveEditedXmlToList messageSaveXml = new SaveEditedXmlToList();
                SendEditedXmlTopNodeToList messageSendXmlTopNode = new SendEditedXmlTopNodeToList();
                XmlParser parser = new XmlParser();
                XmlElement? root = doc?.DocumentElement;
                if ( root != null )
                {
                    XmlNodeList childNodes = root.ChildNodes;
                    if ( childNodes.Count > 0 )
                        //Parallel.For( int = 0; i < childNodes.Count; i++;
                        //delegate ( int index )
                        //{
                        //    XmlNode node = childNodes[index];
                        //} );
                        foreach ( XmlNode node in childNodes )
                        {
                            //ArcenDebugging.LogSingleLine( "Top Node: " + node.Name + " name=" + ((XmlElement)node).GetAttribute( "name" ), Verbosity.DoNotShow );
                            switch ( node.NodeType )
                            {
                                case XmlNodeType.Element:
                                    //task.run on this? risk of losing the correct order of parts, so need a thread-safe structure
                                    IEditedXmlNodeOrComment? result = parser.ProcessXmlElement( (XmlElement)node, true );
                                    if ( result != null )
                                        messageSendXmlTopNode.Nodes.Add( result );
                                    break;
                                case XmlNodeType.Comment:
                                    EditedXmlComment comment = new EditedXmlComment
                                    {
                                        Data = node.InnerText
                                    };
                                    messageSendXmlTopNode.Nodes.Add( comment );
                                    break;
                                default:
                                    ArcenDebugging.LogSingleLine( "Why do we have a " + node.NodeType + " directly under the element node?", Verbosity.DoNotShow );
                                    break;
                            }
                        }
                    else //no children, so root is primary
                    {
                        IEditedXmlNodeOrComment? result = parser.ProcessXmlElement( root, false );
                        if ( result != null )
                            messageSendXmlTopNode.Nodes.Add( result );
                    }
                    messageSaveXml.Nodes.AddRange( messageSendXmlTopNode.Nodes );
                    MainWindow.Instance.MessagesToFrontEnd.Enqueue( messageSaveXml );
                    MainWindow.Instance.MessagesToFrontEnd.Enqueue( messageSendXmlTopNode );
                }
                else
                    ArcenDebugging.LogSingleLine( "Error: root in OpenFileWindow() is null", Verbosity.DoNotShow );
                Interlocked.Decrement( ref numberOfDatasStillLoading );
            } );
        }
    }
}
