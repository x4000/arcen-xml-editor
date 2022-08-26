using System.Xml;

namespace ArcenXE.Utilities.MetadataProcessing
{
    public class MetadataDocument
    {
        public bool IsSingleRootTypeDocument { get; private set; } = false;
        private bool isDataCopyIdentifierAlreadyRead = false;
        public bool IsDataCopyIdentifierAlreadyRead
        {
            get => isDataCopyIdentifierAlreadyRead;
            set
            {
                //    if ( isDataCopyIdentifierAlreadyRead )
                //    {
                //        ArcenDebugging.LogSingleLine( "Set a 2nd time\nOld stack: " + workingStackStrace + "\nNew stack: " + Environment.StackTrace +
                //            "\nOld threadID: " + WorkingThreadID + "\nNew threadID: " + Thread.CurrentThread.ManagedThreadId, Verbosity.DoNotShow );
                //        return;
                //    }
                isDataCopyIdentifierAlreadyRead = value;
                //workingStackStrace = Environment.StackTrace;
                //WorkingThreadID = Thread.CurrentThread.ManagedThreadId;

            }
        }
        //private string workingStackStrace = string.Empty;
        //private long WorkingThreadID;
        public string MetadataFolder { get; private set; } = string.Empty;
        public string MetadataName { get; private set; } = string.Empty;
        public string NodeName { get; private set; } = string.Empty;

        public UnionNode? RelatedTopUnionNode { get; set; } = null;
        public MetadataNodeLayer? TopLevelNode { get; private set; } = null;

        public MetaAttribute_Base? CentralID { get; set; } = null;
        public MetaAttribute_Base? PartialId { get; set; } = null;
        public MetaAttribute_Base? DataCopyId { get; set; } = null;
        public MetaAttribute_Base? Description { get; set; } = null;
        public MetaAttribute_Base? UserFacingName { get; set; } = null;


        public void ParseDocument( string filename, string sharedMetaDataFile )
        {
            XmlDocument? mainDoc = Openers.GenericXmlFileLoader( filename );
            //Decision time!  Is this a "single root" type document?
            if ( mainDoc != null )
            {
                string? tempString = Path.GetFileNameWithoutExtension( Path.GetDirectoryName( filename ) );
                if ( tempString != null )
                    this.MetadataFolder = tempString;
                this.MetadataName = Path.GetFileNameWithoutExtension( filename );
                XmlElement? mainRoot = mainDoc.DocumentElement;
                if ( mainRoot != null )
                {
                    TopLevelNode = new MetadataNodeLayer( this );
                    if ( mainRoot.HasAttribute( "is_for_single_root" ) )
                        this.IsSingleRootTypeDocument = mainRoot.GetAttribute( "is_for_single_root" ).ToLowerInvariant() == "true";
                    else if ( mainRoot.HasAttribute( "node_name" ) )
                        this.NodeName = mainRoot.GetAttribute( "node_name" );
                    else
                        ArcenDebugging.LogSingleLine( $"Metadata file \n'{this.MetadataName}'\n is missing attribute 'node_name' in root. Please provide one.", Verbosity.DoNotShow );

                    if ( !this.IsSingleRootTypeDocument )
                    {
                        // We also need to load the SharedMetaData.metadata file, since this is not a single-root (ExternalData type) document
                        XmlDocument? sharedDoc = Openers.GenericXmlFileLoader( sharedMetaDataFile );
                        if ( sharedDoc != null )
                        {
                            XmlElement? sharedRoot = sharedDoc.DocumentElement;
                            //parse the shared data document first, if not a single-root document
                            if ( sharedRoot != null )
                                TopLevelNode.ParseLayer( sharedRoot );
                            else
                                ArcenDebugging.LogSingleLine( "ERROR: SharedMetadata - Filename " + filename + " has an invalid root element.", Verbosity.DoNotShow );
                        }
                        else
                            ArcenDebugging.LogSingleLine( "ERROR: SharedMetadata - Filename " + filename + " is invalid and can't be read.", Verbosity.DoNotShow );
                    }

                    //then parse our real data.
                    //ArcenDebugging.LogSingleLine( "Parsing specific MetaDocument ", Verbosity.DoNotShow );
                    TopLevelNode.ParseLayer( mainRoot );
                    TopLevelNode.ProcessConditionals();
                    // for debugging
                    //TopLevelNode.DumpLayerData();

                    //check for IsDataCopyIdentifierAlreadyRead still false; it has to be true by the end
                    if ( !this.IsSingleRootTypeDocument )
                        if ( !this.IsDataCopyIdentifierAlreadyRead )
                            ArcenDebugging.LogSingleLine( "Parsing error: \"is_central_identifier\" attribute in" + this.MetadataName + "is absent. Please provide one.", Verbosity.ShowAsWarning );
                }
                else
                {
                    ArcenDebugging.LogSingleLine( "ERROR: Filename " + filename + " has an invalid root element.", Verbosity.DoNotShow );
                    return;
                }
            }
            else
            {
                ArcenDebugging.LogSingleLine( "ERROR: Filename " + filename + " is invalid and can't be read.", Verbosity.DoNotShow );
                return;
            }
        }
    }
}
