using ArcenXE.Universal;
using System.Collections.Concurrent;

namespace ArcenXE.Utilities.XmlDataProcessing
{
    public class XmlVisualizer
    {
        private readonly SuperBasicPool<Label> labelPool = new SuperBasicPool<Label>();
        private readonly SuperBasicPool<TextBox> textBoxPool = new SuperBasicPool<TextBox>();

        public readonly Dictionary<Control, IEditedXmlElement> EditedXmlElementsByControl = new Dictionary<Control, IEditedXmlElement>();

        #region Caret
        private static class Caret
        {
            public static int x = 0, y = 0;
            public static void MoveHorz( int amount ) => x += amount;
            public static void NextLine( int amount ) => y += amount;
            public static void Reset()
            {
                x = 0;
                y = 0;
            }
        }
        #endregion

        #region ReturnAllToPool
        public void ReturnAllToPool()
        {
            Control.ControlCollection controls = MainWindow.Instance.VisPanel.Controls;

            foreach ( Control control in controls )
                if ( control is Label label )
                {
                    label.Font = MainWindow.Instance.VisPanel.Font; //set to default; to be moved in separate SetToDefaults() method
                    labelPool.ReturnToPool( label );
                }
                else if ( control is TextBox textBox )
                    textBoxPool.ReturnToPool( textBox );

            Caret.Reset();
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
                if ( item is EditedXmlComment comment )
                {
                    TextBox textBox = this.textBoxPool.GetOrAdd();
                    SizeF size = graphics.MeasureString( comment.Data, MainWindow.Instance.VisPanel.Font );

                    textBox.Height = (int)Math.Ceiling( size.Height );
                    textBox.Width = (int)Math.Ceiling( size.Width );
                    textBox.Bounds = new Rectangle( Caret.x, Caret.y, textBox.Width + 5, textBox.Height );
                    textBox.Text = comment.Data;

                    comment.CurrentViewControl = textBox;
                    this.EditedXmlElementsByControl[textBox] = comment;

                    controls.Add( textBox );

                    Caret.NextLine( textBox.Height + 2 );
                }
                else
                {
                    if ( item is EditedXmlNode node )
                    {
                        if ( node.NodeName != null ) // top node
                        {
                            Label label = this.labelPool.GetOrAdd();
                            string toWrite = "Top Node Selected: " + node.NodeName.Value;

                            label.Font = new Font( label.Font, FontStyle.Bold );
                            SizeF size = graphics.MeasureString( toWrite, label.Font );

                            label.Height = (int)Math.Ceiling( size.Height );
                            label.Width = (int)Math.Ceiling( size.Width );
                            label.Bounds = new Rectangle( Caret.x, Caret.y, label.Width + 5, label.Height );
                            label.Text = toWrite;


                            node.CurrentViewControl = label;
                            this.EditedXmlElementsByControl[label] = node;

                            controls.Add( label );

                            Caret.NextLine( label.Height + 2 );
                        }

                        foreach ( KeyValuePair<string, EditedXmlAttribute> pair in node.Attributes )
                        {
                            Label label = this.labelPool.GetOrAdd();
                            Label labelV = this.labelPool.GetOrAdd();
                            SizeF size = graphics.MeasureString( pair.Key, MainWindow.Instance.VisPanel.Font );

                            label.Height = (int)Math.Ceiling( size.Height );
                            label.Width = (int)Math.Ceiling( size.Width );
                            label.Bounds = new Rectangle( Caret.x, Caret.y, label.Width + 5, label.Height );
                            label.Text = pair.Key;

                            pair.Value.CurrentViewControl_Name = label;
                            controls.Add( label );

                            Caret.MoveHorz( label.Width + 2 );

                            size = graphics.MeasureString( pair.Value.Value, MainWindow.Instance.VisPanel.Font );

                            labelV.Height = (int)Math.Ceiling( size.Height );
                            labelV.Width = (int)Math.Ceiling( size.Width );
                            labelV.Bounds = new Rectangle( Caret.x, Caret.y, labelV.Width + 5, labelV.Height );
                            labelV.Text = pair.Value.Value;

                            node.CurrentViewControl = label;
                            pair.Value.CurrentViewControl_Value = labelV;
                            this.EditedXmlElementsByControl[labelV] = pair.Value;

                            controls.Add( labelV );

                            Caret.NextLine( labelV.Height + 2 );
                            Caret.MoveHorz( -(label.Width + 2) );

                        }

                        foreach ( IEditedXmlNodeOrComment child in node.ChildNodes )
                            this.Visualize( child );
                    }
                }
        }
    }
}