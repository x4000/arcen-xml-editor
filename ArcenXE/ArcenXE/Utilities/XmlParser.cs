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
                            string complaint = "Why do we have a " + node.NodeType + " directly under the element node?";
                            ArcenDebugging.LogSingleLine( complaint, Verbosity.DoNotShow );
                            MessageBox.Show( complaint );
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

                    if ( IsTopLevelNode && editedNode.NodeName == null && string.Equals( att.Name, "name", StringComparison.CurrentCultureIgnoreCase ) )
                        editedNode.NodeName = att;
                }
            }
            return editedNode;
        }
    }

    #region EditedXmlNode
    public class EditedXmlNode : IEditedXmlNodeOrComment, IEditedXmlElement
    {
        public EditedXmlAttribute? NodeName = null; // if != null, then this is top node
        public List<EditedXmlAttribute> Attributes = new List<EditedXmlAttribute>();
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
        public ArcenXmlAttributeType Type = ArcenXmlAttributeType.Unknown; //to be filled by metadata
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

    public enum ArcenXmlAttributeType // use AttributeType?
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
