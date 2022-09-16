using System.Xml;
namespace ArcenXE.Utilities
{
    #region EditedXmlNode
    public class EditedXmlNode : IEditedXmlNodeOrComment, IEditedXmlElement
    {
        public uint UID { get; set; } = 0;
        public string XmlNodeTagName = string.Empty; // string that defines the tag in xml -- used for subnodes
        public EditedXmlAttribute? NodeCentralID = null; // if != null, then it's a top node
        public Dictionary<string, EditedXmlAttribute> Attributes = new Dictionary<string, EditedXmlAttribute>();
        public List<IEditedXmlNodeOrComment> ChildNodes = new List<IEditedXmlNodeOrComment>();
        public bool IsRootOnly = false;
        public string OuterXml { get; set; } = string.Empty;
        public bool IsComment => false;
        public bool IsDeleted { get; set; } = false;
        public UnionNode? RelatedUnionNode { get; set; } = null;

        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Label? CurrentViewControl;
    }
    #endregion

    #region EditedXmlComment
    public class EditedXmlComment : IEditedXmlNodeOrComment, IEditedXmlElement
    {
        public uint UID { get; set; } = 0;
        public string Data = string.Empty;
        public string OuterXml { get; set; } = string.Empty;
        public bool IsComment => true;
        public bool IsDeleted { get; set; } = false;
        public UnionNode? RelatedUnionNode { get; set; } = null;

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
        public AttributeType Type
        {
            get
            {
                if ( RelatedUnionAttribute != null )
                {
                    return RelatedUnionAttribute.MetaAttribute.Value.Type;
                }      
                return AttributeType.Unknown;
            }
        }
        public string? ValueOnDisk = null;
        public string? TempValue = null;
        public bool IsDeleted { get; set; } = false;
        public UnionAttribute? RelatedUnionAttribute { get; set; } = null;
        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Label? CurrentViewControl_Name;
        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Control? CurrentViewControl_Value;

        public bool GetHasChanges()
        {
            if ( this.TempValue == null )
                return false;
            return this.ValueOnDisk != this.TempValue;
        }

        public string? GetEffectiveValue()
        {
            if ( this.TempValue == null )
                return this.ValueOnDisk;
            else
                return this.TempValue;
        }
    }
    #endregion

    #region IEditedXml
    public interface IEditedXmlNodeOrComment
    {
        public uint UID { get; set; }
        public bool IsComment { get; }
        public UnionNode? RelatedUnionNode { get; set; }
        public string OuterXml { get; set; }
    }

    public interface IEditedXmlElement
    {
        public bool IsDeleted { get; set; }
    }
    #endregion

    #region UIDSource
    /// <summary>
    /// Start with value 1. Use 0 as null value.
    /// </summary>
    public static class UIDSource
    {
        private static uint NextID = 1;

        public static uint GetNext()
        {
            return Interlocked.Increment( ref NextID );
        }

        public static void Reset()
        {
            Interlocked.Exchange( ref NextID, 1 );
        }
    }
    #endregion

    #region TopNodeForVis
    public struct TopNodeForVis
    {
        public string VisName { get; set; }
        public uint UID { set; get; }
        public bool IsComment;

        public TopNodeForVis( string name, uint uid, bool isComment )
        {
            this.VisName = name;
            this.UID = uid;
            this.IsComment = isComment;
        }

        public override string ToString() => this.VisName;
    }
    #endregion
}
