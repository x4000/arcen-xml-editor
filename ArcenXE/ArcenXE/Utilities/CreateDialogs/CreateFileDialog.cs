using System.Text.RegularExpressions;

namespace ArcenXE.Utilities.CreateDialogs
{
    public partial class CreateFileDialog : CreateDialogBase
    {
        private const string textBoxError = "Special characters (excluding _) are not allowed.";

        public CreateFileDialog( out NewFileData? newFileData ) : base( out _ )
        {
            InitializeComponent();
            newFileData = this.newFileData;
        }

        private void FileNameTextBox_Leave( object sender, EventArgs e )
        {
            Regex allowedChars = new Regex( @"^\w+$", RegexOptions.Compiled );
            if ( allowedChars.IsMatch( FileNameTextBox.Text ) )
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
            if ( this.TextBoxNameErrorProvider.GetError( this.FileNameTextBox ) != string.Empty )
            {
                MessageBox.Show( "The file name is invalid!", "File name invalid", MessageBoxButtons.OK, MessageBoxIcon.Stop );
                return;
            }
            newFileData = new NewFileData( this.FileNameTextBox.Text );
            this.Close();
        }
    }
}