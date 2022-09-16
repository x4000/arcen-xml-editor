namespace ArcenXE.Utilities.CreateDialogs
{
    public class NewFileData
    {
        public string FileName { get; private set; }

        public NewFileData()
        {
            this.FileName = string.Empty;
        }
        public NewFileData( string name )
        {
            this.FileName = name;
        }
    }

    public class NewTopNodeData
    {
        public string NodeName { get; private set; }

        public NewTopNodeData()
        {
            this.NodeName = string.Empty;
        }
        public NewTopNodeData( string name )
        {
            this.NodeName = name;
        }
    }
}
