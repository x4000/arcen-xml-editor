using System.Xml;
using ArcenXE.Utilities.MetadataProcessing.BooleanLogic;

namespace ArcenXE.Utilities.MetadataProcessing
{
    public class MetadataNodeLayer
    {
        public MetadataDocument ParentDoc;
        public string Name = string.Empty;

        public MetadataNodeLayer( MetadataDocument parentDoc )
        {
            this.ParentDoc = parentDoc;
        }

        private readonly Dictionary<string, BooleanLogicCheckerTree> ConditionalsTree = new Dictionary<string, BooleanLogicCheckerTree>();
        private readonly Dictionary<string, AttributeData_Base> AttributesData = new Dictionary<string, AttributeData_Base>();
        private readonly Dictionary<string, MetadataNodeLayer> SubNodes = new Dictionary<string, MetadataNodeLayer>();

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
                    foreach ( XmlNode node in nodesForThisLayer )
                        if ( node != null ) // if node is null then it must have been a comment
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
                        else
                            ArcenDebugging.LogSingleLine( "There is no nodes in " + layerRoot.OwnerDocument.Name + " !", Verbosity.DoNotShow );

                //now process these:

                //1: process outer conditionals 
                foreach ( XmlNode node in nodesConditional )
                {
                    BooleanLogicCheckerTree? result = BooleanLogicCheckerTreeParser.ProcessMetadataConditionalOuterShell( (XmlElement)node );

                    if ( result != null )
                        ConditionalsTree.TryAdd( result.Name, result );
                }
                //2: process attributes
                foreach ( XmlNode attNode in nodesAttribute )
                {
                    List<AttributeData_Base>? results = MetadataAttributeParser.ProcessMetadataAttributes( (XmlElement)attNode, this.ParentDoc );

                    if ( results != null )
                        foreach ( AttributeData_Base attributeData in results )
                            AttributesData.TryAdd( attributeData.Key, attributeData );
                }

                //3: process subnodes
                foreach ( XmlNode node in nodesSubNode )
                {
                    MetadataNodeLayer subNode = new MetadataNodeLayer( this.ParentDoc );
                    subNode.ParseLayer( (XmlElement)node );
                    this.SubNodes.Add( subNode.Name, subNode );
                }

                //4: process conditionals 
                foreach ( XmlElement node in nodesConditional )
                {                    
                    if ( ConditionalsTree.TryGetValue( node.GetAttribute("name"), out BooleanLogicCheckerTree? checkerTree ) )
                        BooleanLogicCheckerTreeParser.ProcessMetadataConditionals( node, checkerTree.RootGroup, AttributesData, true );
                }
            }
        }
    }
}
