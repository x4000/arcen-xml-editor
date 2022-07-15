using System.Collections.Concurrent;

namespace ArcenXE.Utilities
{
    public class XmlVisualizer
    {
        //private readonly List<IEditedXmlNodeOrComment> currentXmlForVis = new List<IEditedXmlNodeOrComment>();
        //public void GetXmlForVis() => this.currentXmlForVis.AddRange( MainWindow.Instance.CurrentXmlForVis );

        //public XmlVisualizer() => this.currentXmlForVis.AddRange( MainWindow.Instance.CurrentXmlForVis );

        private readonly SuperBasicPool<Label> labelPool = new SuperBasicPool<Label>();
        private readonly SuperBasicPool<TextBox> textBoxPool = new SuperBasicPool<TextBox>();

        public readonly Dictionary<Control, IEditedXmlElement> EditedXmlElementsByControl = new Dictionary<Control, IEditedXmlElement>();

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

        #region ReturnAllToPool
        public void ReturnAllToPool()
        {
            Control.ControlCollection controls = MainWindow.Instance.VisPanel.Controls;

            foreach ( Control control in controls )
            {
                if ( control is Label )
                {
                    Label lbl = (Label)control;
                    labelPool.ReturnToPool( lbl );
                }
                else if ( control is TextBox )
                {
                    TextBox txt = (TextBox)control;
                    textBoxPool.ReturnToPool( txt );
                }
            }

            controls.Clear();
            EditedXmlElementsByControl.Clear();
        }
        #endregion

        public void Visualize( IEditedXmlNodeOrComment editedXmlNodeOrComment )
        {
            IEditedXmlNodeOrComment item = editedXmlNodeOrComment;
            Caret.x = 0; // new item, reset caret to start line
            Graphics graphics = MainWindow.Instance.VisPanel.CreateGraphics();
            Control.ControlCollection controls = MainWindow.Instance.VisPanel.Controls;

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
                    comment.CurrentViewControl = label;
                    EditedXmlElementsByControl[label] = comment;
                    controls.Add( label );

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
                            node.CurrentViewControl = label;
                            EditedXmlElementsByControl[label] = node;
                            controls.Add( label );

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
                            att.CurrentViewControl_Label = label;
                            controls.Add( label );
                            Caret.MoveHorz( label.Width + 2 );

                            size = graphics.MeasureString( att.Value, MainWindow.Instance.VisPanel.Font );
                            labelV.Height = (int)Math.Ceiling( size.Height );
                            labelV.Width = (int)Math.Ceiling( size.Width );
                            labelV.Bounds = new Rectangle( Caret.x, Caret.y, labelV.Width + 5, labelV.Height );
                            labelV.Text = att.Value;
                            node.CurrentViewControl = label;
                            att.CurrentViewControl_Label = labelV;
                            EditedXmlElementsByControl[labelV] = att;
                            controls.Add( labelV );

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