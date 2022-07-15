namespace ArcenXE.Utilities.MessagesToMainThread
{
    public class SendEditedXmlTopNodeToList : IBGMessageToMainThread
    {
        public List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();

        public void ProcessMessageOnMainThread()
        {
            foreach ( EditedXmlNode node in Nodes )
            {
                if ( node.NodeName != null )
                    MainWindow.Instance.TopNodesList.Items.Add( node.NodeName.Value );
            }
        }    
    }

    public class SaveEditedXmlToList : IBGMessageToMainThread
    {
        public List<IEditedXmlNodeOrComment> Nodes = new List<IEditedXmlNodeOrComment>();
        public void ProcessMessageOnMainThread() => MainWindow.Instance.CurrentXmlForVis.AddRange( this.Nodes );
    }
}
