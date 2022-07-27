using System.Xml;

namespace ArcenXE.Utilities.MetadataProcessing
{
    public class MetadataDocument
    {
        public bool IsSingleRootTypeDocument { get; set; } = false;
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
        public string Name { get; set; } = string.Empty;

        private MetadataNodeLayer? topLevelNode;

        public void ParseDocument( string Filename, string sharedMetaDataFile )
        {
            XmlDocument mainDoc = new XmlDocument()
            {
                PreserveWhitespace = false
            };
            try
            {
                mainDoc.Load( Filename );
            }
            catch ( Exception e )
            {
                ArcenDebugging.LogErrorWithStack( e );
            }
            this.Name = Filename;

            //Decision time!  Is this a "single root" type document?
            if ( mainDoc != null )
            {
                XmlElement? mainRoot = mainDoc.DocumentElement;
                if ( mainRoot != null )
                {
                    topLevelNode = new MetadataNodeLayer( this );
                    if ( mainRoot.HasAttribute( "is_for_single_root" ) )
                        this.IsSingleRootTypeDocument = mainRoot.GetAttribute( "is_for_single_root" ).ToLowerInvariant() == "true";

                    if ( !this.IsSingleRootTypeDocument )
                    {
                        // We also need to load the SharedMetaData.metadata file, since this is not a single-root (ExternalData type) document
                        XmlDocument sharedDoc = new XmlDocument()
                        {
                            PreserveWhitespace = false
                        };
                        try
                        {
                            sharedDoc.Load( sharedMetaDataFile );
                        }
                        catch ( Exception e )
                        {
                            ArcenDebugging.LogErrorWithStack( e );
                        }

                        XmlElement? sharedRoot = sharedDoc?.DocumentElement;
                        //parse the shared data document first, if not a single-root document
                        topLevelNode.ParseLayer( sharedRoot );
                    }

                    //then parse our real data.
                    topLevelNode.ParseLayer( mainRoot );
                    topLevelNode.ProcessConditionals();
                    // for debugging
                    //topLevelNode.DumpLayerData();

                    //check for IsDataCopyIdentifierAlreadyRead still false; it has to be true by the end
                    if ( !this.IsSingleRootTypeDocument )
                        if ( !this.IsDataCopyIdentifierAlreadyRead )
                            ArcenDebugging.LogSingleLine( "Parsing error: \"is_central_identifier\" attribute in" + this.Name + "is absent. Please provide one.", Verbosity.ShowAsWarning );
                }
                else
                {
                    ArcenDebugging.LogSingleLine( "ERROR: Filename " + Filename + " has an invalid root element.", Verbosity.DoNotShow );
                    return;
                }
            }
            else
            {
                ArcenDebugging.LogSingleLine( "ERROR: Filename " + Filename + " is invalid and can't be read.", Verbosity.DoNotShow );
                return;
            }
        }
    }
}
