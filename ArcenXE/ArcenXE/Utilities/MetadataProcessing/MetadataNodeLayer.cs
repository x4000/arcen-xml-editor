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
        private readonly List<XmlNode> nodesConditional = new List<XmlNode>();

        //for debugging
        private readonly List<string> dump1 = new List<string>();
        private readonly List<string> dump2 = new List<string>();


        public void ParseLayer( XmlElement? layerRoot )
        {
            if ( layerRoot != null )
            {
                if ( layerRoot.HasAttribute( "name" ) )
                    this.Name = layerRoot.GetAttribute( "name" );
                else if ( layerRoot.Name == "root" )
                    this.Name = "root";
                else
                    ArcenDebugging.LogSingleLine( "INFO: Metadata file \"" + layerRoot.BaseURI + "\" has no \"name\" attribute. Please provide one.", Verbosity.DoNotShow );

                List<XmlNode> nodesAttribute = new List<XmlNode>();
                List<XmlNode> nodesSubNode = new List<XmlNode>();

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
                                    //dump1.Add( ((XmlElement)node).GetAttribute( "key" ) );
                                    break;
                                case "sub_node":
                                    nodesSubNode.Add( node );
                                    break;
                                case "#comment": // do nothing, ignore it
                                    break;
                                default:
                                    ArcenDebugging.LogSingleLine( "WARNING: Node " + node.Name + " is not recognized!", Verbosity.DoNotShow );
                                    break;
                            }
                        else
                            ArcenDebugging.LogSingleLine( "ERROR: Null Node in the " + this.Name + " layer in this file" + layerRoot.BaseURI + " !", Verbosity.DoNotShow );
                else
                {
                    ArcenDebugging.LogSingleLine( "ERROR: Metadata file \"" + layerRoot.BaseURI + "\" is missing nodes in the " + this.Name + " layer. Please verify the file.", Verbosity.DoNotShow );
                    return;
                }
                //now process these:
                //1: process attributes
                foreach ( XmlNode attNode in nodesAttribute )
                {

                    MetadataAttributeParser.ProcessMetadataAttributes( (XmlElement)attNode, this.ParentDoc, out AttributeData_Base? result );

                    if ( result != null )
                        AttributesData.TryAdd( result.Key, result );
                    else
                        ArcenDebugging.LogSingleLine( "WARNING: AttributeData_Base returned from ProcessMetadataAttributes() is null!", Verbosity.DoNotShow );
                }

                //2: process subnodes
                foreach ( XmlNode node in nodesSubNode )
                {
                    MetadataNodeLayer subNode = new MetadataNodeLayer( this.ParentDoc );
                    subNode.ParseLayer( (XmlElement)node );
                    this.SubNodes.Add( subNode.Name, subNode );
                }
            }
            else
            {
                ArcenDebugging.LogSingleLine( "ERROR: layerRoot is null! No metadata will be processed for this file! " , Verbosity.DoNotShow );
            }
        }

        public void ProcessConditionals()
        {
            foreach ( XmlNode node in nodesConditional )
            {
                BooleanLogicCheckerTree? result = BooleanLogicCheckerTreeParser.ProcessMetadataConditionalOuterShell( (XmlElement)node );

                if ( result != null )
                {
                    ConditionalsTree.TryAdd( result.Name, result );
                    BooleanLogicCheckerTreeParser.ProcessMetadataConditionals( (XmlElement)node, result.RootGroup, AttributesData, true );
                }
                else
                    ArcenDebugging.LogSingleLine( "WARNING: BooleanLogicCheckerTree returned from ProcessMetadataConditionalOuterShell() is null!", Verbosity.DoNotShow );
            }

            foreach ( KeyValuePair<string, MetadataNodeLayer> layer in this.SubNodes )
            {
                layer.Value.ProcessConditionals();
            }
        }

        public void DumpLayerData() // for debugging
        {
            string error = "Parent Doc calling: " + this.ParentDoc.Name + " Layer name: " + this.Name;
            error += "\nattributesData contents: ";
            foreach ( KeyValuePair<string, AttributeData_Base> kv in this.AttributesData )
            {
                error += "\n" + kv.Key + "  " + (kv.Value != null);
            }
            if ( dump1.Count > 0 )
            {
                error += "\ndump1 contents: ";
                foreach ( string s in this.dump1 )
                {
                    error += "\n" + s;
                }
            }
            if ( dump2.Count > 0 )
            {
                error += "\ndump2 contents: ";
                foreach ( string s in this.dump2 )
                {
                    error += "\n" + s;
                }
            }
            ArcenDebugging.LogSingleLine( error, Verbosity.DoNotShow );
            foreach ( KeyValuePair<string, MetadataNodeLayer> layer in this.SubNodes )
            {
                layer.Value.DumpLayerData();
            }
        }
    }
}
