using System.Xml;
using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE.Utilities.XmlDataProcessing
{
    public class XmlParser
    {
        public IEditedXmlNodeOrComment? ProcessXmlElement( XmlElement element, MetadataDocument metaDoc, bool IsTopLevelNode )
        {
            EditedXmlNode editedNode = new EditedXmlNode();

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
                                editedNode.ChildNodes.Add( childResult );
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
            XmlAttributeCollection attributes = element.Attributes;
            if ( attributes.Count > 0 )
                foreach ( XmlAttribute attribute in attributes )
                {
                    EditedXmlAttribute att = new EditedXmlAttribute
                    {
                        Name = attribute.Name,
                        Value = attribute.Value
                    };
                    editedNode.Attributes.TryAdd( att.Name, att );
#pragma warning disable CS8602
                    if ( att.Name.ToLowerInvariant() == metaDoc.CentralID.Key )
                        editedNode.Attributes[att.Name].Type = AttributeType.String;
#pragma warning restore CS8602

                    if ( IsTopLevelNode && editedNode.NodeCentralID == null && string.Equals( att.Name, metaDoc.CentralID.Key, StringComparison.InvariantCultureIgnoreCase) )
                        editedNode.NodeCentralID = att;
                }
            else
            {
                ArcenDebugging.LogSingleLine( "WARNING: attributes from node " + element.Name + " in file " + element.BaseURI + " are missing.", Verbosity.DoNotShow );
            }
            return editedNode;
        }
    }

    #region EditedXmlNode
    public class EditedXmlNode : IEditedXmlNodeOrComment, IEditedXmlElement
    {
        public EditedXmlAttribute? NodeCentralID = null; // if != null, then this is a top node
        public Dictionary<string, EditedXmlAttribute> Attributes = new Dictionary<string, EditedXmlAttribute>(); // maybe switch to list for performance
        public List<IEditedXmlNodeOrComment> ChildNodes = new List<IEditedXmlNodeOrComment>();

        public bool IsComment => false;

        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Label? CurrentViewControl;
    }
    #endregion

    #region EditedXmlComment
    public class EditedXmlComment : IEditedXmlNodeOrComment, IEditedXmlElement
    {
        public string Data = string.Empty;

        public bool IsComment => true;

        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Control? CurrentViewControl;
    }
    #endregion

    #region EditedXmlAttribute
    public class EditedXmlAttribute : IEditedXmlElement
    {
        public string Name = string.Empty;
        public AttributeType Type = AttributeType.Unknown; //to be filled by metadata
        public string Value = string.Empty;

        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Label? CurrentViewControl_Name;
        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Control? CurrentViewControl_Value;
    }
    #endregion

    #region IEditedXml
    public interface IEditedXmlNodeOrComment
    {
        public bool IsComment { get; }
    }

    public interface IEditedXmlElement
    {

    }
    #endregion
}
