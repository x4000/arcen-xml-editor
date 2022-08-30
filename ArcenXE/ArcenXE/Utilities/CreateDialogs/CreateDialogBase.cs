using System.Windows.Forms;

namespace ArcenXE.Utilities.CreateDialogs
{
    public /*abstract*/ partial class CreateDialogBase : Form
    {
        protected NewFileData? newFileData = null;

        public CreateDialogBase()
        {

        }

        public CreateDialogBase( out NewFileData? newFileData )
        {
            InitializeComponent();
            newFileData = this.newFileData;
        }

        protected virtual void CreateDialogBase_Load( object sender, EventArgs e )
        {
            this.TextBoxNameErrorProvider.SetIconAlignment( this, ErrorIconAlignment.TopLeft );
            CreateButton.DialogResult = DialogResult.OK;
            CancelButton.DialogResult = DialogResult.Cancel;
        }

        protected /*abstract*/ virtual void CancelButton_Click( object sender, EventArgs e )
        {

        }
        protected /*abstract*/ virtual void CreateButton_Click( object sender, EventArgs e )
        {

        }

    }

    public class NewFileData
    {
        public readonly string FileName;

        public NewFileData( string name )
        {
            this.FileName = name;
        }
    }
}
