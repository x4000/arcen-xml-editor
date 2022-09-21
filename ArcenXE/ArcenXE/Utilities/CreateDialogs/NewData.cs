namespace ArcenXE.Utilities.CreateDialogs
{
    public class NewFileData
    {
        public string FileName { get; set; }

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
        public string NodeName { get; set; }
        public bool IsComment { get; set; }

        public NewTopNodeData()
        {
            this.NodeName = string.Empty;
            this.IsComment = false;
        }
        public NewTopNodeData( string name, bool isComment )
        {
            this.NodeName = name;
            this.IsComment = isComment;
        }
    }
}
