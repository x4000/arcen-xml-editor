using System.Xml;
using ArcenXE.Utilities.MetadataProcessing.BooleanLogic;

namespace ArcenXE.Utilities.MetadataProcessing
{
    public class MetadataNodeLayer
    {
        public readonly MetadataDocument ParentDoc;
        public string Name = string.Empty;

        public MetadataNodeLayer( MetadataDocument parentDoc )
        {
            this.ParentDoc = parentDoc;
        }

        public UnionNode? RelatedUnionNode { get; set; } = null;

        public readonly Dictionary<string, BooleanLogicCheckerTree> ConditionalsTree = new Dictionary<string, BooleanLogicCheckerTree>();
        public readonly Dictionary<string, MetaAttribute_Base> AttributesData = new Dictionary<string, MetaAttribute_Base>();
        public readonly Dictionary<string, MetadataNodeLayer> SubNodes = new Dictionary<string, MetadataNodeLayer>();
        private readonly List<XmlNode> nodesConditional = new List<XmlNode>();

        //for debugging
        //private readonly List<string> dump1 = new List<string>();
        //private readonly List<string> dump2 = new List<string>();

        public void ParseLayer( XmlElement? layerRoot )
        {
            if ( layerRoot != null )
            {
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
                            ArcenDebugging.LogSingleLine( "ERROR: Null Node in the " + this.Name + " layer in this file" + this.ParentDoc.MetadataName + " !", Verbosity.DoNotShow );
                else
                {
                    ArcenDebugging.LogSingleLine( "ERROR: Metadata file \"" + this.ParentDoc.MetadataName + "\" is missing nodes in the " + this.Name + " layer. Please verify the file.", Verbosity.DoNotShow );
                    return;
                }
                //now process these:
                //1: process attributes
                foreach ( XmlNode attNode in nodesAttribute )
                {
                    MetadataAttributeParser.ProcessMetadataAttributes( (XmlElement)attNode, this.ParentDoc, out MetaAttribute_Base? result );

                    if ( result != null )
                        AttributesData.TryAdd( result.Key, result );
                    else
                        ArcenDebugging.LogSingleLine( "WARNING: MetaAttribute_Base returned from ProcessMetadataAttributes() is null!", Verbosity.DoNotShow );
                }

                //2: process subnodes
                foreach ( XmlNode node in nodesSubNode )
                {
                    MetadataNodeLayer subNode = new MetadataNodeLayer( this.ParentDoc );
                    subNode.ParseLayer( (XmlElement)node );
                    this.SubNodes.Add( subNode.Name, subNode );
                }

                if ( layerRoot.HasAttribute( ParentDoc.CentralID?.Key ?? "id" ) )
                    this.Name = layerRoot.GetAttribute( ParentDoc.CentralID?.Key ?? "id" );
                else if ( layerRoot.Name == "root" )
                    this.Name = "root";
                else
                    ArcenDebugging.LogSingleLine( "INFO: Metadata file \"" + this.ParentDoc.MetadataName + "\" has no \"name\" attribute. Please provide one.", Verbosity.DoNotShow );
            }
            else
            {
                ArcenDebugging.LogSingleLine( "ERROR: layerRoot is null! No metadata will be processed for this file! ", Verbosity.DoNotShow );
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

        //public void DumpLayerData() // for debugging
        //{
        //    string error = "Parent Doc calling: " + this.ParentDoc.MetadataName + " Layer name: " + this.Name;
        //    error += "\nattributesData contents: ";
        //    foreach ( KeyValuePair<string, MetaAttribute_Base> kv in this.AttributesData )
        //    {
        //        error += "\n" + kv.Key + "  " + (kv.Value != null);
        //    }
        //    if ( dump1.Count > 0 )
        //    {
        //        error += "\ndump1 contents: ";
        //        foreach ( string s in this.dump1 )
        //        {
        //            error += "\n" + s;
        //        }
        //    }
        //    if ( dump2.Count > 0 )
        //    {
        //        error += "\ndump2 contents: ";
        //        foreach ( string s in this.dump2 )
        //        {
        //            error += "\n" + s;
        //        }
        //    }
        //    ArcenDebugging.LogSingleLine( error, Verbosity.DoNotShow );
        //    foreach ( KeyValuePair<string, MetadataNodeLayer> layer in this.SubNodes )
        //    {
        //        layer.Value.DumpLayerData();
        //    }
        //}
    }
}
