using System.Xml;
using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Utilities.XmlDataProcessing;

namespace ArcenXE.Utilities
{
    public static class TopNodesCaching
    {
        private readonly static Dictionary<string, CachedTopNodeList> AllCachedTopNodes = new Dictionary<string, CachedTopNodeList>(); // Dictionary<FolderName, CachedTopNodeList>

        /// <summary>
        /// Returns the Top Nodes given a folder/dataTable name through a MetadataDocument, and if they don't exist, create a cache for that folder.
        /// </summary>
        public static List<TopNode>? GetAllNodesForDataTable( MetadataDocument metaDoc )
        {
            if ( AllCachedTopNodes.TryGetValue( metaDoc.MetadataFolder, out CachedTopNodeList? cachedTopNodeList ) )
                return cachedTopNodeList.GetNodes( metaDoc );
            else
            {
                CachedTopNodeList cachedNodeList = new CachedTopNodeList();
                cachedNodeList.FolderName = metaDoc.MetadataFolder;
                AllCachedTopNodes.Add( cachedNodeList.FolderName, cachedNodeList );
                return cachedNodeList.GetNodes( metaDoc );
            }
        }

        private readonly static XmlParser parser = new XmlParser();

        private static void ParseAllTopNodesForNodeDropdown( MetadataDocument metaDoc, CachedTopNodeList cachedTopNodeList )
        {
            XmlDataTable? dataTable = XmlRootFolders.GetXmlDataTableByName( metaDoc.MetadataFolder );
            if ( dataTable == null )
                return; // complain

            foreach ( XmlDataTableFile file in dataTable.Files )
                FillCachedNode( metaDoc, cachedTopNodeList.nodes, file.FullFilePath );

            cachedTopNodeList.LastRefreshed = DateTime.Now;
        }

        #region FillCachedNode
        private static void FillCachedNode( MetadataDocument metaDoc, List<TopNode> listToAddTo, string filePath )
        {
            XmlDocument? xmlDocument = Openers.GenericXmlFileLoader( filePath );
            if ( xmlDocument == null ) // invert if and put complaint + continue, remove else and put normal code
            {
                ArcenDebugging.LogSingleLine( "WARNING: XML with path:\n" + filePath + "\nis invalid and can't be read.", Verbosity.DoNotShow );
                return;
            }

            XmlElement? root = xmlDocument.DocumentElement;
            if ( root == null )
            {
                ArcenDebugging.LogSingleLine( "WARNING: XML with path:\n" + filePath + "\nhas an invalid root element.", Verbosity.DoNotShow );
                return;
            }

            if ( root.ChildNodes.Count <= 0 )
            {
                ArcenDebugging.LogSingleLine( $"WARNING: Skipped root element of XML with path:\n{filePath}\nbecause it has no top nodes", Verbosity.DoNotShow );
                return;
            }

            IEditedXmlNodeOrComment? nodeOrComment = null;
            foreach ( XmlNode element in root.ChildNodes )
            {
                switch ( element.NodeType )
                {
                    case XmlNodeType.Element:
                        nodeOrComment = parser.ProcessXmlElement( (XmlElement)element, metaDoc, true );
                        if ( nodeOrComment == null || nodeOrComment.IsComment )
                        {
#pragma warning disable CS8602
                            ArcenDebugging.LogSingleLine( $"INFO: Skipped element {((XmlElement)element).GetAttribute( metaDoc.UserFacingName.Key )} inside {((XmlElement)element).BaseURI} ", Verbosity.DoNotShow );
#pragma warning restore CS8602
                            continue;
                        }
                        break;
                    case XmlNodeType.Comment:
                        break;
                }
                if ( nodeOrComment == null )
                    return;

                EditedXmlNode node;
                node = (EditedXmlNode)nodeOrComment;
                if ( node.NodeCentralID != null )
                {
                    TopNode topNode = new TopNode();
                    if ( node.NodeCentralID.ValueOnDisk != null ) // need to look into temp value
                        topNode.CentralID = node.NodeCentralID.ValueOnDisk;
                    topNode.UserFacingName = string.Empty;
                    topNode.Description = string.Empty;

                    if ( metaDoc.UserFacingName != null )
                    {
                        if ( node.Attributes.TryGetValue( metaDoc.UserFacingName.Key, out EditedXmlAttribute? attribute ) && attribute.ValueOnDisk != null )
                            topNode.UserFacingName = attribute.ValueOnDisk;
                    }
                    if ( metaDoc.Description != null )
                    {
                        if ( node.Attributes.TryGetValue( metaDoc.Description.Key, out EditedXmlAttribute? attribute ) && attribute.ValueOnDisk != null )
                            topNode.Description = attribute.ValueOnDisk;
                    }
                    listToAddTo.Add( topNode );
                }
                else { }
            }
        }
        #endregion

        public class CachedTopNodeList
        {
            public string FolderName = string.Empty;
            public DateTime LastRefreshed = DateTime.UnixEpoch;
            internal readonly List<TopNode> nodes = new List<TopNode>();
            public List<TopNode> GetNodes( MetadataDocument metaDoc )
            {
                int RefreshTimeLimitInSeconds = 2;
                if ( LastRefreshed == DateTime.UnixEpoch || (DateTime.Now - LastRefreshed).TotalSeconds > RefreshTimeLimitInSeconds )
                {
                    nodes.Clear();
                    if ( MetadataStorage.CurrentVisMetadata != null )
                        ParseAllTopNodesForNodeDropdown( metaDoc, this );
                }
                return nodes;
            }
        }

        public class TopNode
        {
            public string CentralID = string.Empty;
            public string UserFacingName = string.Empty;
            public string Description = string.Empty;
        }
    }
}
