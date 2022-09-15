namespace ArcenXE.Utilities.CreateDialogs
{
    public class NewFileData
    {
        public string FileName;

        public NewFileData()
        {
            this.FileName = string.Empty;
        }
        public NewFileData( string name )
        {
            this.FileName = name;
        }
    }
}
