using System.Collections.Concurrent;
using System.Diagnostics;
using ArcenXE.Universal;
using ArcenXE.Utilities;
using ArcenXE.Utilities.MessagesToMainThread;
using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Utilities.XmlDataProcessing;

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

        public readonly List<IEditedXmlNodeOrComment> CurrentXmlForVis = new List<IEditedXmlNodeOrComment>(); // merge with XmlPathsVis and make a Dictionary<string, IEditedXmlNodeOrComment> ?

        //useless? public readonly List<string> DataTableNames = new List<string>(); //todo: full path of data table and its name
        public readonly Dictionary<string, IEditedXmlNodeOrComment> TopNodesVis = new Dictionary<string, IEditedXmlNodeOrComment>();

        public IEditedXmlElement? XmlElementCurrentlyBeingEdited { get; } //todo: should be updated with the current node being selected/edited

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

        public int SelectedFolderIndex { get; private set; } = -1;
        public XmlDataTableFile? SelectedFile { get; private set; } = null;
        public int SelectedTopNodeIndex { get; private set; } = -1;

        public MainWindow()
        {
            if ( Instance == null )
                Instance = this;

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
            while ( MessagesToFrontEnd.TryDequeue( out IBGMessageToMainThread? message ) )
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
            ErrorLogToolStripButton.Text = "Error List: " + ErrorsWrittenToLog;
            ProgramPermanentSettings.SetPaths();
        }

        private void LoadMeta_Click( object sender, EventArgs e )
        {
            //MetadataLoader.LoadDataTables( ProgramPermanentSettings.MainPath );
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
            this.SelectedFolderIndex = FolderList.SelectedIndex;
            if ( this.SelectedFolderIndex != -1 && previouslySelectedIndex != this.SelectedFolderIndex ) //todo: introduce lazy loading of all xml files
            {
                if ( TopNodesList.Items.Count > 0 )
                    TopNodesList.Items.Clear();

                List<string> dataTableNames = XmlRootFolders.GetXmlDataTableNames();
                string selectedItem = dataTableNames[this.SelectedFolderIndex];
                MetadataStorage.CurrentVisMetadata = MetadataStorage.AllMetadatas[selectedItem];

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
            }
        }
        #endregion

        #region TopNodes
        private void TopNodesList_SelectedIndexChanged( object sender, EventArgs e )
        {
            this.SelectedTopNodeIndex = TopNodesList.SelectedIndex;
            if ( this.SelectedTopNodeIndex != -1 )
            {
                XmlVisualizer visualizer = new XmlVisualizer();
                visualizer.ReturnAllToPool();
                if ( CurrentXmlForVis.Count > 0 && TopNodesList.Items[0].ToString() != "There are no nodes to display in this file!" )
                {
                    int numberOfMetaDatasStillLoading = MetadataLoader.NumberOfMetaDatasStillLoading;
                    if ( numberOfMetaDatasStillLoading == 0 )
                    {
                        ApplyAttributeTypeToEditedXml( CurrentXmlForVis.ElementAt( TopNodesList.SelectedIndex ) );
                        visualizer.VisualizeSelectedNode( CurrentXmlForVis.ElementAt( TopNodesList.SelectedIndex ) );
                    }
                    else
                        //todo: needs new static class
                        MessageBox.Show( $"There are still {numberOfMetaDatasStillLoading} metadata files being loaded in memory. Try again in moment.", "Metadata still loading", MessageBoxButtons.OK, MessageBoxIcon.Information );
                }
            }
        }

        public void ApplyAttributeTypeToEditedXml( IEditedXmlNodeOrComment element )
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

        public void FillTopNodesList()
        {
            if ( XmlLoader.NumberOfDatasStillLoading == 0 ) // extra safety control - not strictly necessary
            {
                if ( this.TopNodesList.Items.Count > 0 )
                    this.TopNodesList.Items.Clear();
                foreach ( KeyValuePair<string, IEditedXmlNodeOrComment> kv in this.TopNodesVis )
                {
                    if ( kv.Value.IsComment )
                        this.TopNodesList.Items.Add( "Comment: " + ((EditedXmlComment)kv.Value).Data ); // colour comments in green
                    else
                    {
                        EditedXmlAttribute? att = ((EditedXmlNode)kv.Value).NodeCentralID;
                        if ( att != null )
                            this.TopNodesList.Items.Add( att.Value );
                    }
                }
                if ( TopNodesList.Items.Count == 0 )
                {
                    string noNodes = "There are no nodes to display in this file!";
                    this.TopNodesList.Items.Add( noNodes );
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
    }
}