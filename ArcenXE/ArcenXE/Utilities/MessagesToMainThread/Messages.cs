namespace ArcenXE.Utilities.MessagesToMainThread
{
    public class WriteAStringToListBox : IBGMessageToMainThread
    {
        public string DataString = string.Empty;
        public string Types = string.Empty;

        public void ProcessMessageOnMainThread()
        {
            if ( this.DataString != null && this.DataString.Length > 0 )
            {
                if ( MainWindow.Instance != null )
                {
                    MainWindow.Instance.lstItems.Items.Add( this.DataString );
                    MainWindow.Instance.lstItems.Items.Add( this.Types );
                }
            }
        }
    }

    public class SendEditedXmlTopNodeToList : IBGMessageToMainThread
    {
        public List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();

        public void ProcessMessageOnMainThread()
        {
            foreach ( EditedXmlNode node in Nodes )
            {
                if ( node.NodeName != null )
                    MainWindow.Instance.lstItems.Items.Add( node.NodeName.Value );
                MainWindow.Instance.xmlVisualizer.Visualize( node );
            }

        }
    }
}
