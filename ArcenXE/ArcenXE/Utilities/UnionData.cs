using ArcenXE.Universal;
using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE.Utilities
{
    public class UnionNode : IUnionElement  // A UnionComment will have an empty UnionAttributes list
    {
        public UnionNode? ParentUnionNode { get; set; } = null;
        /// <summary>
        /// Create a new UnionTopNodeAttribute and assign it for top nodes
        /// </summary>
        public UnionTopNodeAttribute? NodeData = null;
        public readonly ActionList<UnionNode> UnionSubNodes;
        public readonly ActionList<UnionAttribute> UnionAttributes;
        public readonly MetadataDocument MetaDocument;
        public readonly MetadataNodeLayer MetaLayer;
        public IEditedXmlNodeOrComment? XmlNodeOrComment = null;
        public List<Control> Controls { get; set; } = new List<Control>();
        //public bool IsDeleted { get; set; } = false;
        public bool? IsComment
        {
            get
            {
                if ( XmlNodeOrComment == null )
                    return null;
                if ( XmlNodeOrComment.IsComment )
                    return true;
                else
                    return false;
            }
        }

        public UnionNode( MetadataNodeLayer metaLayer )
        {
            this.MetaLayer = metaLayer;
            this.MetaDocument = MetaLayer.ParentDoc;
            this.UnionSubNodes = new ActionList<UnionNode>( ( UnionNode newNode ) =>
            {
                newNode.ParentUnionNode = this;
            }, null );

            this.UnionAttributes = new ActionList<UnionAttribute>( ( UnionAttribute newAtt ) =>
            {
                newAtt.ParentUnionNode = this;
            }, null );
        }
    }

    public class UnionAttribute : IUnionElement
    {
        public UnionNode? ParentUnionNode { get; set; } = null;
        public KeyValuePair<string, MetaAttribute_Base> MetaAttribute;
        public EditedXmlAttribute? XmlAttribute = null;
        public List<Control> Controls { get; set; } = new List<Control>();
        //public bool IsDeleted { get; set; } = false;

        public UnionAttribute( KeyValuePair<string, MetaAttribute_Base> metaAttribute )
        {
            this.MetaAttribute = metaAttribute;
        }
    }

    public class UnionTopNodeAttribute
    {
        public readonly string CentralID;
        public readonly EditedXmlAttribute XmlAttribute;

        public UnionTopNodeAttribute( string centralID, EditedXmlAttribute xmlAttribute )
        {
            this.CentralID = centralID;
            this.XmlAttribute = xmlAttribute;
        }
    }

    public interface IUnionElement
    {
        public UnionNode? ParentUnionNode { get; }
        public List<Control> Controls { get; set; }
        //public bool IsDeleted { get; set; }
    }
}
