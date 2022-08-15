using ArcenXE.Utilities;
using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE
{
    public static class MetadataStorage
    {
        public static MetadataDocument? CurrentVisMetadata = null;
        private readonly static Dictionary<string, MetadataDocument> allMetadatas = new Dictionary<string, MetadataDocument>(); // Dictionary<FolderName, MetadataDocument>

        public static MetadataDocument? GetMetadataDocumentByName( string tableAndFolderName )
        {
            if ( allMetadatas.Count == 0 )
                throw new Exception( "Called GetMetadataDocumentByName with 0 MetadataDocuments available!" );
            if ( allMetadatas.TryGetValue( tableAndFolderName, out MetadataDocument? metaDoc ) )
                return metaDoc;
            else
            {
                ArcenDebugging.LogSingleLine( $"Tried to find Metadata Document with '{tableAndFolderName}' name, but it wasn't found ", Verbosity.DoNotShow );
                return null;
            }
        }

        public static bool AddMetadataDocument( string tableAndFolderName, MetadataDocument metaDoc )
        {
            if ( allMetadatas.TryAdd( tableAndFolderName, metaDoc ) )
                return true;
            else
            {
                ArcenDebugging.LogSingleLine( $"Was unable to add {tableAndFolderName} metadata document to the dictionary.", Verbosity.DoNotShow );
                return false;
            }
        }

        public static void ClearAllMetadata()
        {        
            if ( allMetadatas.Count > 0)
                allMetadatas.Clear();
        }

    }
}
