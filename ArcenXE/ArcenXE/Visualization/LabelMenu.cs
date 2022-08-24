using ArcenXE.Utilities;

namespace ArcenXE.Visualization.Utilities
{
    public class LabelMenu : ToolStripPanel
    {
        private bool mouseDown;
        private Point lastLocation;
        private readonly Control callerControl;

        public LabelMenu( Control callerControl, int x, int y )
        {
            this.callerControl = callerControl;
            this.Bounds = new Rectangle( x, y, 160, 30 );
            this.Cursor = Cursors.Default;
            this.BackColor = Color.WhiteSmoke;
            this.LabelMenu_Load();
            MainWindow.Instance.RightSplitContainer.Panel2.Controls.Add( this );
            this.BringToFront();
        }

        private void LabelMenu_Load()
        {
            ToolStrip toolStrip = new ToolStrip // toolstrips inside of ToolStripPanel ignore many properties like Dock, Margin, etc.
            {
                BackColor = Color.WhiteSmoke,
                ImageScalingSize = new Size( 24, 24 ),
                Stretch = true,
                Cursor = Cursors.SizeAll,
                GripStyle = ToolStripGripStyle.Hidden
            };
            toolStrip.MouseDown += new MouseEventHandler( ToolStrip_MouseDown );
            toolStrip.MouseMove += new MouseEventHandler( ToolStrip_MouseMove );
            toolStrip.MouseUp += new MouseEventHandler( ToolStrip_MouseUp );

            ToolStripButton deleteAttribute = new ToolStripButton
            {
                Alignment = ToolStripItemAlignment.Left,
                Image = Image.FromFile( ProgramPermanentSettings.AssetsPath + @"Icons\tabler-icons\Trash\trashX24.png" )
            };
            deleteAttribute.Click += new EventHandler( this.DeleteAttribute_Click );
            deleteAttribute.MouseHover += new EventHandler( this.ChangeCursor_MouseEnter );
            toolStrip.Items.Add( deleteAttribute );

            ToolStripButton closeLabelMenu = new ToolStripButton
            {
                Alignment = ToolStripItemAlignment.Right,
                Image = Image.FromFile( ProgramPermanentSettings.AssetsPath + @"Icons\tabler-icons\X24.png" )
            };
            closeLabelMenu.Click += new EventHandler( this.CloseMenu_Click );
            closeLabelMenu.MouseHover += new EventHandler( this.ChangeCursor_MouseEnter );
            toolStrip.Items.Add( closeLabelMenu );
            this.Controls.Add( toolStrip );
        }

        private void DeleteAttribute_Click( object? sender, EventArgs e )
        {
            PooledControlTagInfo tagInfo = (PooledControlTagInfo)callerControl.Tag;
            IUnionElement? uItem = tagInfo.RelatedUnionElement;
            if ( uItem == null )
            {
                ArcenDebugging.LogSingleLine( "uItem == null", Verbosity.DoNotShow );
                this.Hide();
                return;
            }
            IEditedXmlNodeOrComment? item;
            XmlVisualizer vis = new XmlVisualizer();
            switch ( uItem )
            {
                case UnionNode uNode when uItem is UnionNode:
                    {
                        item = uNode.XmlNodeOrComment;
                        if ( item != null )
                        {
                            ((IEditedXmlElement)item).IsDeleted = true;
                            vis.VisualizeSelectedNode( item, uNode.MetaLayer, true );
                        }
                    }
                    break;
                case UnionAttribute uAtt when uItem is UnionAttribute:
                    {
                        if ( uAtt.XmlAttribute == null )
                        {
                            ArcenDebugging.LogSingleLine( "uAtt.XmlAttribute == null", Verbosity.DoNotShow );
                            this.Hide();
                            return;
                        }
                        EditedXmlAttribute eAtt = uAtt.XmlAttribute;
                        if ( uAtt.ParentUnionNode == null || uAtt.ParentUnionNode.XmlNodeOrComment == null )
                        {
                            if ( uAtt.ParentUnionNode == null )
                            {
                                ArcenDebugging.LogSingleLine( "uAtt.ParentUnionNode == null", Verbosity.DoNotShow );
                                this.Hide();
                                return;
                            }
                            if ( uAtt.ParentUnionNode.XmlNodeOrComment == null )
                                ArcenDebugging.LogSingleLine( "uAtt.ParentUnionNode.XmlNodeOrComment == null", Verbosity.DoNotShow );

                            this.Hide();
                            return;
                        }
                        IEditedXmlNodeOrComment? currentXmlNodeOrComment;
                        item = uAtt.ParentUnionNode.XmlNodeOrComment;
                        if ( item.IsComment )
                        {
                            ArcenDebugging.LogSingleLine( "item IsComment", Verbosity.DoNotShow );
                            EditedXmlComment comment = (EditedXmlComment)item;
                            if ( MainWindow.Instance.CurrentXmlTopNodesForVis.TryGetValue( comment.UID, out currentXmlNodeOrComment ) )
                                ((EditedXmlComment)currentXmlNodeOrComment).IsDeleted = true;
                        }
                        else
                        {
                            if ( uAtt.MetaAttribute.Value.IsRequired )
                            {
                                MessageBox.Show( "You cannot remove an attribute that is required", "Unremovable attribute", MessageBoxButtons.OK, MessageBoxIcon.Information );
                                this.Hide();
                                return;
                            }
                            EditedXmlNode node = (EditedXmlNode)item;
                            if ( node.NodeCentralID != null && MainWindow.Instance.CurrentXmlTopNodesForVis.TryGetValue( node.UID, out currentXmlNodeOrComment ) )
                                ((EditedXmlNode)currentXmlNodeOrComment).Attributes[eAtt.Name].IsDeleted = true;
                        }
                        if ( item != null )
                            vis.VisualizeSelectedNode( item, uAtt.ParentUnionNode.MetaLayer, true );
                    }
                    break;
            }
            this.Hide();
        }

        private void CloseMenu_Click( object? sender, EventArgs e )
        {
            this.Hide();
        }

        private void ToolStrip_MouseDown( object? sender, MouseEventArgs e )
        {
            mouseDown = true;
            lastLocation = e.Location;
        }

        private void ToolStrip_MouseMove( object? sender, MouseEventArgs e )
        {
            if ( mouseDown )
            {
                this.Location = new Point( this.Location.X - this.lastLocation.X + e.X, this.Location.Y - this.lastLocation.Y + e.Y );
                this.Update();
            }
        }

        private void ToolStrip_MouseUp( object? sender, MouseEventArgs e ) => mouseDown = false;
        private void ChangeCursor_MouseEnter( object? sender, EventArgs e ) => Cursor = Cursors.Default; // not working
    }
}
