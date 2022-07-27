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

        public readonly List<IEditedXmlNodeOrComment> CurrentXmlForVis = new List<IEditedXmlNodeOrComment>();
        public readonly XmlVisualizer xmlVisualizer = new XmlVisualizer();

        public readonly List<string> DataTableNames = new List<string>(); //todo: full path of data table and its name
        public readonly List<string> XmlPaths = new List<string>(); //full path to filename

        public readonly Dictionary<string, DataTable> GlobalMetadata = new Dictionary<string, DataTable>();//todo
        public MetadataDocument? metadataDocument;//todo

        public readonly Dictionary<string, EditedXmlNode> TopNodesVis = new Dictionary<string, EditedXmlNode>();

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

        public int SelectedFileIndex { get; set; } = -1;
        public int SelectedTopNodeIndex { get; set; } = -1;

        public List<string> FilesNames { get; } = new List<string>(); //todo

        private readonly string path = Path.GetFullPath( Application.ExecutablePath );//todo

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

        private void MainThreadLoop()
        {
            while ( MessagesToFrontEnd.TryDequeue( out IBGMessageToMainThread? message ) )
            {
                if ( message != null )
                    message.ProcessMessageOnMainThread();
            }
        }

        private void MainWindow_Load( object sender, EventArgs e )
        {
            ErrorLogToolStripButton.Text = "Error List: " + ErrorsWrittenToLog;
        }

        private void TopNodesList_SelectedIndexChanged( object sender, EventArgs e )
        {
            this.SelectedTopNodeIndex = TopNodesList.SelectedIndex;
            XmlVisualizer visualizer = new XmlVisualizer();
            if ( this.SelectedTopNodeIndex != -1 )
                visualizer.ReturnAllToPool();
            visualizer.Visualize( CurrentXmlForVis.ElementAt( TopNodesList.SelectedIndex ) );
        }

        public void FillFileList() //todo colour coding based on origin (basegame (to not display), dlc, mod)
        {
            if ( this.FileList.Items.Count > 0 )
            {
                this.FileList.Items.Clear();
            }

            foreach ( string path in this.XmlPaths )
            {
                this.FileList.Items.Add( Path.GetFileNameWithoutExtension( path ) );
            }
        }

        private void FileList_SelectedIndexChanged( object sender, EventArgs e )
        {
            int currentlySelectedIndex = this.SelectedFileIndex;
            this.SelectedFileIndex = FileList.SelectedIndex;
            //ArcenDebugging.LogSingleLine( "FileList.SelectedIndex: " + FileList.SelectedIndex, Verbosity.DoNotShow );
            if ( currentlySelectedIndex != this.SelectedFileIndex ) //todo: introduce lazy loading of all xml files
            {
                string selectedItem = XmlPaths[this.SelectedFileIndex];
                XmlLoader.LoadXml( selectedItem );
            }
        }

        public void VisualizeTopNodesFromSelectedFile()
        {
            if ( XmlLoader.NumberOfDatasStillLoading == 0 ) // extra safety control - not strictly necessary
            {
                if ( TopNodesList.Items.Count > 0 )
                    TopNodesList.Items.Clear();
                foreach ( KeyValuePair<string, EditedXmlNode> kv in this.TopNodesVis )
                    TopNodesList.Items.Add( kv.Key );
                if ( TopNodesList.Items.Count == 0 )
                {
                    string noNodes = "There are no nodes to display in this file!";
                    TopNodesList.Items.Add( noNodes );
                }
            }
        }

        private void LoadMeta_Click( object sender, EventArgs e )
        {
            MetadataLoader.LoadAllDataTables( @"C:\Users\Daniel\ArcenDev\Arcology5\GameData\Configuration" );
        }

        private void ErrorLogToolStripButton_Click( object sender, EventArgs e )
        {
            ProcessStartInfo debugLogStartInfo = new ProcessStartInfo
            {
                FileName = @"C:\Users\Daniel\ArcenDev\arcen-xml-editor\ArcenXE\ArcenXE\logs\XEDebugLog.txt",
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

        private void FolderToolStripMenuItem_Click( object sender, EventArgs e )
        {
            FolderOpener opener = new FolderOpener();
            opener.OpenFolderWindow();
        }

        private void FileToolStripMenuItem_Click( object sender, EventArgs e )
        {
            FileOpeners.OpenFileDialog();
        }

        private void ExplorerToolStripMenuItem_Click( object sender, EventArgs e )
        {
            Explorer explorer = new Explorer();
            explorer.Show();
        }

        private void Button1_Click( object sender, EventArgs e )
        {
            FileOpeners.OpenFileDialog();
        }
    }

}