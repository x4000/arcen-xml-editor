using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ArcenXE.Utilities;

namespace ArcenXE
{
    public partial class LabelMenu : Form
    {
        private bool mouseDown;
        private Point lastLocation;
        private readonly Control callerControl;

        public LabelMenu( Control callerControl )
        {
            InitializeComponent();
            this.callerControl = callerControl;
            this.Size = new Size( 200, 34 );
        }

        private void LabelMenu_Load( object sender, EventArgs e )
        {
            ToolStripPanel stripPanel = new ToolStripPanel();
            ToolStrip toolStrip = new ToolStrip();
            stripPanel.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            stripPanel.Dock = DockStyle.Fill;
            stripPanel.Margin = new Padding( 0 );
            stripPanel.Size = new Size( this.Width - 2, this.Height - 2 );
            stripPanel.Cursor = Cursors.SizeAll;
            stripPanel.Controls.Add( toolStrip );
            this.Controls.Add( stripPanel );

            toolStrip.Dock = DockStyle.Fill;
            toolStrip.Size = new Size( this.Width - 3, this.Height - 2 );
            toolStrip.ImageScalingSize = new Size( 24, 24 );
            toolStrip.Stretch = true;
            toolStrip.GripStyle = ToolStripGripStyle.Hidden;
            toolStrip.MouseDown += new MouseEventHandler( ToolStrip_MouseDown );
            toolStrip.MouseMove += new MouseEventHandler( ToolStrip_MouseMove );
            toolStrip.MouseUp += new MouseEventHandler( ToolStrip_MouseUp );

            ToolStripButton closeLabelMenu = new ToolStripButton
            {
                Alignment = ToolStripItemAlignment.Right,
                Image = Image.FromFile( ProgramPermanentSettings.AssetsPath + @"Icons\tabler-icons\X24.png" )
            };
            closeLabelMenu.Click += new EventHandler( this.CloseMenu_Click );
            toolStrip.Items.Add( closeLabelMenu );

            ToolStripButton deleteAttribute = new ToolStripButton
            {
                Alignment = ToolStripItemAlignment.Left,
                Image = Image.FromFile( ProgramPermanentSettings.AssetsPath + @"Icons\tabler-icons\Trash\trashX24.png" )
            };
            closeLabelMenu.Click += new EventHandler( this.DeleteAttribute_Click );
            toolStrip.Items.Add( deleteAttribute );

        }

        private void DeleteAttribute_Click( object? sender, EventArgs e )
        {
            //todo

        }

        private void CloseMenu_Click( object? sender, EventArgs e )
        {
            this.Close();
        }

        private void ToolStrip_MouseDown( object? sender, MouseEventArgs e )
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void ToolStrip_MouseMove( object? sender, MouseEventArgs e )
        {
            if ( mouseDown )
            {
                this.Location = new Point( this.Location.X - this.lastLocation.X + e.X, this.Location.Y - this.lastLocation.Y + e.Y );
                this.Update();
            }
        }

        private void ToolStrip_MouseUp( object? sender, MouseEventArgs e ) => mouseDown = false;
    }
}
