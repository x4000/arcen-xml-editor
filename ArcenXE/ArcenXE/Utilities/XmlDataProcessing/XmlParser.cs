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
            if ( IsTopLevelNode )
                editedNode.OuterXml = element.OuterXml;

            XmlNodeList childNodes = element.ChildNodes;
            if ( (IsTopLevelNode && !IsRootOnly) || (!IsTopLevelNode && childNodes.Count > 0) )
                foreach ( XmlNode node in childNodes )
                {
                    //ArcenDebugging.LogSingleLine( "Node: " + node.Name + " name=" + ((XmlElement)node).GetAttribute("name"), Verbosity.DoNotShow );
                    switch ( node.NodeType )
                    {
                        case XmlNodeType.Element:
                            EditedXmlNode? childResult = (EditedXmlNode?)ProcessXmlElement( (XmlElement)node, metaDoc, false ); //task.run on this? risk of losing the correct order of parts, and need a thread-safe structure
                            if ( childResult != null )
                            {
                                editedNode.ChildNodes.Add( childResult );
                                //dump2.Enqueue( $"Node = {node.Name}\t" );
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
                        case XmlNodeType.Whitespace:
                        case XmlNodeType.SignificantWhitespace:
                            break;
                        default:
                            string complaint = "Why do we have a " + node.NodeType + " directly under the element node?";
                            ArcenDebugging.LogSingleLine( complaint, Verbosity.DoNotShow );
                            return null;
                    }
                }                

            XmlAttributeCollection attributes = element.Attributes;
            if ( attributes.Count > 0 )
            {
                //ArcenDebugging.LogSingleLine( "it's almost x3 happening", Verbosity.DoNotShow );
                foreach ( XmlAttribute attribute in attributes )
                {
                    EditedXmlAttribute att = new EditedXmlAttribute
                    {
                        Name = attribute.Name,
                        ValueOnDisk = attribute.Value
                    };
                    editedNode.Attributes.Add( att.Name, att );
                    //dump1.Enqueue( attribute.Name + "\t" + attribute.Value );
                    //if ( metaDoc.CentralID != null && att.Name.ToLowerInvariant() == metaDoc.CentralID.Key )
                    //    editedNode.Attributes[att.Name].Type = AttributeType.String;
                    //ArcenDebugging.LogSingleLine( "it's almost x2 happening", Verbosity.DoNotShow );

                    if ( IsTopLevelNode && editedNode.NodeCentralID == null && (string.Equals( att.Name, metaDoc.CentralID?.Key, StringComparison.InvariantCultureIgnoreCase ) || IsRootOnly) )
                    {
                        //ArcenDebugging.LogSingleLine( "it's almost happening", Verbosity.DoNotShow );
                        if ( IsRootOnly )
                        {
                            //ArcenDebugging.LogSingleLine( "it's happening", Verbosity.DoNotShow );
                            EditedXmlAttribute rootNode = new EditedXmlAttribute
                            {
                                Name = "id",
                                ValueOnDisk = "Root Node"
                            };
                            editedNode.IsRootOnly = true;
                            editedNode.NodeCentralID = rootNode;
                        }
                        else
                            editedNode.NodeCentralID = att;
                    }
                }
            }
            else
                ArcenDebugging.LogSingleLine( "WARNING: attributes from node " + element.Name + " in file " + element.BaseURI + " are missing.", Verbosity.DoNotShow );
            return editedNode;
        }

        #region DumpXmlData
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
        #endregion
    }
}
