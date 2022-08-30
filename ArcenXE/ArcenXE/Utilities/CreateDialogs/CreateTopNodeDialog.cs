using System.Text.RegularExpressions;

namespace ArcenXE.Utilities.CreateDialogs
{
    public partial class CreateTopNodeDialog : CreateDialogBase
    {
        private const string textBoxError = "Special characters (excluding _) are not allowed.";

        public CreateTopNodeDialog( out NewFileData? newFileData ) : base( out _ )
        {
            InitializeComponent();
            newFileData = this.newFileData;
        }

        private void NodeNameTextBox_Leave( object sender, EventArgs e )
        {
            Regex allowedChars = new Regex( @"^\w+$", RegexOptions.Compiled );
            if ( allowedChars.IsMatch( NodeNameTextBox.Text ) )
                this.TextBoxNameErrorProvider.SetError( this, string.Empty );
            else
                this.TextBoxNameErrorProvider.SetError( this, textBoxError );
        }

        protected override void CancelButton_Click( object sender, EventArgs e )
        {
            newFileData = null;
            this.Close();
        }

        protected override void CreateButton_Click( object sender, EventArgs e )
        {
            if ( this.TextBoxNameErrorProvider.GetError( this.NodeNameTextBox ) != string.Empty )
            {
                MessageBox.Show( "The node name is invalid!", "Node name invalid", MessageBoxButtons.OK, MessageBoxIcon.Stop );
                return;
            }
            newFileData = new NewFileData( this.NodeNameTextBox.Text );
            this.Close();
        }
    }
}
