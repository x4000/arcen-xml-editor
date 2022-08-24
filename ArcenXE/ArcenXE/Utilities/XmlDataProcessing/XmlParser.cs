using System.Xml;
using ArcenXE.Utilities.MetadataProcessing;
using System.Collections.Concurrent;

namespace ArcenXE.Utilities.XmlDataProcessing
{
    public class XmlParser
    {
        //public static readonly ConcurrentQueue<string> dump1 = new ConcurrentQueue<string>();
        //public static readonly ConcurrentQueue<string> dump2 = new ConcurrentQueue<string>();

        public IEditedXmlNodeOrComment? ProcessXmlElement( XmlElement element, MetadataDocument metaDoc, bool IsTopLevelNode, bool IsRootOnly = false )
        {
            EditedXmlNode editedNode = new EditedXmlNode
            {
                UID = UIDSource.GetNext(),
                XmlNodeTagName = element.Name
            };
            XmlNodeList childNodes = element.ChildNodes;
            if ( childNodes.Count > 0 )
                foreach ( XmlNode node in childNodes )
                {
                    //ArcenDebugging.LogSingleLine( "Node: " + node.Name + " name=" + ((XmlElement)node).GetAttribute("name"), Verbosity.DoNotShow );
                    switch ( node.NodeType )
                    {
                        case XmlNodeType.Element:
                            EditedXmlNode? childResult = (EditedXmlNode?)ProcessXmlElement( (XmlElement)node, metaDoc, false ); //task.run on this? risk of losing the correct order of parts, so need a thread-safe structure                         
                            if ( childResult != null )
                            {
                                editedNode.ChildNodes.Add( childResult );
                                //dump2.Enqueue( $"Node = {node.Name}\t\tchildResult = {childResult.}" );
                            }
                            else
                                ArcenDebugging.LogSingleLine( "ERROR: Processing of " + ((XmlElement)node).GetAttribute( "key" ) + " node failed.", Verbosity.DoNotShow );
                            break;
                        case XmlNodeType.Comment:
                            EditedXmlComment childComment = new EditedXmlComment
                            {
                                Data = node.InnerText
                            };
                            editedNode.ChildNodes.Add( childComment );
                            break;
                        default:
                            string complaint = "Why do we have a " + node.NodeType + " directly under the element node?";
                            ArcenDebugging.LogSingleLine( complaint, Verbosity.DoNotShow );
                            return null;
                    }
                }
            if ( IsRootOnly )
                editedNode.IsRootOnly = true;
            XmlAttributeCollection attributes = element.Attributes;
            if ( attributes.Count > 0 )
            {
                foreach ( XmlAttribute attribute in attributes )
                {
                    EditedXmlAttribute att = new EditedXmlAttribute
                    {
                        Name = attribute.Name,
                        ValueOnDisk = attribute.Value
                    };
                    editedNode.Attributes.Add( att.Name, att );
                    //dump1.Enqueue( attribute.Name + "\t" + attribute.Value );
                    if ( metaDoc.CentralID != null && att.Name.ToLowerInvariant() == metaDoc.CentralID.Key )
                        editedNode.Attributes[att.Name].Type = AttributeType.String;

                    if ( IsTopLevelNode && editedNode.NodeCentralID == null && (string.Equals( att.Name, metaDoc.CentralID?.Key, StringComparison.InvariantCultureIgnoreCase ) || IsRootOnly) )
                    {
                        if ( IsRootOnly )
                        {
                            EditedXmlAttribute rootNode = new EditedXmlAttribute
                            {
                                Name = "name",
                                ValueOnDisk = "Root Node"
                            };
                            editedNode.NodeCentralID = rootNode;
                        }
                        else
                            editedNode.NodeCentralID = att;
                    }
                }
            }
            else
            {
                ArcenDebugging.LogSingleLine( "WARNING: attributes from node " + element.Name + " in file " + element.BaseURI + " are missing.", Verbosity.DoNotShow );
            }
            return editedNode;
        }

        //public static void DumpXmlData()
        //{
        //    string error = "\nattributesData contents: ";
        //    if ( !dump1.IsEmpty )
        //    {
        //        error += "\ndump1 contents: ";
        //        foreach ( string s in dump1 )
        //        {
        //            error += "\n" + s;
        //        }
        //    }
        //    if ( dump2.Count > 0 )
        //    {
        //        error += "\ndump2 contents: ";
        //        foreach ( string s in dump2 )
        //        {
        //            error += "\n" + s;
        //        }
        //    }
        //    ArcenDebugging.LogSingleLine( error, Verbosity.DoNotShow );
        //}
    }
}
