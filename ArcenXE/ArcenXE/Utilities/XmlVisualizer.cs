using System.Collections.Concurrent;

namespace ArcenXE.Utilities
{
    public class XmlVisualizer
    {
        //private readonly List<IEditedXmlNodeOrComment> currentXmlForVis = new List<IEditedXmlNodeOrComment>();
        //public void GetXmlForVis() => this.currentXmlForVis.AddRange( MainWindow.Instance.CurrentXmlForVis );

        //public XmlVisualizer() => this.currentXmlForVis.AddRange( MainWindow.Instance.CurrentXmlForVis );

        private readonly SuperBasicPool<Label> labelPool = new SuperBasicPool<Label>();
        private readonly ConcurrentQueue<Label> labelQueue = new ConcurrentQueue<Label>();

        private readonly SuperBasicPool<TextBox> textBoxPool = new SuperBasicPool<TextBox>();
        private readonly ConcurrentQueue<TextBox> textBoxQueue = new ConcurrentQueue<TextBox>();

        private static class Caret
        {
            public static int x = 0, y = 0;
            public static void MoveHorz( int amount )
            {
                x += amount;
            }
            public static void NextLine( int amount )
            {
                y += amount;
            }
        }

        public void Visualize( IEditedXmlNodeOrComment editedXmlNodeOrComment )
        {
            IEditedXmlNodeOrComment item = editedXmlNodeOrComment;
            Caret.x = 0; // new item, reset caret to start line
            Graphics graphics = MainWindow.Instance.VisPanel.CreateGraphics();

            using ( graphics )
            {
                if ( item is EditedXmlComment comment )
                {
                    Label label = labelPool.GetOrAdd();
                    SizeF size;
                    size = graphics.MeasureString( comment.Data, MainWindow.Instance.VisPanel.Font );
                    label.Height = (int)Math.Ceiling( size.Height );
                    label.Width = (int)Math.Ceiling( size.Width );
                    label.Bounds = new Rectangle( Caret.x, Caret.y, label.Width + 5, label.Height );
                    label.Text = comment.Data;
                    MainWindow.Instance.VisPanel.Controls.Add( label );
                    labelQueue.Enqueue( label );

                    Caret.NextLine( label.Height );
                }
                else
                {
                    if ( item is EditedXmlNode node )
                    {
                        if ( node.NodeName != null ) // top node
                        {
                            Label label = labelPool.GetOrAdd();
                            SizeF size;
                            size = graphics.MeasureString( node.NodeName.Value, MainWindow.Instance.VisPanel.Font );
                            label.Height = (int)Math.Ceiling( size.Height );
                            label.Width = (int)Math.Ceiling( size.Width );
                            label.Bounds = new Rectangle( Caret.x, Caret.y, label.Width + 5, label.Height );
                            label.Text = node.NodeName.Value;
                            MainWindow.Instance.VisPanel.Controls.Add( label );
                            labelQueue.Enqueue( label );

                            Caret.MoveHorz( label.Width + 2 );
                        }

                        foreach ( EditedXmlAttribute att in node.Attributes )
                        {
                            Label label = labelPool.GetOrAdd();
                            Label labelV = labelPool.GetOrAdd();
                            SizeF size;

                            size = graphics.MeasureString( att.Name, MainWindow.Instance.VisPanel.Font );
                            label.Height = (int)Math.Ceiling( size.Height );
                            label.Width = (int)Math.Ceiling( size.Width );
                            label.Bounds = new Rectangle( Caret.x, Caret.y, label.Width + 5, label.Height );
                            label.Text = att.Name;
                            MainWindow.Instance.VisPanel.Controls.Add( label );
                            labelQueue.Enqueue( label );
                            Caret.MoveHorz( label.Width + 2 );

                            size = graphics.MeasureString( att.Value, MainWindow.Instance.VisPanel.Font );
                            labelV.Height = (int)Math.Ceiling( size.Height );
                            labelV.Width = (int)Math.Ceiling( size.Width );
                            labelV.Bounds = new Rectangle( Caret.x, Caret.y, labelV.Width + 5, labelV.Height );
                            labelV.Text = att.Value;
                            MainWindow.Instance.VisPanel.Controls.Add( labelV );
                            labelQueue.Enqueue( labelV );

                            Caret.NextLine( labelV.Height );
                            Caret.MoveHorz( -(label.Width + 2) );

                        }

                        foreach ( IEditedXmlNodeOrComment child in node.ChildNodes )
                            this.Visualize( child );
                    }
                }
            }
        }
    }
}