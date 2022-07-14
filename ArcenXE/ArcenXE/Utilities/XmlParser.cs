using System.Xml;

namespace ArcenXE.Utilities
{
    public class XmlParser
    {
        public IEditedXmlNodeOrComment? ProcessXmlElement( XmlElement element, bool IsTopLevelNode )
        {
            EditedXmlNode editedNode = new EditedXmlNode();

            XmlNodeList childNodes = element.ChildNodes;
            if ( childNodes.Count > 0 )
            {
                foreach ( XmlNode node in childNodes )
                {
                    switch ( node.NodeType )
                    {
                        case XmlNodeType.Element:
                            EditedXmlNode? childResult = (EditedXmlNode?)ProcessXmlElement( (XmlElement)node, false ); //task.run on this? risk of losing the correct order of parts, so need a thread-safe structure
                            if ( childResult != null )
                                editedNode.ChildNodes.Add( childResult );
                            break;
                        case XmlNodeType.Comment:
                            EditedXmlComment childComment = new EditedXmlComment
                            {
                                Data = node.InnerText
                            };
                            editedNode.ChildNodes.Add( childComment );
                            break;
                        default:
                            MessageBox.Show( "why do we have a " + node.NodeType + " directly under the element node?" );
                            return null;
                    }
                }
            }

            XmlAttributeCollection attributes = element.Attributes;
            if ( attributes.Count > 0 )
            {
                foreach ( XmlAttribute attribute in attributes )
                {
                    EditedXmlAttribute att = new EditedXmlAttribute
                    {
                        Name = attribute.Name,
                        Value = attribute.Value
                    };
                    editedNode.Attributes.Add( att );

                    if ( IsTopLevelNode && editedNode.NodeName == null && string.Equals( att.Name, "name" ) )
                        editedNode.NodeName = att;
                }
            }
            return editedNode;
        }
    }
    public class EditedXmlNode : IEditedXmlNodeOrComment
    {
        public EditedXmlAttribute? NodeName = null; // if != null, then this is top node
        public List<EditedXmlAttribute> Attributes = new List<EditedXmlAttribute>();
        public List<IEditedXmlNodeOrComment> ChildNodes = new List<IEditedXmlNodeOrComment>();

        public bool IsComment => false;
    }

    public class EditedXmlComment : IEditedXmlNodeOrComment
    {
        public string Data = string.Empty;

        public bool IsComment => true;
    }

    public class EditedXmlAttribute
    {
        public string Name = string.Empty;
        public ArcenXmlAttributeType Type = ArcenXmlAttributeType.Unknown; //to be filled by metadata
        public string Value = string.Empty;
    }

    public interface IEditedXmlNodeOrComment
    {
        public bool IsComment { get; }
    }

    public enum ArcenXmlAttributeType
    {
        Unknown = 0,
        Bool,
        String,
        Int,
        Int64,
        FInt,
        Float,
        DataTable,
        ArbitraryStringOptions,
        Length
    }

}
