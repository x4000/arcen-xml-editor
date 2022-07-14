using System.Xml;
using ArcenXE.Utilities.MessagesToMainThread;

namespace ArcenXE.Utilities
{
    public class FileOpener //make static or singleton?
    {
        public void OpenFileWindow()
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
                    Task.Run( () =>
                    {
                        try //remove try catch?
                        {
                            XmlDocument doc = new XmlDocument
                            {
                                PreserveWhitespace = false
                            };
                            try
                            {
                                doc.Load( openFileDialog.FileName );
                            }
                            catch ( Exception e )
                            {
                                MessageBox.Show( e.ToString() );
                            }
                            //parse xml document into complex data structure (with other bg threads)
                            SendEditedXmlTopNodeToList message = new SendEditedXmlTopNodeToList();
                            Task.Run( () =>
                            {
                                XmlParser parser = new XmlParser();
                                XmlElement? root = doc?.DocumentElement;
                                if ( root != null )
                                {
                                    XmlNodeList childNodes = root.ChildNodes;
                                    if ( childNodes.Count > 0 )
                                    {
                                        //Parallel.For( int = 0; i < childNodes.Count; i++;
                                        //delegate ( int index )
                                        //{
                                        //    XmlNode node = childNodes[index];
                                        //} );
                                        foreach ( XmlNode node in childNodes )
                                        {
                                            switch ( node.NodeType )
                                            {
                                                case XmlNodeType.Element:
                                                    IEditedXmlNodeOrComment? result = parser.ProcessXmlElement( (XmlElement)node, true ); //task.run on this? risk of losing the correct order of parts, so need a thread-safe structure
                                                    if ( result != null )
                                                        message.Nodes.Add( result );
                                                    break;
                                                case XmlNodeType.Comment:
                                                    EditedXmlComment comment = new EditedXmlComment
                                                    {
                                                        Data = node.InnerText
                                                    };
                                                    message.Nodes.Add( comment );
                                                    break;
                                                default:
                                                    MessageBox.Show( "why do we have a " + node.NodeType + " directly under the root node?" );
                                                    break;
                                            }
                                        }
                                    }
                                    else //no children, so root is primary
                                    {
                                        IEditedXmlNodeOrComment? result = parser.ProcessXmlElement( root, false );
                                        if ( result != null )
                                            message.Nodes.Add( result );
                                    }
                                    MainWindow.Instance.CurrentXmlForVis.AddRange( message.Nodes ); //need to save message.Nodes elsewhere because it's needed for Vis. No longer necessary?
                                    MainWindow.Instance.MessagesToFrontEnd.Enqueue( message );
                                }
                                else
                                {
                                    MessageBox.Show( "Error: root in OpenFileWindow() is null" );
                                }
                            } );
                        }
                        catch ( Exception e )
                        {
                            MessageBox.Show( e.ToString() );
                        }
                    } );
                    break;
                default:
                    MessageBox.Show( dialogResult.ToString() );
                    break;
            }
        }
    }
}
