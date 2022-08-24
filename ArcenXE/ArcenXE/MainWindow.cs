using System.Collections.Concurrent;
using System.Diagnostics;
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