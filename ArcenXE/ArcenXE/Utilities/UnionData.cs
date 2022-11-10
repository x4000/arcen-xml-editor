using ArcenXE.Universal;
using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE.Utilities
{
    public class UnionNode : IUnionElement  // A UnionComment will have an empty UnionAttributes list
    {
        /// <summary>
        /// Null for top nodes, referencing an UnionNode when it's a sub node
        /// </summary>
        public UnionNode? ParentUnionNode { get; set; } = null;
        /// <summary>
        /// Create a new UnionTopNodeAttribute and assign it for top nodes
        /// </summary>
        public UnionTopNodeAttribute? TopNodeData = null;
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

        public void CheckDataIntegrity( string extraDebugInfo = "", bool logWithStackTrace = false )
        {
            string output = string.Empty;
            if ( this.ParentUnionNode == null )
                output += "ParentUnionNode is NULL";
            else
                output += "ParentUnionNode is not NULL";
            output += "\n";

            if ( this.TopNodeData == null )
                output += "TopNodeData is NULL";
            else
                output += "TopNodeData is not NULL";
            output += "\n";

            if ( this.MetaDocument == null )
                output += "MetaDocument is NULL";
            else
                output += "MetaDocument is not NULL";
            output += "\n";

            if ( this.MetaLayer == null )
                output += "MetaLayer is NULL";
            else
                output += "MetaLayer is not NULL";
            output += "\n";

            if ( this.XmlNodeOrComment == null )
                output += "XmlNodeOrComment is NULL";
            else
                output += "XmlNodeOrComment is not NULL";
            output += "\n";
            output += "Extra Debug Info: " + extraDebugInfo + "\n";
            if ( logWithStackTrace )
                ArcenDebugging.LogWithStack( output, Verbosity.DoNotShow );
            else
                ArcenDebugging.LogSingleLine( output, Verbosity.DoNotShow );
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
