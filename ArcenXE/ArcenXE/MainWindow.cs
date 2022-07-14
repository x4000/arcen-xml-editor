using System.Collections.Concurrent;
using ArcenXE.Utilities;
using System.Xml;
using ArcenXE.Utilities.MessagesToMainThread;

namespace ArcenXE
{
    public sealed partial class MainWindow : Form
    {
        public static MainWindow? Instance = null;

        public readonly SuperBasicPool<Panel> PanelPool = new SuperBasicPool<Panel>();

        public readonly ConcurrentQueue<IBGMessageToMainThread> MessagesToFrontEnd = new ConcurrentQueue<IBGMessageToMainThread>();

        public readonly List<IEditedXmlNodeOrComment> CurrentXmlForVis = new List<IEditedXmlNodeOrComment>();
        public readonly XmlVisualizer xmlVisualizer = new XmlVisualizer();

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
            this.MainThreadLoop();
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
    }

}