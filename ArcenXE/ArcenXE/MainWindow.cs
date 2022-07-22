using System.Collections.Concurrent;
using ArcenXE.Utilities;
using ArcenXE.Utilities.MessagesToMainThread;

namespace ArcenXE
{
    public sealed partial class MainWindow : Form
    {
#pragma warning disable CA2211
#pragma warning disable CS8618
        public static MainWindow Instance;
#pragma warning restore CS8618
#pragma warning restore CA2211

        public readonly SuperBasicPool<Panel> PanelPool = new SuperBasicPool<Panel>();

        public readonly ConcurrentQueue<IBGMessageToMainThread> MessagesToFrontEnd = new ConcurrentQueue<IBGMessageToMainThread>();

        public readonly List<IEditedXmlNodeOrComment> CurrentXmlForVis = new List<IEditedXmlNodeOrComment>();
        public readonly XmlVisualizer xmlVisualizer = new XmlVisualizer();

        private int selectedTopNodeIndex = -1;
        public int SelectedTopNodeIndex
        {
            get => this.selectedTopNodeIndex;
            private set
            {
                if ( value < 0 )
                    throw new ArgumentOutOfRangeException( "selectedTopNodeIndex", "must be greater or equal to 0" );
                else
                    this.selectedTopNodeIndex = value;
            }
        }
        public List<string> FilesNames { get; } = new List<string>();


        private readonly string path = Path.GetFullPath( Application.ExecutablePath );

        public MainWindow()
        {
            if ( Instance == null )
                Instance = this;

            InitializeComponent();
            this.cboGameAndMods.Items.Add( "Base Game" );
            this.cboGameAndMods.Items.Add( "DLC 1" );
            this.cboGameAndMods.Items.Add( "DLC 2" );
            this.cboGameAndMods.Items.Add( "DLC 3" );
            this.cboGameAndMods.Items.Add( "Mod A" );
            this.cboGameAndMods.Items.Add( "Mod B" );
            this.cboGameAndMods.Items.Add( "Mod C" );
            this.cboGameAndMods.Items.Add( "Mod D" );
            this.cboGameAndMods.SelectedIndex = 0;
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

        }

        private void FolderToolStripMenuItem_Click( object sender, EventArgs e )
        {
            FolderOpener opener = new FolderOpener();
            opener.OpenFolderWindow();
        }

        private void FileToolStripMenuItem_Click( object sender, EventArgs e )
        {
            FileOpener opener = new FileOpener();
            opener.OpenFileWindow();
        }

        private void ExplorerToolStripMenuItem_Click( object sender, EventArgs e )
        {
            Explorer explorer = new Explorer();
            explorer.Show();
        }

        private void Button1_Click( object sender, EventArgs e )
        {
            FileOpener opener = new FileOpener();
            opener.OpenFileWindow();
        }

        private void TopNodesList_SelectedIndexChanged( object sender, EventArgs e )
        {
            this.SelectedTopNodeIndex = TopNodesList.SelectedIndex;
            XmlVisualizer visualizer = new XmlVisualizer();
            if ( this.SelectedTopNodeIndex != -1 )
                visualizer.ReturnAllToPool();
            visualizer.Visualize( CurrentXmlForVis.ElementAt( TopNodesList.SelectedIndex ) );
        }
    }

}