using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using System.Xml.Linq;
using ArcenXE.Universal;
using ArcenXE.Utilities;
using ArcenXE.Utilities.MessagesToMainThread;
using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Utilities.XmlDataProcessing;
using ArcenXE.Visualization;

namespace ArcenXE
{
    public sealed partial class MainWindow : Form
    {
#pragma warning disable CA2211
#pragma warning disable CS8618
        public static MainWindow Instance;
#pragma warning restore CS8618
#pragma warning restore CA2211

        public readonly ConcurrentQueue<IBGMessageToMainThread> MessagesToFrontEnd = new ConcurrentQueue<IBGMessageToMainThread>();

        public readonly Dictionary<uint, IEditedXmlNodeOrComment> CurrentXmlTopNodesForVis = new Dictionary<uint, IEditedXmlNodeOrComment>(); // merge with MetadataStorage and make it Storage

        public IEditedXmlNodeOrComment? XmlElementCurrentlyBeingEdited; //updated with the current node being selected/edited

        #region ErrorLogValues
        private int errorsWrittenToLog = 0;
        public int ErrorsWrittenToLog
        {
            get => errorsWrittenToLog;
            set
            {
                errorsWrittenToLog = value;
                ErrorLogToolStripButtonCounterUpdate();
            }
        }
        #endregion

        #region SelectedIndexes
        public int SelectedFolderIndex { get; private set; } = -1;
        public XmlDataTableFile? SelectedFile { get; private set; } = null;
        public int SelectedTopNodeIndex { get; set; } = -1;
        #endregion

        public MainWindow()
        {
            Instance ??= this;
            InitializeComponent();
        }

        private void MainTimer_Tick( object sender, EventArgs e )
        {
            ArcenDebugging.DumpAllPriorDelayedSingleLines();

            this.MainThreadLoop();

            ArcenDebugging.DumpAllPriorDelayedSingleLines();
        }

        private int lastMetatadaCountHasDoneLoadFor = 0;

        private void MainThreadLoop()
        {
            while ( this.MessagesToFrontEnd.TryDequeue( out IBGMessageToMainThread? message ) )
            {
                if ( message != null )
                    message.ProcessMessageOnMainThread();
            }
            int currentLoadingXmlFiles = MetadataLoader.NumberOfMetaDatasStillLoading;
            if ( currentLoadingXmlFiles == 0 && MetadataLoader.NumberOfMetadataLoadingStarts > lastMetatadaCountHasDoneLoadFor &&
                MetadataLoader.IgnoreMetaDataStartsUntil < DateTime.Now )
            {
                OnAllMetadataLoaded();
                lastMetatadaCountHasDoneLoadFor = MetadataLoader.NumberOfMetadataLoadingStarts;
            }
            //this.ErrorLogToolStripButton.Text = "Waiting on: " + currentLoaddingXmlFiles;            
        }

        private void OnAllMetadataLoaded()
        {
            XmlRootFolders.ResetXmlFoldersAndFilesToSearch( ProgramPermanentSettings.MainPath );
            FillFolderList();
        }

        private void MainWindow_Load( object sender, EventArgs e )
        {
            this.ErrorLogToolStripButton.Text = "Error List: " + ErrorsWrittenToLog;
            ProgramPermanentSettings.SetPaths();
        }

        private void LoadMeta_Click( object sender, EventArgs e )
        {
            _ = Openers.OpenFolderDialogToSelectRootFolder( ProgramPermanentSettings.MainPath );
        }

        #region Folder
        private void FolderToolStripMenuItem_Click( object sender, EventArgs e )
        {
            _ = Openers.OpenFolderDialogToSelectRootFolder();
        }

        private void OpenFolderToolStripButton_Click( object sender, EventArgs e )
        {
            _ = Openers.OpenFolderDialogToSelectRootFolder();
        }

        public void FillFolderList()
        {
            if ( this.FolderList.Items.Count > 0 )
                this.FolderList.Items.Clear();
            List<string> dataTableNames = XmlRootFolders.GetXmlDataTableNames();
            foreach ( string tableAndFolderName in dataTableNames )
                this.FolderList.Items.Add( tableAndFolderName );
        }

        private void FolderList_SelectedIndexChanged( object sender, EventArgs e )
        {
            int previouslySelectedIndex = this.SelectedFolderIndex;
            this.SelectedFolderIndex = this.FolderList.SelectedIndex;
            if ( this.SelectedFolderIndex != -1 && previouslySelectedIndex != this.SelectedFolderIndex )
            {
                // reset to initial state
                if ( this.TopNodesList.Items.Count > 0 )
                {
                    TopNodesList.DataSource = null;
                    TopNodesList.ValueMember = null;
                    this.TopNodesList.Items.Clear();
                }

                List<string> dataTableNames = XmlRootFolders.GetXmlDataTableNames();
                string selectedItem = dataTableNames[this.SelectedFolderIndex];
                MetadataStorage.CurrentVisMetadata = MetadataStorage.GetMetadataDocumentByName( selectedItem );

                XmlDataTable? table = XmlRootFolders.GetXmlDataTableByName( selectedItem );
                if ( table != null )
                    this.FillFileList( table );
            }
        }
        #endregion

        #region File
        private void FileToolStripMenuItem_Click( object sender, EventArgs e )
        {
            Openers.OpenFileDialog();
        }

        public void FillFileList( XmlDataTable xmlDataTable ) //todo colour coding based on origin (basegame (to not colour), dlc, mod)
        {
            if ( this.FileList.Items.Count > 0 )
                this.FileList.Items.Clear();

            this.SelectedFile = null;

            foreach ( XmlDataTableFile file in xmlDataTable.Files )
                this.FileList.Items.Add( file );
        }

        private void FileList_SelectedIndexChanged( object sender, EventArgs e )
        {
            XmlDataTableFile? previouslySelectedIndex = this.SelectedFile;
            this.SelectedFile = (XmlDataTableFile)this.FileList.SelectedItem;
            //ArcenDebugging.LogSingleLine( "FileList.SelectedIndex: " + FileList.SelectedIndex, Verbosity.DoNotShow );
            if ( this.SelectedFile != null && previouslySelectedIndex != this.SelectedFile ) //todo: introduce lazy loading of all xml files
            {
#pragma warning disable CS8604
                XmlLoader.LoadXml( this.SelectedFile.FullFilePath, MetadataStorage.CurrentVisMetadata );
#pragma warning restore CS8604
                //foreach ( IEditedXmlNodeOrComment item in this.CurrentXmlForVis )
                //{
                //    DumpAllVisXml( item );
                //}
            }
        }
        #endregion

        #region TopNodes
        public void FillTopNodesList()
        {
            if ( XmlLoader.NumberOfDatasStillLoading == 0 ) // extra safety control - not strictly necessary
            {
                // reset to initial state
                if ( this.TopNodesList.Items.Count > 0 )
                {
                    TopNodesList.DataSource = null;
                    TopNodesList.ValueMember = null;
                    this.TopNodesList.Items.Clear();
                }

                List<TopNodeForVis> topNodesForVis = new List<TopNodeForVis>();
                foreach ( KeyValuePair<uint, IEditedXmlNodeOrComment> kv in this.CurrentXmlTopNodesForVis )
                {
                    if ( kv.Value.IsComment ) // colour comments in green
                        topNodesForVis.Add( new TopNodeForVis( ((EditedXmlComment)kv.Value).Data, kv.Value.UID, true ) );
                    else
                    {
                        EditedXmlAttribute? att = ((EditedXmlNode)kv.Value).NodeCentralID;
                        if ( att != null && att.ValueOnDisk != null )
                            topNodesForVis.Add( new TopNodeForVis( att.ValueOnDisk, kv.Value.UID, false ) ); //change to GetEffectiveValue() ?
                    }
                }
                if ( topNodesForVis.Count == 0 )
                {
                    string noNodes = "There are no nodes to display in this file!";
                    this.TopNodesList.Items.Add( noNodes );
                    return;
                }
                TopNodesList.DataSource = topNodesForVis; // possibility for data islands?
                TopNodesList.DisplayMember = "VisName";
                TopNodesList.ValueMember = "UID";
            }
        }
        private void TopNodesList_SelectedIndexChanged( object sender, EventArgs e )
        {
            this.SelectedTopNodeIndex = this.TopNodesList.SelectedIndex;
            if ( this.SelectedTopNodeIndex != -1 )
                CallXmlVisualizer();
        }

        /// <summary>
        /// IEditedXmlNodeOrComment used for Root Only XML files 
        /// </summary>
        /// <param name="element"></param>
        public void CallXmlVisualizer( IEditedXmlNodeOrComment? element = null )
        {
            XmlVisualizer visualizer = new XmlVisualizer();
            if ( element == null && this.CurrentXmlTopNodesForVis.Count > 0 && this.TopNodesList.Items[0].ToString() != "There are no nodes to display in this file!" )
            {
                int numberOfMetaDatasStillLoading = MetadataLoader.NumberOfMetaDatasStillLoading;
                if ( numberOfMetaDatasStillLoading == 0 )
                {
                    uint key = ((TopNodeForVis)this.TopNodesList.SelectedItem).UID;
                    if ( this.CurrentXmlTopNodesForVis.TryGetValue( key, out this.XmlElementCurrentlyBeingEdited ) )
                    {
                        ApplyAttributeTypeToEditedXml( this.XmlElementCurrentlyBeingEdited );
                        visualizer.VisualizeSelectedNode( this.XmlElementCurrentlyBeingEdited, MetadataStorage.CurrentVisMetadata?.TopLevelNode, true );
                    }
                }
                else
                    //todo: needs new static class
                    MessageBox.Show( $"There are still {numberOfMetaDatasStillLoading} metadata files being loaded in memory. Try again in moment.", "Metadata still loading", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
            if ( element != null )
            {
                XmlElementCurrentlyBeingEdited = element;
                visualizer.VisualizeSelectedNode( element, MetadataStorage.CurrentVisMetadata?.TopLevelNode );
            }
        }

        private static void ApplyAttributeTypeToEditedXml( IEditedXmlNodeOrComment element )
        {
            if ( element.IsComment )
                return;
            else
            {
                if ( MetadataStorage.CurrentVisMetadata != null && MetadataStorage.CurrentVisMetadata.TopLevelNode != null )
                {
                    foreach ( KeyValuePair<string, EditedXmlAttribute> dataAttribute in ((EditedXmlNode)element).Attributes )
                    {
                        if ( MetadataStorage.CurrentVisMetadata.TopLevelNode.AttributesData.TryGetValue( dataAttribute.Value.Name, out MetaAttribute_Base? metadata ) )
                        {
                            if ( metadata != null )
                            {
                                dataAttribute.Value.Type = metadata.Type;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region ErrorButton
        private void ErrorLogToolStripButton_Click( object sender, EventArgs e )
        {
            ProcessStartInfo debugLogStartInfo = new ProcessStartInfo
            {
                FileName = Environment.CurrentDirectory.Replace( @"bin\Debug\net6.0-windows", @"logs\" ) + "XEDebugLog.txt",
                UseShellExecute = true
            };
            Process.Start( debugLogStartInfo );
            this.ErrorLogToolStripButton.ForeColor = Color.Black;
        }

        private void ErrorLogToolStripButtonCounterUpdate()
        {
            this.ErrorLogToolStripButton.Text = "Error List: " + this.ErrorsWrittenToLog;
            this.ErrorLogToolStripButton.ForeColor = Color.Red;
        }
        #endregion

        private void ExplorerToolStripMenuItem_Click( object sender, EventArgs e )
        {
            Explorer explorer = new Explorer();
            explorer.Show();
        }

        private void Button1_Click( object sender, EventArgs e )
        {
            Openers.OpenFileDialog();
        }

        private void SaveToolStripButton_Click( object sender, EventArgs e )
        {
            //ArcenDebugging.LogSingleLine( $"Step 1", Verbosity.DoNotShow );
            XmlWriter xmlOutput = new XmlWriter();
            if ( XmlElementCurrentlyBeingEdited != null && !XmlElementCurrentlyBeingEdited.IsComment )
            {
                //ArcenDebugging.LogSingleLine( $"Step 2", Verbosity.DoNotShow );

                EditedXmlNode node = (EditedXmlNode)XmlElementCurrentlyBeingEdited;
                if ( node.IsRootOnly )
                {
                    //ArcenDebugging.LogSingleLine( $"Step 3.a", Verbosity.DoNotShow );

                    xmlOutput.StartOpenNode( "root", false ).NewLine( XmlLeadingWhitespace.None );
                    bool justInsertedLineBreak = false;

                    foreach ( KeyValuePair<string, EditedXmlAttribute> att in node.Attributes )
                    {
                        //ArcenDebugging.LogSingleLine( $"Step 3.b.1", Verbosity.DoNotShow );
                        MetaAttribute_Base? metaAttribute = att.Value.RelatedUnionAttribute?.MetaAttribute.Value;
                        if ( metaAttribute == null )
                        {
                            ArcenDebugging.LogSingleLine( $"RelatedUnionAttribute inside Edited Attribute {att.Key} is null. Can't save this attribute to file!", Verbosity.DoNotShow );
                            continue;
                        }
                        XmlAttLeadInOut leadIn = CalculateLineBreakBefore( metaAttribute, ref justInsertedLineBreak, xmlOutput );
                        string? effectiveValue = att.Value.GetEffectiveValue();
                        if ( effectiveValue == null )
                        {
                            ArcenDebugging.LogSingleLine( $"GetEffectiveValue() inside Edited Attribute {att.Key} returned null. Can't save this attribute to file!", Verbosity.DoNotShow );
                            continue;
                        }
                        //ArcenDebugging.LogSingleLine( $"Step 3.b.2", Verbosity.DoNotShow );

                        switch ( att.Value.Type )
                        {
                            case AttributeType.Bool:
                                xmlOutput.BoolAttribute( leadIn, att.Key, bool.Parse( effectiveValue ) );
                                break;
                            case AttributeType.BoolInt:
                            case AttributeType.Int:
                                xmlOutput.IntAttribute( leadIn, att.Key, int.Parse( effectiveValue ) );
                                break;
                            case AttributeType.String:
                            case AttributeType.StringMultiLine:
                            case AttributeType.ArbitraryString:
                            case AttributeType.ArbitraryNode:
                            case AttributeType.NodeList:
                            case AttributeType.FolderList:
                                xmlOutput.StringAttribute( leadIn, att.Key, effectiveValue );
                                break;
                            case AttributeType.Float:
                                xmlOutput.FloatAttribute( leadIn, att.Key, float.Parse( effectiveValue ), ((MetaAttribute_Float)metaAttribute).MinimumDigits );
                                break;
                            case AttributeType.Point:
                                {
                                    string? coordinates = effectiveValue;
                                    if ( coordinates != null )
                                    {
                                        string[] splitCoord = coordinates.Split( "," );
                                        xmlOutput.PointAttribute( leadIn, att.Key, ArcenPoint.Create( int.Parse( splitCoord[0] ), int.Parse( splitCoord[1] ) ) );
                                    }
                                }
                                break;
                            case AttributeType.Vector2:
                                {
                                    string? coordinates = effectiveValue;
                                    if ( coordinates != null )
                                    {
                                        string[] splitCoord = coordinates.Split( "," );
                                        xmlOutput.Vector2Attribute( leadIn, att.Key, new Vector2( float.Parse( splitCoord[0] ), float.Parse( splitCoord[1] ) ),
                                                                ((MetaAttribute_Vector2)metaAttribute).x.MinimumDigits );
                                    }
                                }
                                break;
                            case AttributeType.Vector3:
                                {
                                    string? coordinates = effectiveValue;
                                    if ( coordinates != null )
                                    {
                                        string[] splitCoord = coordinates.Split( "," );
                                        xmlOutput.Vector3Attribute( leadIn, att.Key, new Vector3( float.Parse( splitCoord[0] ), float.Parse( splitCoord[1] ), float.Parse( splitCoord[2] ) ),
                                                                ((MetaAttribute_Vector3)metaAttribute).x.MinimumDigits );
                                    }
                                }
                                break;
                        }
                        //ArcenDebugging.LogSingleLine( $"Step 4", Verbosity.DoNotShow );

                        xmlOutput.HandleAttributeLeadInOut( CalculateLineBreakAfter( metaAttribute, ref justInsertedLineBreak ) );
                    }
                    xmlOutput.FinishOpenNode( true );
                    xmlOutput.CloseNode( "root", false );
                }
                else
                {
                    // loop attributes (like above), then do the same for all the subnodes
                    //ArcenDebugging.LogSingleLine( $"Step 3.b", Verbosity.DoNotShow );
                    xmlOutput.StartOpenNode( "root", true ).NewLine( XmlLeadingWhitespace.None );
                }

                //write to file
                if ( node.RelatedUnionNode == null )
                {
                    ArcenDebugging.LogSingleLine( $"RelatedUnionNode inside Edited Node {node} is null. Can't save this node to file!", Verbosity.DoNotShow );
                    return;
                }
                //ArcenDebugging.LogSingleLine( $"Step 5", Verbosity.DoNotShow );
                //ArcenDebugging.LogSingleLine( $"{node.RelatedUnionNode.MetaDocument.MetadataFolder}", Verbosity.DoNotShow );
                File.WriteAllText( ProgramPermanentSettings.MainPath + @"\" + node.RelatedUnionNode.MetaDocument.MetadataFolder + @"\" + node.RelatedUnionNode.MetaDocument.MetadataFolder + ".xml", xmlOutput.GetFinishedXmlDocument() ); //add file name field in vis?
            }
        }

        private static XmlAttLeadInOut CalculateLineBreakBefore( MetaAttribute_Base metaAttribute, ref bool justInsertedLineBreak, XmlWriter xmlWriter )
        {
            bool requireLineBreak = xmlWriter.CalculateIfNewLineIsRequired( (ushort)metaAttribute.ContentWidthPx );

            switch ( metaAttribute.LinebreakBefore )
            {
                case LineBreakType.Always:
                    if ( !justInsertedLineBreak && !requireLineBreak )
                    {
                        justInsertedLineBreak = true;
                        return XmlAttLeadInOut.Linebreak;
                    }
                    else
                    {
                        justInsertedLineBreak = false;
                        return XmlAttLeadInOut.Space;
                    }
                case LineBreakType.PreferNot:
                    if ( requireLineBreak )
                    {
                        justInsertedLineBreak = true;
                        return XmlAttLeadInOut.Linebreak;
                    }
                    justInsertedLineBreak = false;
                    return XmlAttLeadInOut.Space;
            }
            return XmlAttLeadInOut.Space;
        }

        private static XmlAttLeadInOut CalculateLineBreakAfter( MetaAttribute_Base metaAttribute, ref bool justInsertedLineBreak )
        {
            switch ( metaAttribute.LinebreakAfter )
            {
                case LineBreakType.Always:
                    justInsertedLineBreak = true;
                    return XmlAttLeadInOut.Linebreak;
                case LineBreakType.PreferNot:
                    justInsertedLineBreak = false;
                    return XmlAttLeadInOut.Space;
            }
            return XmlAttLeadInOut.Space;
        }

        #region Debugging
        //private void DumpAllVisXml( IEditedXmlNodeOrComment item )
        //{
        //    if ( item is EditedXmlNode node )
        //    {
        //        ArcenDebugging.LogSingleLine( $"Node", Verbosity.DoNotShow );
        //        foreach ( KeyValuePair<string, EditedXmlAttribute> att in node.Attributes )
        //        {
        //            ArcenDebugging.LogSingleLine( $"Att key = {att.Key}\t att.value.name = {att.Value.Name}\t att.value.value = {att.Value.ValueOnDisk}", Verbosity.DoNotShow );
        //        }
        //        //foreach ( IEditedXmlNodeOrComment subnode in node.ChildNodes )
        //           // DumpAllVisXml( subnode );
        //    }
        //}
        #endregion
    }
}