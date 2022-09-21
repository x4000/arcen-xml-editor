using System.Xml;
using ArcenXE.Utilities.MessagesToMainThread;
using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE.Utilities.XmlDataProcessing
{
    public static class XmlLoader
    {
        private static int numberOfDatasStillLoading = 0;
        public static int NumberOfDatasStillLoading { get; }
        public static void LoadXml( string fileName, MetadataDocument metaDoc )
        {
            Interlocked.Increment( ref numberOfDatasStillLoading );
            //parse xml document into complex data structure (with other bg threads)
            Task.Run( () =>
            {
                CopyEditedXmlAndFillVis_Message messageSaveXml = new CopyEditedXmlAndFillVis_Message();
                XmlParser parser = new XmlParser();
                XmlDocument? doc = Openers.GenericXmlFileLoader( fileName, true );
                XmlElement? root = doc?.DocumentElement;
                if ( root != null )
                {
                    if ( !metaDoc.IsSingleRootTypeDocument )
                    {
                        //risk of losing the correct order of parts with this, so it'd need a thread-safe structure
                        //Parallel.For( int = 0; i < childNodes.Count; i++;
                        //delegate ( int index )
                        //{
                        //    XmlNode node = childNodes[index];
                        //} );
                        foreach ( XmlNode node in root.ChildNodes )
                        {
                            //ArcenDebugging.LogSingleLine( "Top Node: " + node.Name + " name=" + ((XmlElement)node).GetAttribute( "name" ), Verbosity.DoNotShow );
                            switch ( node.NodeType )
                            {
                                case XmlNodeType.Element:
                                    IEditedXmlNodeOrComment? result = parser.ProcessXmlElement( (XmlElement)node, metaDoc, true, false );
                                    if ( result != null )
                                        messageSaveXml.Nodes.Add( result );
                                    break;
                                case XmlNodeType.Comment:
                                    EditedXmlComment comment = new EditedXmlComment
                                    {
                                        Data = node.InnerText,
                                        OuterXml = node.OuterXml,
                                    };
                                    messageSaveXml.Nodes.Add( comment );
                                    break;
                                case XmlNodeType.Whitespace:
                                case XmlNodeType.SignificantWhitespace:
                                    break;
                                default:
                                    ArcenDebugging.LogSingleLine( "Why do we have a " + node.NodeType + $" directly under the element in {node.BaseURI}?", Verbosity.DoNotShow );
                                    break;
                            }
                        }
                    }
                    else //root is primary
                    {
                        IEditedXmlNodeOrComment? result = parser.ProcessXmlElement( root, metaDoc, true, true );
                        //XmlParser.DumpXmlData();
                        if ( result != null )
                            messageSaveXml.Nodes.Add( result );
                    }
                    MainWindow.Instance.MessagesToFrontEnd.Enqueue( messageSaveXml );
                }
                else
                    ArcenDebugging.LogSingleLine( "Error: root in OpenFileWindow() is null", Verbosity.DoNotShow );
                Interlocked.Decrement( ref numberOfDatasStillLoading );
            } );
        }
    }
}
