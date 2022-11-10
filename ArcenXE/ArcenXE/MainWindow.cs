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
using ArcenXE.Utilities.XmlDataSavingToDisk;

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

        public readonly XmlVisualizer Visualizer = new XmlVisualizer(); 
        public readonly VisControls VisControls = new VisControls();

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
            MainWindow.Instance.RightSplitContainer.Panel2.Controls.Add( VisControls );
            VisControls.Dock = DockStyle.Fill;
            VisControls.AutoScroll = true;
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
                ResetTopNodesList();

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
                    this.TopNodesList.ClearSelected();
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
        private readonly List<TopNodeForVis> topNodesForVis_List = new List<TopNodeForVis>();
        private readonly Dictionary<string, TopNodeForVis> topNodesForVis_Dict = new Dictionary<string, TopNodeForVis>();

        private bool ignoreSelectedIndexChanged = false;
        public void FillTopNodesList()
        {
            if ( XmlLoader.NumberOfDatasStillLoading == 0 ) // extra safety control - not strictly necessary
            {
                ResetTopNodesList();
                TopNodeForVis newNodeForVis = new TopNodeForVis( newTopNodeTextForListBox, 0, false );
                this.topNodesForVis_List.Add( newNodeForVis );
                this.topNodesForVis_Dict.Add( newNodeForVis.VisName, newNodeForVis );
                //ArcenDebugging.LogSingleLine( $"CurrentXmlTopNodesForVis.Count = {CurrentXmlTopNodesForVis.Count}", Verbosity.DoNotShow );
                foreach ( KeyValuePair<uint, IEditedXmlNodeOrComment> kv in this.CurrentXmlTopNodesForVis )
                {
                    if ( kv.Value.IsComment ) // colour comments in green
                    {
                        TopNodeForVis commentForVis = new TopNodeForVis( ((EditedXmlComment)kv.Value).Data, kv.Value.UID, true );
                        this.topNodesForVis_List.Add( commentForVis );
                        this.topNodesForVis_Dict.Add( commentForVis.VisName, commentForVis );
                    }
                    else
                    {
                        EditedXmlAttribute? att = ((EditedXmlNode)kv.Value).NodeCentralID;
                        if ( att != null )
                        {
                            string? currentValue = att.GetEffectiveValue();
                            if ( currentValue != null )
                            {
                                TopNodeForVis nodeForVis = new TopNodeForVis( currentValue, kv.Value.UID, false );
                                this.topNodesForVis_List.Add( nodeForVis );
                                this.topNodesForVis_Dict.Add( nodeForVis.VisName, nodeForVis );
                                //ArcenDebugging.LogSingleLine( $"nodeForVis.VisName = {nodeForVis.VisName}", Verbosity.DoNotShow );
                            }
                        }
                    }
                }
                if ( this.topNodesForVis_List.Count == 0 ) //unused -- there will always be the "add node" element or a root node
                {
                    this.TopNodesList.Items.Add( noNodesText );
                    return;
                }
                foreach ( TopNodeForVis node in topNodesForVis_List )
                    TopNodesList.Items.Add( node.VisName );
                RefreshTopNodesList();
                this.TopNodesList.SelectedIndex = -1;
            }
        }

        private void RefreshTopNodesList()
        {
            this.TopNodesList.Items.Clear();
            foreach ( TopNodeForVis node in topNodesForVis_List )
                TopNodesList.Items.Add( node.VisName );
            this.TopNodesList.SelectedIndex = -1;
        }

        private void ResetTopNodesList()
        {
            // reset to initial state
            if ( this.TopNodesList.Items.Count > 0 )
            {
                this.TopNodesList.Items.Clear();
                this.TopNodesList.SelectedIndex = -1;
                this.topNodesForVis_List.Clear();
                this.topNodesForVis_Dict.Clear();
            }
        }

        private void TopNodesList_SelectedIndexChanged( object sender, EventArgs e )
        {
            if ( ignoreSelectedIndexChanged )
                return;
            this.SelectedTopNodeIndex = this.TopNodesList.SelectedIndex;
            if ( this.SelectedTopNodeIndex != -1 )
                if ( this.SelectedTopNodeIndex == 0 )
                {
                    NewTopNodeData newTopNodeData = new NewTopNodeData();
                    CreateTopNodeDialog createTopNodeDialog = new CreateTopNodeDialog( newTopNodeData );
                    Regex allowedChars = new Regex( @"^\w+$", RegexOptions.Compiled );
                    DialogResult dialogResult = createTopNodeDialog.ShowDialog();
                    while ( dialogResult == DialogResult.OK && !allowedChars.IsMatch( newTopNodeData.NodeName ) )
                    {
                        MessageBox.Show( "The node name is invalid! Insert again", "Node name invalid", MessageBoxButtons.OK, MessageBoxIcon.Stop );
                        dialogResult = createTopNodeDialog.ShowDialog();
                    }
                    if ( dialogResult == DialogResult.OK )
                    {
                        TopNodeForVis newNodeForVis = new TopNodeForVis( newTopNodeData.NodeName, UIDSource.GetNext(), newTopNodeData.IsComment );
                        this.topNodesForVis_List.Add( newNodeForVis );
                        this.topNodesForVis_Dict.Add( newNodeForVis.VisName, newNodeForVis );
                        RefreshTopNodesList();
                        IEditedXmlNodeOrComment nodeOrComment = newNodeForVis.IsComment ? new EditedXmlComment() : new EditedXmlNode();
                        nodeOrComment.UID = newNodeForVis.UID;
                        nodeOrComment.OuterXml = string.Empty;
                        if ( nodeOrComment is EditedXmlNode node )
                        {
                            EditedXmlAttribute nodeCentralID = new EditedXmlAttribute()
                            {
                                Name = MetadataStorage.CurrentVisMetadata?.CentralID?.Key ?? string.Empty,
                                TempValue = newNodeForVis.VisName,
                            };
                            node.NodeCentralID = nodeCentralID;
                            node.XmlNodeTagName = MetadataStorage.CurrentVisMetadata?.NodeName ?? string.Empty;
                            node.Attributes.Add( nodeCentralID.Name, nodeCentralID );
                        }
                        //save or discard when leaving node
                        this.CurrentXmlTopNodesForVis.Add( nodeOrComment.UID, nodeOrComment );
                        this.XmlElementCurrentlyBeingEdited = nodeOrComment;
                        this.TopNodesList.SelectedIndex = this.SelectedTopNodeIndex = this.TopNodesList.Items.Count - 1; // this will trigger CallXmlVisualizer() on the new node
                    }
                }
                else
                    this.CallXmlVisualizer();
        }

        /// <summary>
        ///  
        /// </summary>
        /// <param name="element">used for Root Only XML files </param>
        public void CallXmlVisualizer( IEditedXmlNodeOrComment? element = null )
        {
            if ( element == null && this.CurrentXmlTopNodesForVis.Count > 0 && this.TopNodesList.Items[0].ToString() != noNodesText )
            {
                int numberOfMetaDatasStillLoading = MetadataLoader.NumberOfMetaDatasStillLoading;
                if ( numberOfMetaDatasStillLoading == 0 )
                {
                    string sKey = (string)this.TopNodesList.SelectedItem;
                    uint key = this.topNodesForVis_Dict[sKey].UID;
                    if ( this.CurrentXmlTopNodesForVis.TryGetValue( key, out this.XmlElementCurrentlyBeingEdited ) )
                    {
                        //ApplyAttributeTypeToEditedXml( this.XmlElementCurrentlyBeingEdited );
                        Visualizer.OuterVisualizeSelectedNode( this.XmlElementCurrentlyBeingEdited, MetadataStorage.CurrentVisMetadata?.TopLevelNode, forceClearVis: true );
                    }
                }
                else
                    //todo: needs new static class
                    MessageBox.Show( $"There are still {numberOfMetaDatasStillLoading} metadata files being loaded in memory. Try again in moment.", "Metadata still loading", MessageBoxButtons.OK, MessageBoxIcon.Information );
            }
            if ( element != null )
            {
                XmlElementCurrentlyBeingEdited = element;
                Visualizer.OuterVisualizeSelectedNode( element, MetadataStorage.CurrentVisMetadata?.TopLevelNode, forceClearVis: true );
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

        private void SaveToolStripButton_Click( object sender, EventArgs e )
        {
            if ( !SavingToFile.TrySave( this.XmlElementCurrentlyBeingEdited, false ) )
                MessageBox.Show( "Error while saving! Check the log for details.", "Saving error", MessageBoxButtons.OK, MessageBoxIcon.Error );            
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