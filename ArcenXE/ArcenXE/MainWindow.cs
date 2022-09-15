using System.Text.RegularExpressions;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Numerics;
using ArcenXE.Universal;
using ArcenXE.Utilities;
using ArcenXE.Utilities.MessagesToMainThread;
using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Utilities.XmlDataProcessing;
using ArcenXE.Utilities.CreateDialogs;
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

        #region Folders
        public int SelectedFolderIndex { get; private set; } = -1;
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

        #region Files
        private void FileToolStripMenuItem_Click( object sender, EventArgs e )
        {
            Openers.OpenFileDialog();
        }

        private XmlDataTableFile? SelectedFile { get; set; } = null;
        private const string newFileTextForListBox = "Click to create new file...";
        public void FillFileList( XmlDataTable xmlDataTable ) //todo colour coding based on origin (basegame (to not colour), dlc, mod)
        {
            if ( this.FileList.Items.Count > 0 )
                this.FileList.Items.Clear();

            this.SelectedFile = null;
            this.FileList.Items.Add( newFileTextForListBox );
            foreach ( XmlDataTableFile file in xmlDataTable.Files )
                this.FileList.Items.Add( file );
        }

        private void FileList_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( this.FileList.SelectedItem is string selectedString && selectedString == newFileTextForListBox ) //open file creation form
            {
                NewFileData newFileData = new NewFileData();
                CreateFileDialog createFileDialog = new CreateFileDialog( newFileData );
                Regex allowedChars = new Regex( @"^\w+$", RegexOptions.Compiled );
                DialogResult dialogResult = createFileDialog.ShowDialog();
                while ( dialogResult == DialogResult.OK && !allowedChars.IsMatch( newFileData.FileName ) )
                {
                    MessageBox.Show( "The file name is invalid! Insert again", "File name invalid", MessageBoxButtons.OK, MessageBoxIcon.Stop );
                    dialogResult = createFileDialog.ShowDialog();
                }
                if ( dialogResult == DialogResult.OK )
                {
                    FileStream fs = File.Create( ProgramPermanentSettings.MainPath + @"\" + (string)this.FolderList.SelectedItem + @"\" + newFileData.FileName + ".xml" );
                    MessageBox.Show( "File created at path: " + fs.Name, "File created", MessageBoxButtons.OK, MessageBoxIcon.Information );
                }

            }
            else
            {
                XmlDataTableFile? previouslySelectedFile = this.SelectedFile;
                this.SelectedFile = (XmlDataTableFile)this.FileList.SelectedItem;
                //ArcenDebugging.LogSingleLine( "FileList.SelectedIndex: " + FileList.SelectedIndex, Verbosity.DoNotShow );
                if ( this.SelectedFile != null && previouslySelectedFile != this.SelectedFile )
                {
#pragma warning disable CS8604
                    XmlLoader.LoadXml( this.SelectedFile.FullFilePath, MetadataStorage.CurrentVisMetadata );
#pragma warning restore CS8604
                    #region Dump
                    //foreach ( IEditedXmlNodeOrComment item in this.CurrentXmlForVis )
                    //{
                    //    DumpAllVisXml( item );
                    //}
                    #endregion
                }
            }
        }
        #endregion

        #region TopNodes
        public int SelectedTopNodeIndex { get; set; } = -1;
        private const string newTopNodeTextForListBox = "Click to create new element..."; // make a class and attach modal form to it?
        private const string noNodesText = "There are no nodes to display in this file!";
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
                topNodesForVis.Add( new TopNodeForVis( newTopNodeTextForListBox, 0, false ) );
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
                    this.TopNodesList.Items.Add( noNodesText );
                    return;
                }
                TopNodesList.DataSource = topNodesForVis; // possibility of data islands on the old DataSource?
                TopNodesList.DisplayMember = "VisName";
                TopNodesList.ValueMember = "UID";
            }
        }

        private void TopNodesList_SelectedIndexChanged( object sender, EventArgs e )
        {
            this.SelectedTopNodeIndex = this.TopNodesList.SelectedIndex;
            if ( this.SelectedTopNodeIndex != -1 )
                if ( this.SelectedTopNodeIndex == 0 )
                {
                    //call modal create node

                }
                else
                    CallXmlVisualizer();
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="element">used for Root Only XML files </param>
        public void CallXmlVisualizer( IEditedXmlNodeOrComment? element = null )
        {
            XmlVisualizer visualizer = new XmlVisualizer();
            if ( element == null && this.CurrentXmlTopNodesForVis.Count > 0 && this.TopNodesList.Items[0].ToString() != noNodesText )
            {
                int numberOfMetaDatasStillLoading = MetadataLoader.NumberOfMetaDatasStillLoading;
                if ( numberOfMetaDatasStillLoading == 0 )
                {
                    uint key = ((TopNodeForVis)this.TopNodesList.SelectedItem).UID;
                    if ( this.CurrentXmlTopNodesForVis.TryGetValue( key, out this.XmlElementCurrentlyBeingEdited ) )
                    {
                        //ApplyAttributeTypeToEditedXml( this.XmlElementCurrentlyBeingEdited );
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

        //private static void ApplyAttributeTypeToEditedXml( IEditedXmlNodeOrComment element )
        //{
        //    if ( element.IsComment )
        //        return;
        //    else if ( MetadataStorage.CurrentVisMetadata != null && MetadataStorage.CurrentVisMetadata.TopLevelNode != null )
        //    {
        //        foreach ( KeyValuePair<string, EditedXmlAttribute> dataAttribute in ((EditedXmlNode)element).Attributes )
        //            if ( MetadataStorage.CurrentVisMetadata.TopLevelNode.AttributesData.TryGetValue( dataAttribute.Value.Name, out MetaAttribute_Base? metadata ) && metadata != null )
        //                dataAttribute.Value.Type = metadata.Type;
        //    }
        //}
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

        #region Saving
        private void SaveToolStripButton_Click( object sender, EventArgs e )
        {
            //ArcenDebugging.LogSingleLine( $"Step 1", Verbosity.DoNotShow );
            if ( this.XmlElementCurrentlyBeingEdited == null )
            {
                ArcenDebugging.LogSingleLine( $"XmlElementCurrentlyBeingEdited is null. Can't save this xml to file!", Verbosity.DoNotShow );
                return;
            }

            XmlWriter xmlOutput = new XmlWriter();
            if ( this.XmlElementCurrentlyBeingEdited.IsComment )
            {
                //todo
            }
            else
            {
                //ArcenDebugging.LogSingleLine( $"Step 2", Verbosity.DoNotShow );
                bool justInsertedLineBreak = false;
                EditedXmlNode node = (EditedXmlNode)XmlElementCurrentlyBeingEdited;
                if ( node.IsRootOnly )
                {
                    // loop node's attributes
                    //ArcenDebugging.LogSingleLine( $"Step 3.a", Verbosity.DoNotShow );
                    xmlOutput.StartOpenNode( "root", false ).NewLine( XmlLeadingWhitespace.None );
                    foreach ( KeyValuePair<string, EditedXmlAttribute> att in node.Attributes )
                        ReadAttributeFromEditedData( xmlOutput, att, ref justInsertedLineBreak, false );
                    xmlOutput.FinishOpenNode( true );
                    xmlOutput.CloseNode( "root", false );
                }
                else
                {
                    // loop top node's attributes, then do the same for all the subnodes
                    //ArcenDebugging.LogSingleLine( $"Step 3.b", Verbosity.DoNotShow );
                    if ( node.RelatedUnionNode == null )
                    {
                        ArcenDebugging.LogSingleLine( $"RelatedUnionNode inside EditedXmlNode {node.NodeCentralID?.GetEffectiveValue() ?? node.XmlNodeTagName} is null. Can't save this node to file!", Verbosity.DoNotShow );
                        return;
                    }
                    xmlOutput.StartOpenNode( "root", true ).NewLine( XmlLeadingWhitespace.None );
                    xmlOutput.StartOpenNode( node.RelatedUnionNode.MetaDocument.NodeName, false );
                    foreach ( KeyValuePair<string, EditedXmlAttribute> att in node.Attributes )
                    {
                        if ( node.ChildNodes.Count > 0 && att.Key == "name" ) //subnode container, it doesn't need a newline after the only attribute it prints
                            ReadAttributeFromEditedData( xmlOutput, att, ref justInsertedLineBreak, true );
                        else
                            ReadAttributeFromEditedData( xmlOutput, att, ref justInsertedLineBreak, false );
                    }
                    xmlOutput.FinishOpenNode( true );
                    foreach ( EditedXmlNode subNode in node.ChildNodes.Cast<EditedXmlNode>() )
                    {
                        xmlOutput.StartOpenNode( subNode.XmlNodeTagName, false );
                        foreach ( KeyValuePair<string, EditedXmlAttribute> sAtt in subNode.Attributes )
                        {
                            //ArcenDebugging.LogSingleLine( $"sAtt.Key = {sAtt.Key}!", Verbosity.DoNotShow );
                            ReadAttributeFromEditedData( xmlOutput, sAtt, ref justInsertedLineBreak, true );
                        }
                        xmlOutput.FinishOpenNodeAndSelfClose();
                    }
                    xmlOutput.CloseNode( node.RelatedUnionNode.MetaDocument.NodeName, true );
                    xmlOutput.CloseNode( "root", false );
                }

                //write to file
                if ( node.RelatedUnionNode == null )
                {
                    ArcenDebugging.LogSingleLine( $"RelatedUnionNode inside Edited Node {node} is null. Can't save this node to file!", Verbosity.DoNotShow );
                    return;
                }
                //add file name field in vis
                File.WriteAllText( ProgramPermanentSettings.MainPath + @"\" + node.RelatedUnionNode.MetaDocument.MetadataFolder + @"\" +
                                   node.RelatedUnionNode.MetaDocument.MetadataFolder + ".xml", xmlOutput.GetFinishedXmlDocument() );
                //ArcenDebugging.LogSingleLine( $"Step 5", Verbosity.DoNotShow );
                //ArcenDebugging.LogSingleLine( $"{node.RelatedUnionNode.MetaDocument.MetadataFolder}", Verbosity.DoNotShow );
            }

        }

        private bool ReadAttributeFromEditedData( XmlWriter xmlOutput, KeyValuePair<string, EditedXmlAttribute> att, ref bool justInsertedLineBreak, bool skipAttributeLeadOut = false )
        {
            //ArcenDebugging.LogSingleLine( $"Step 3.b.1", Verbosity.DoNotShow );
            MetaAttribute_Base? metaAttribute = att.Value.RelatedUnionAttribute?.MetaAttribute.Value;
            if ( metaAttribute == null )
            {
                ArcenDebugging.LogSingleLine( $"RelatedUnionAttribute inside Edited Attribute {att.Key} is null. Can't save this attribute to file!", Verbosity.DoNotShow );
                return false;
            }
            //ArcenDebugging.LogSingleLine( $"Before calc Att = {att.Key} \t\tjustInsertedLineBreak = {justInsertedLineBreak}", Verbosity.DoNotShow );
            string? effectiveValue = att.Value.GetEffectiveValue();
            if ( effectiveValue == null )
            {
                ArcenDebugging.LogSingleLine( $"GetEffectiveValue() inside Edited Attribute {att.Key} returned null. Can't save this attribute to file!", Verbosity.DoNotShow );
                return false;
            }
            //ArcenDebugging.LogSingleLine( $"att.Key = {att.Key}\t\teffectiveValue = {effectiveValue}", Verbosity.DoNotShow );
            //ArcenDebugging.LogSingleLine( $"Step 3.b.2", Verbosity.DoNotShow );
            XmlAttLeadInOut leadIn = CalculateLineBreakBefore( metaAttribute, ref justInsertedLineBreak, xmlOutput );
            //ArcenDebugging.LogSingleLine( $"After calc Att = {att.Key} \t\tjustInsertedLineBreak = {justInsertedLineBreak}", Verbosity.DoNotShow );
            switch ( att.Value.Type )
            {
                #region All Cases
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
                default:
                    ArcenDebugging.LogSingleLine( $"Unknown attribute type {att.Value.Type} during saving to xml", Verbosity.DoNotShow );
                    break;
                    #endregion
            }
            //ArcenDebugging.LogSingleLine( $"Step 4", Verbosity.DoNotShow );
            if ( leadIn == XmlAttLeadInOut.Linebreak )
                xmlOutput.IncrementPixelsOnCurrentLine( (ushort)metaAttribute.ContentWidthPx );
            if ( !skipAttributeLeadOut )
            {
                XmlAttLeadInOut leadInOut = CalculateLineBreakAfter( metaAttribute, ref justInsertedLineBreak );
                if ( leadInOut == XmlAttLeadInOut.Linebreak )
                    xmlOutput.HandleAttributeLeadInOut( leadInOut );
            }
            //ArcenDebugging.LogSingleLine( $"End calc Att = {att.Key} \t\tjustInsertedLineBreak = {justInsertedLineBreak}", Verbosity.DoNotShow );
            return true;
        }

        private static XmlAttLeadInOut CalculateLineBreakBefore( MetaAttribute_Base metaAttribute, ref bool justInsertedLineBreak, XmlWriter xmlWriter )
        {
            bool requireLineBreak = xmlWriter.CalculateIfNewLineIsRequired( (ushort)metaAttribute.ContentWidthPx );

            switch ( metaAttribute.LinebreakBefore )
            {
                case LineBreakType.Always:
                    if ( !justInsertedLineBreak )
                    {
                        justInsertedLineBreak = true;
                        return XmlAttLeadInOut.Linebreak;
                    }
                    else
                    {
                        //skipLeadInSpace = true;
                        justInsertedLineBreak = false;
                        return XmlAttLeadInOut.Space;
                    }
                case LineBreakType.PreferNot:
                    if ( requireLineBreak )
                    {
                        justInsertedLineBreak = true;
                        return XmlAttLeadInOut.Linebreak;
                    }
                    if ( justInsertedLineBreak )
                    {
                        justInsertedLineBreak = false;
                        return XmlAttLeadInOut.None;
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
        #endregion

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