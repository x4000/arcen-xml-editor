using System.Text.RegularExpressions;

namespace ArcenXE.Utilities.CreateDialogs
{
    public partial class CreateTopNodeDialog : Form
    {
        private const string textBoxError = "Empry strings and special characters (excluding _) are not allowed.";
        private NewTopNodeData? newTopNodeData;

        public CreateTopNodeDialog( NewTopNodeData newTopNodeData )
        {
            InitializeComponent();
            this.newTopNodeData = newTopNodeData;
        }

        private void CreateTopNodeDialog_Load( object sender, EventArgs e )
        {
            this.NodeNameErrorProvider.SetIconAlignment( this, ErrorIconAlignment.TopLeft );
            CreateButton.DialogResult = DialogResult.OK;
            CancelButton.DialogResult = DialogResult.Cancel;
        }

        private void NodeNameTextBox_Leave( object sender, EventArgs e )
        {
            Regex allowedChars = new Regex( @"^\w+$", RegexOptions.Compiled );
            if ( allowedChars.IsMatch( NodeNameTextBox.Text ) )
                this.NodeNameErrorProvider.SetError( this, string.Empty );
            else
                this.NodeNameErrorProvider.SetError( this, textBoxError );
        }

        protected void CancelButton_Click( object sender, EventArgs e )
        {
            newTopNodeData = null;
            this.Close();
        }

        protected void CreateButton_Click( object sender, EventArgs e )
        {
            if ( this.NodeNameErrorProvider.GetError( this.NodeNameTextBox ) != string.Empty )
            {
                MessageBox.Show( "The node name is invalid!", "Node name invalid", MessageBoxButtons.OK, MessageBoxIcon.Stop );
                return;
            }
            if ( this.newTopNodeData == null )
            {
                ArcenDebugging.LogSingleLine( "newTopNodeData is null, can't proceed in the creation ofthe node!", Verbosity.DoNotShow );
                return;
            }
            this.newTopNodeData.NodeName = this.NodeNameTextBox.Text;
            this.newTopNodeData.IsComment = this.IsCommentCheckBox.Checked;
            this.Close();
        }
    }
}
