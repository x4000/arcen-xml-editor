using System.Xml;
using ArcenXE.Utilities.MessagesToMainThread;

namespace ArcenXE.Utilities.XmlDataProcessing
{
    public class XmlLoader //make singleton?
    {
        public void OpenFileWindow()
        {
            /*OpenFileDialog openFileDialog = new OpenFileDialog
            {
                InitialDirectory = "c:\\",
                Filter = "Xml Files (*.xml)|*.xml|All files (*.*)|*.*",
                RestoreDirectory = true
            };
            
            DialogResult dialogResult = openFileDialog.ShowDialog();*/
            DialogResult dialogResult = DialogResult.OK;
            switch ( dialogResult )
            {
                //check if file is xml
                case DialogResult.OK:
                    //Task.Run( () =>
                    {
                        try //remove try catch?
                        {
                            XmlDocument doc = new XmlDocument
                            {
                                PreserveWhitespace = false
                            };
                            try
                            {
                                //doc.Load( openFileDialog.FileName );
                                doc.Load( "C:\\Users\\Daniel\\ArcenDev\\AIWar2_dev\\GameData\\Configuration\\Expansion\\KDL_Expansions.xml" );
                            }
                            catch ( Exception e )
                            {
                                ArcenDebugging.LogErrorWithStack( e );
                            }
                            //parse xml document into complex data structure (with other bg threads)
                            //Task.Run( () =>
                            {
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
                            } //);
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogErrorWithStack( e );
                        }
                    } //);
                    break;
                default:
                    ArcenDebugging.LogSingleLine( dialogResult.ToString(), Verbosity.DoNotShow );
                    break;
            }
        }
    }
}
