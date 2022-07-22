using System.Xml;

namespace ArcenXE.Utilities
{
    public class MetadataAttributeLayer
    {
        public MetadataDocument ParentDoc;
        public string Name = string.Empty;

        public MetadataAttributeLayer( MetadataDocument parentDoc )
        {
            this.ParentDoc = parentDoc;
        }

        private readonly Dictionary<string, MetadataAttributeLayer> SubNodes = new Dictionary<string, MetadataAttributeLayer>();

        public readonly List<AttributeData_Base> AttributeDataList = new List<AttributeData_Base>();

        public void ParseLayer( XmlElement? layerRoot )
        {
            List<XmlNode> nodesConditional = new List<XmlNode>();
            List<XmlNode> nodesAttribute = new List<XmlNode>();
            List<XmlNode> nodesSubNode = new List<XmlNode>();

            if ( layerRoot != null )
            {
                if ( layerRoot.HasAttribute( "name" ) )
                    this.Name = layerRoot.GetAttribute( "name" );

                XmlNodeList nodesForThisLayer = layerRoot.ChildNodes;
                if ( nodesForThisLayer.Count > 0 )
                {
                    foreach ( XmlNode node in nodesForThisLayer )
                    {
                        if ( node != null ) // if node is null then it must have been a comment
                        {
                            switch ( node.Name.ToLowerInvariant() )
                            {
                                case "conditional":
                                    nodesConditional.Add( node );
                                    break;
                                case "attribute":
                                    nodesAttribute.Add( node );
                                    break;
                                case "sub_node":
                                    nodesSubNode.Add( node );
                                    break;
                                case "#comment": // do nothing, ignore it
                                    break;
                                default:
                                    ArcenDebugging.LogSingleLine( "Node " + node.Name + " is not recognized!", Verbosity.DoNotShow );
                                    break;
                            }
                        }
                    }
                }
                else
                    ArcenDebugging.LogSingleLine( "There is no nodes in " + layerRoot.OwnerDocument.Name + " !", Verbosity.DoNotShow );

                //now process these:

                //1: process conditionals

                //2: process attributes
                foreach ( XmlNode attNode in nodesAttribute )
                {
                    List<AttributeData_Base>? results = MetadataAttributeParser.ProcessMetadataAttributes( (XmlElement)attNode, this.ParentDoc );

                    if ( results != null )
                        foreach ( AttributeData_Base attributeData in results )
                            AttributeDataList.Add( attributeData );
                }

                //3: process subnodes
                foreach ( XmlNode node in nodesSubNode )
                {
                    MetadataAttributeLayer subNode = new MetadataAttributeLayer( this.ParentDoc );
                    subNode.ParseLayer( (XmlElement)node );
                    this.SubNodes.Add( subNode.Name, subNode );
                }
            }
        }
    }
}
