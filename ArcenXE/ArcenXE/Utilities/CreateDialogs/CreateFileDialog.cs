using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace ArcenXE.Utilities.CreateDialogs
{
    public partial class CreateFileDialog : Form
    {
        private const string textBoxError = "Empty strings or Special characters (excluding _) are not allowed";
        private NewFileData? newFileData;

        public CreateFileDialog( NewFileData newFileData )
        {
            InitializeComponent();
            this.newFileData = newFileData;
        }

        private void CreateFileDialog_Load( object sender, EventArgs e )
        {
            this.FileNameErrorProvider.SetIconAlignment( this, ErrorIconAlignment.TopLeft );
            CreateButton.DialogResult = DialogResult.OK;
            CancelButton.DialogResult = DialogResult.Cancel;
        }

        private void FileNameTextBox_Leave( object sender, EventArgs e )
        {
            Regex allowedChars = new Regex( @"^\w+$", RegexOptions.Compiled );
            if ( allowedChars.IsMatch( this.FileNameTextBox.Text ) )
                this.FileNameErrorProvider.SetError( this.FileNameTextBox, string.Empty );
            else
                this.FileNameErrorProvider.SetError( this.FileNameTextBox, textBoxError );
        }

        private void CancelButton_Click( object sender, EventArgs e )
        {
            this.newFileData = null;
            this.Dispose();
        }

        private void CreateButton_Click( object sender, EventArgs e )
        {
            ArcenDebugging.LogSingleLine( $"Create event", Verbosity.DoNotShow );

            if ( this.newFileData == null )
            {
                ArcenDebugging.LogSingleLine( $"this.newFileData is null", Verbosity.DoNotShow );
                return;
            }
            this.newFileData.FileName = this.FileNameTextBox.Text;
            this.Close();
        }
    }
}
