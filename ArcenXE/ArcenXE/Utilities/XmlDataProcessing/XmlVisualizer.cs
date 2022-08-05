using ArcenXE.Universal;
using ArcenXE.Utilities.MetadataProcessing;

namespace ArcenXE.Utilities.XmlDataProcessing
{
    public class XmlVisualizer
    {
        private readonly SuperBasicPool<Label> labelPool = new SuperBasicPool<Label>();
        private readonly SuperBasicPool<TextBox> textBoxPool = new SuperBasicPool<TextBox>();
        private readonly SuperBasicPool<ComboBox> comboBoxPool = new SuperBasicPool<ComboBox>();
        private readonly SuperBasicPool<CheckBox> checkBoxPool = new SuperBasicPool<CheckBox>();
        private readonly SuperBasicPool<NumericUpDown> numericUpDownPool = new SuperBasicPool<NumericUpDown>();
        private readonly SuperBasicPool<Button> buttonOpenCheckedBoxListEventPool = new SuperBasicPool<Button>();

        public readonly Dictionary<Control, IEditedXmlElement> EditedXmlElementsByControl = new Dictionary<Control, IEditedXmlElement>();

        private static readonly CheckedListBox checkedListBox = new CheckedListBox(); //todo: defaults method
        private static readonly ToolTip toolTip = new ToolTip(); //todo

        private static bool justInsertedLineBreak = false;

        private const int extraPixels = 5;

        #region Caret
        private static class Caret
        {
            public static int x = 0, y = 0;
            public static void MoveHorizontally( int amount ) => x += amount;
            public static void NextLine( int amount )
            {
                y += amount;
                x = 0;
            }
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
            Control.ControlCollection controls = MainWindow.Instance.RightSplitContainer.Panel2.Controls;

            foreach ( Control control in controls )
            {
                PooledControlTagInfo? pooledControl = control.Tag as PooledControlTagInfo;
                if ( pooledControl == null )
                    continue;

                switch ( control ) // move upper if add different ControlTags
                {
                    case Label label when control is Label:
                        label.Font = MainWindow.Instance.RightSplitContainer.Panel2.Font; //set to default; to be moved in separate SetToDefaults() method
                        break;
                    case Button button when control is Button:
                        button.Image = null;
                        break;
                    case TextBox textBox when control is TextBox:
                        textBox.Multiline = false;
                        textBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;
                        break;
                    case ComboBox comboBox when control is ComboBox:
                        break;
                    case CheckBox checkBox when control is CheckBox:
                        checkBox.Checked = false;
                        break;
                    case NumericUpDown numericUpDown when control is NumericUpDown:
                        break;
                }
                pooledControl.ReturnToPool();
            }
            Caret.Reset();
            controls.Clear();
            EditedXmlElementsByControl.Clear();
        }
        #endregion

        public void VisualizeSelectedNode( IEditedXmlNodeOrComment editedXmlNodeOrComment )
        {
            IEditedXmlNodeOrComment item = editedXmlNodeOrComment;
            Caret.x = 0; // new item, reset caret to start line
            MetadataDocument? currentMetaDoc = MetadataStorage.CurrentVisMetadata;
            if ( currentMetaDoc == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR: CurrentVisMetadata is NULL in VisualizeSelectedNode()!", Verbosity.ShowAsError );
                return;
            }
            Graphics graphics = MainWindow.Instance.RightSplitContainer.Panel2.CreateGraphics();
            Control.ControlCollection controls = MainWindow.Instance.RightSplitContainer.Panel2.Controls;
            Dictionary<string, MetaAttribute_Base>? metaAttributes = MetadataStorage.CurrentVisMetadata?.TopLevelNode?.AttributesData;
            if ( metaAttributes == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR: Metadata attributes are NULL in VisualizeSelectedNode()!", Verbosity.ShowAsError );
                return;
            }

            using ( graphics )
            {
                if ( item is EditedXmlComment comment )
                {
                    #region Comment
                    TextBox textBox = this.textBoxPool.GetOrAdd( null );
                    SizeF size = graphics.MeasureString( comment.Data, MainWindow.Instance.RightSplitContainer.Panel2.Font );

                    textBox.Height = (int)Math.Ceiling( size.Height );
                    textBox.Width = (int)Math.Ceiling( size.Width );
                    textBox.Bounds = new Rectangle( Caret.x, Caret.y, textBox.Width + 5, textBox.Height );
                    textBox.Text = comment.Data;

                    comment.CurrentViewControl = textBox;
                    this.EditedXmlElementsByControl[textBox] = comment;

                    controls.Add( textBox );

                    Caret.NextLine( textBox.Height + 5 );
                    #endregion
                }
                else
                {
                    if ( item is EditedXmlNode node ) // loop over metadata
                    {
                        #region TopNode
                        if ( node.NodeCentralID != null ) // top node 
                        { //this code has issues
                            Label label = this.labelPool.GetOrAdd( null );
                            string toWrite = "Top Node Selected: " + node.NodeCentralID.Value;

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
                        #endregion

                        foreach ( KeyValuePair<string, MetaAttribute_Base> pair in metaAttributes ) // read from MetaDoc and lookup in xmldata
                        {
                            // 1 MetaAttribute describes 1 EditedXmlAttribute                            
                            #region NameLabel
                            Label label = this.labelPool.GetOrAdd( null );
                            SizeF size = graphics.MeasureString( pair.Key, MainWindow.Instance.RightSplitContainer.Panel2.Font );

                            label.Height = (int)Math.Ceiling( size.Height );
                            label.Width = (int)Math.Ceiling( size.Width );
                            label.Bounds = new Rectangle( Caret.x, Caret.y, label.Width + 5, label.Height );
                            label.Text = pair.Key;

                            MoveCaretBasedOnLineBreakBefore( pair.Value, label.Height );
                            
                            controls.Add( label );
                            node.CurrentViewControl = label;

                            Caret.MoveHorizontally( label.Width + 3 );
                            #endregion

                            if ( node.Attributes.TryGetValue( pair.Value.Key, out EditedXmlAttribute? xmlAttribute ) )
                            {
                                //GeorgeAttribute value to be printed in VisElementByType
                                xmlAttribute.CurrentViewControl_Name = label;
                                VisElementByType( controls, currentMetaDoc, pair.Value, xmlAttribute, graphics, label.Height );
                            }
                            else
                            {
                                if ( pair.Value.IsRequired )
                                {
                                    // add the empty field on Vis
                                    // need to run again the switch above
                                    VisElementByType( controls, currentMetaDoc, pair.Value, null, graphics, label.Height );
                                }
                                else
                                {
                                    // add to list of addable fields which will be shown at the bottom with a PLUS button
                                }
                            }
                            MoveCaretBasedOnLineBreakAfter( pair.Value, label.Height );
                        }

                        foreach ( IEditedXmlNodeOrComment child in node.ChildNodes )
                            this.VisualizeSelectedNode( child );
                    }
                }
            }
        }

        private void VisElementByType( Control.ControlCollection controls, MetadataDocument currentMetaDoc, MetaAttribute_Base metaAttribute, EditedXmlAttribute? xmlAttribute, Graphics graphics, int controlHeight )
        {
            switch ( metaAttribute.Type )
            {
                #region Bool
                case AttributeType.Bool:
                case AttributeType.BoolInt:
                    {
                        CheckBox boxBool = this.checkBoxPool.GetOrAdd( null );
                        boxBool.Bounds = new Rectangle( Caret.x, Caret.y, controlHeight, controlHeight );
                        boxBool.Text = string.Empty;
                        boxBool.CheckAlign = ContentAlignment.MiddleCenter;

                        if ( xmlAttribute != null )
                        {
                            if ( metaAttribute.Type == AttributeType.Bool )
                                boxBool.Checked = bool.Parse( xmlAttribute.Value );
                            if ( metaAttribute.Type == AttributeType.BoolInt )
                            {
                                if ( int.Parse( xmlAttribute.Value ) == 1 )
                                    boxBool.Checked = true;
                                if ( int.Parse( xmlAttribute.Value ) == 0 )
                                    boxBool.Checked = true;
                            }

                            ((PooledControlTagInfo)boxBool.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = boxBool;
                        }
                        else
                        {
                            if ( metaAttribute.Type == AttributeType.Bool && ((MetaAttribute_Bool)metaAttribute).Default == true )
                                boxBool.Checked = true;
                            else if ( metaAttribute.Type == AttributeType.BoolInt && ((MetaAttribute_BoolInt)metaAttribute).Default == 1 )
                                boxBool.Checked = true;
                            else
                                boxBool.Checked = false;
                        }

                        controls.Add( boxBool );
                        Caret.NextLine( boxBool.Height + extraPixels );
                    }
                    break;
                #endregion

                #region String
                case AttributeType.String:
                    {
                        TextBox textBox = this.textBoxPool.GetOrAdd( null );
                        textBox.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_String)metaAttribute).ContentWidthPx, controlHeight );
                        textBox.MaxLength = ((MetaAttribute_String)metaAttribute).MaxLength;
                        //textboxes don't have a MinLength property, check it at doc validation time ( used for hex colours )

                        if ( xmlAttribute != null )
                        {
                            textBox.Text = xmlAttribute.Value;
                            ((PooledControlTagInfo)textBox.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = textBox;
                        }
                        else
                            textBox.Text = ((MetaAttribute_String)metaAttribute).Default;

                        controls.Add( textBox );
                        Caret.NextLine( textBox.Height + extraPixels );
                    }
                    break;
                #endregion

                #region StringMultiLine
                case AttributeType.StringMultiLine:
                    {
                        TextBox textBox = this.textBoxPool.GetOrAdd( null );
                        int lines = ((MetaAttribute_StringMultiline)metaAttribute).ShowLines;
                        textBox.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_StringMultiline)metaAttribute).ContentWidthPx, controlHeight );
                        textBox.Height *= lines;
                        textBox.WordWrap = true;
                        textBox.Multiline = true;
                        textBox.AcceptsReturn = true;
                        textBox.ScrollBars = ScrollBars.Vertical;
                        textBox.MaxLength = ((MetaAttribute_StringMultiline)metaAttribute).MaxLength;

                        if ( xmlAttribute != null )
                        {
                            textBox.Text = xmlAttribute.Value;
                            ((PooledControlTagInfo)textBox.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = textBox;
                        }
                        else
                            textBox.Text = ((MetaAttribute_StringMultiline)metaAttribute).Default;

                        controls.Add( textBox );
                        Caret.NextLine( (textBox.Height * lines) + extraPixels );
                    }
                    break;
                #endregion

                #region ArbitraryString
                case AttributeType.ArbitraryString:
                    {
                        ComboBox comboBox = this.comboBoxPool.GetOrAdd( null );
                        comboBox.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_ArbitraryString)metaAttribute).ContentWidthPx, controlHeight );
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                        foreach ( string option in ((MetaAttribute_ArbitraryString)metaAttribute).Options )
                            comboBox.Items.Add( option );

                        if ( xmlAttribute != null )
                        {
                            comboBox.SelectedText = xmlAttribute.Value;
                            ((PooledControlTagInfo)comboBox.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = comboBox;
                        }
                        else
                        {
                            comboBox.SelectedText = ((MetaAttribute_ArbitraryString)metaAttribute).Default;
                        }

                        controls.Add( comboBox );
                        Caret.NextLine( comboBox.Height + extraPixels );
                    }
                    break;
                #endregion

                #region Int
                case AttributeType.Int:
                    {
                        NumericUpDown numeric = this.numericUpDownPool.GetOrAdd( null );
                        numeric.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Int)metaAttribute).MinimumDigits * 20, controlHeight ); // width set by meta

                        numeric.ThousandsSeparator = true;
                        numeric.DecimalPlaces = 0;
                        numeric.AutoSize = true;
                        numeric.Maximum = ((MetaAttribute_Int)metaAttribute).Max;
                        numeric.Minimum = ((MetaAttribute_Int)metaAttribute).Min;

                        if ( xmlAttribute != null )
                        {
                            numeric.Value = int.Parse( xmlAttribute.Value );
                            ((PooledControlTagInfo)numeric.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = numeric;
                        }
                        else
                            numeric.Value = ((MetaAttribute_Int)metaAttribute).Default;

                        controls.Add( numeric );
                        Caret.NextLine( numeric.Height + extraPixels );
                    }
                    break;
                #endregion

                #region Float
                case AttributeType.Float:
                    {
                        NumericUpDown numeric = this.numericUpDownPool.GetOrAdd( null );
                        numeric.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Float)metaAttribute).MinimumDigits * 20, controlHeight ); // width set by meta

                        numeric.ThousandsSeparator = true;
                        numeric.DecimalPlaces = ((MetaAttribute_Float)metaAttribute).Precision;
                        numeric.Maximum = Convert.ToDecimal( ((MetaAttribute_Float)metaAttribute).Max );
                        numeric.Minimum = Convert.ToDecimal( ((MetaAttribute_Float)metaAttribute).Min );

                        if ( xmlAttribute != null )
                        {
                            numeric.Value = decimal.Parse( xmlAttribute.Value );
                            ((PooledControlTagInfo)numeric.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = numeric;
                        }
                        else
                            numeric.Value = Convert.ToDecimal( ((MetaAttribute_Float)metaAttribute).Default );

                        controls.Add( numeric );
                        Caret.NextLine( numeric.Height + extraPixels );
                    }
                    break;
                #endregion

                #region ArbitraryNode
                case AttributeType.ArbitraryNode:
                    {
                        ComboBox comboBox = this.comboBoxPool.GetOrAdd( null );
                        comboBox.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_ArbitraryNode)metaAttribute).ContentWidthPx, controlHeight );
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                        XmlDataTable? table = XmlRootFolders.GetXmlDataTableByName( ((MetaAttribute_ArbitraryNode)metaAttribute).NodeSource );
                        if ( table == null )
                        {
                            //complain
                            return;
                        }
                        List<TopNodesCaching.TopNode>? topNodesCache = TopNodesCaching.GetAllNodesForDataTable( table.MetaDoc );
                        if ( topNodesCache == null )
                        {
                            ArcenDebugging.LogSingleLine( "ERROR: topNodes is NULL in VisElementByType() - AttributeType.ArbitraryNode!", Verbosity.ShowAsError );
                            return;
                        }
                        foreach ( TopNodesCaching.TopNode? item in topNodesCache )
                            if ( item != null )
                                comboBox.Items.Add( item.UserFacingName );

                        if ( xmlAttribute != null )
                        {
                            comboBox.SelectedText = xmlAttribute.Value;
                            ((PooledControlTagInfo)comboBox.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = comboBox;
                        }
                        else
                            comboBox.SelectedText = ((MetaAttribute_ArbitraryNode)metaAttribute).Default.Name;

                        controls.Add( comboBox );
                        Caret.NextLine( comboBox.Height + extraPixels );
                    }
                    break;
                #endregion

                #region NodeList
                case AttributeType.NodeList: //needs testing
                    {
                        Button openListButton = this.buttonOpenCheckedBoxListEventPool.GetOrAdd( ( Button newButton ) =>
                        {
                            newButton.Click += new EventHandler( OpenCheckedBoxList_ButtonClickEvent );
                            newButton.Leave += new EventHandler( OpenCheckedBoxList_ButtonLeaveEvent );
                        } );
                        openListButton.Bounds = new Rectangle( Caret.x, Caret.y, controlHeight, controlHeight );
                        Bitmap icon = new Bitmap( AppContext.BaseDirectory.Replace( @"bin\Debug\net6.0-windows", @"Assets\Icons\iconoir\arrowDown\" ) + "arrowDown18.png" );
                        openListButton.Image = icon;
                        openListButton.Text = string.Empty;

                        controls.Add( openListButton );
                        Caret.MoveHorizontally( controlHeight );

                        checkedListBox.Visible = false;
                        checkedListBox.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_NodeList)metaAttribute).ContentWidthPx, controlHeight );
                        checkedListBox.SelectionMode = SelectionMode.MultiExtended;

                        XmlDataTable? table = XmlRootFolders.GetXmlDataTableByName( ((MetaAttribute_NodeList)metaAttribute).NodeSource );
                        if ( table == null )
                        {
                            //complain
                            return;
                        }
                        List<TopNodesCaching.TopNode>? topNodesCache = TopNodesCaching.GetAllNodesForDataTable( table.MetaDoc );
                        if ( topNodesCache == null )
                        {
                            ArcenDebugging.LogSingleLine( "ERROR: topNodes is NULL in VisElementByType() - AttributeType.ArbitraryNode!", Verbosity.ShowAsError );
                            return;
                        }
                        foreach ( TopNodesCaching.TopNode? item in topNodesCache )
                            if ( item != null )
                                checkedListBox.Items.Add( item.UserFacingName );

                        if ( xmlAttribute != null )
                        {
                            string[] alreadySelectedNodes = xmlAttribute.Value.Split( ',' );
                            for ( int i = 0; i < alreadySelectedNodes.Length; i++ ) //validation??
                                checkedListBox.SelectedIndices.Add( i );

                            ((PooledControlTagInfo)checkedListBox.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = checkedListBox;
                        }
                        else
                        {
                            List<ReferenceXmlNode> defaultNodes = ((MetaAttribute_NodeList)metaAttribute).Defaults;
                            for ( int i = 0; i < defaultNodes.Count; i++ )
                                checkedListBox.SelectedIndices.Add( i );
                        }

                        controls.Add( checkedListBox );
                        Caret.NextLine( (checkedListBox.Height * 5) + extraPixels );
                    }
                    break;
                #endregion

                #region FolderList
                case AttributeType.FolderList:
                    {
                        Button openListButton = this.buttonOpenCheckedBoxListEventPool.GetOrAdd( ( Button newButton ) =>
                        {
                            newButton.Click += new EventHandler( OpenCheckedBoxList_ButtonClickEvent );
                            newButton.Leave += new EventHandler( OpenCheckedBoxList_ButtonLeaveEvent );
                        } );
                        openListButton.Bounds = new Rectangle( Caret.x, Caret.y, controlHeight, controlHeight );
                        Bitmap icon = new Bitmap( AppContext.BaseDirectory.Replace( @"bin\Debug\net6.0-windows", @"Assets\Icons\iconoir\arrowDown\" ) + "arrowDown18.png" );
                        openListButton.Image = icon;
                        openListButton.Text = string.Empty;

                        controls.Add( openListButton );
                        Caret.MoveHorizontally( controlHeight );

                        checkedListBox.Visible = false;
                        checkedListBox.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_FolderList)metaAttribute).ContentWidthPx, controlHeight );
                        checkedListBox.SelectionMode = SelectionMode.MultiExtended;


                        List<string> folders = GetFolderListForDropdown( ((MetaAttribute_FolderList)metaAttribute).FolderSource );
                        foreach ( string folder in folders )
                            checkedListBox.Items.Add( folder );

                        if ( xmlAttribute != null )
                        {
                            List<string> alreadySelectedFolders = ((MetaAttribute_FolderList)metaAttribute).Defaults;
                            for ( int i = 0; i < alreadySelectedFolders.Count; i++ )
                                checkedListBox.SelectedIndices.Add( i );

                            ((PooledControlTagInfo)checkedListBox.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = checkedListBox;
                        }
                        else
                            checkedListBox.SelectedIndex = -1;

                        controls.Add( checkedListBox );
                        Caret.NextLine( (checkedListBox.Height * 5) + extraPixels );
                    }
                    break;
                #endregion

                #region Point
                case AttributeType.Point:
                    {
                        NumericUpDown numeric1 = this.numericUpDownPool.GetOrAdd( null );
                        NumericUpDown numeric2 = this.numericUpDownPool.GetOrAdd( null );

                        numeric1.Bounds = new Rectangle( Caret.x, Caret.y, 80, controlHeight );
                        Caret.MoveHorizontally( 80 + extraPixels );
                        numeric2.Bounds = new Rectangle( Caret.x, Caret.y, 80, controlHeight );

                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = 0;
                        numeric1.AutoSize = true;

                        numeric1.Minimum = ((MetaAttribute_Point)metaAttribute).x.Min;
                        numeric1.Maximum = ((MetaAttribute_Point)metaAttribute).x.Max;

                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = 0;
                        numeric2.AutoSize = true;

                        numeric2.Minimum = ((MetaAttribute_Point)metaAttribute).y.Min;
                        numeric2.Maximum = ((MetaAttribute_Point)metaAttribute).y.Max;


                        if ( xmlAttribute != null )
                        {
                            string[] values = xmlAttribute.Value.Split( ',' );
                            numeric1.Value = int.Parse( values[0] );
                            numeric2.Value = int.Parse( values[1] );

                            ((PooledControlTagInfo)numeric1.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = numeric1;
                            ((PooledControlTagInfo)numeric2.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = numeric2;
                        }
                        else
                        {
                            numeric1.Value = ((MetaAttribute_Point)metaAttribute).x.Default;
                            numeric2.Value = ((MetaAttribute_Point)metaAttribute).y.Default;
                        }

                        controls.Add( numeric1 );
                        controls.Add( numeric2 );
                        Caret.NextLine( numeric1.Height + extraPixels );
                    }
                    break;
                #endregion

                #region Vector2
                case AttributeType.Vector2:
                    {
                        NumericUpDown numeric1 = this.numericUpDownPool.GetOrAdd( null );
                        NumericUpDown numeric2 = this.numericUpDownPool.GetOrAdd( null );
                        numeric1.Bounds = new Rectangle( Caret.x, Caret.y, 80, controlHeight );
                        Caret.MoveHorizontally( 80 + extraPixels );
                        numeric2.Bounds = new Rectangle( Caret.x, Caret.y, 80, controlHeight );

                        numeric1.AutoSize = true;
                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = ((MetaAttribute_Vector2)metaAttribute).x.Precision;
                        numeric1.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).x.Min );
                        numeric1.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).x.Max );

                        numeric2.AutoSize = true;
                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = ((MetaAttribute_Vector2)metaAttribute).y.Precision;
                        numeric2.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).y.Min );
                        numeric2.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).y.Max );


                        if ( xmlAttribute != null )
                        {
                            string[] values = xmlAttribute.Value.Split( ',' );
                            numeric1.Value = decimal.Parse( values[0] );
                            numeric2.Value = decimal.Parse( values[1] );

                            ((PooledControlTagInfo)numeric1.Tag).RelatedTo = xmlAttribute; // how to split here
                            xmlAttribute.CurrentViewControl_Value = numeric1;
                            ((PooledControlTagInfo)numeric2.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = numeric2;
                        }
                        else
                        {
                            numeric1.Value = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).x.Default );
                            numeric2.Value = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).y.Default );
                        }

                        controls.Add( numeric1 );
                        controls.Add( numeric2 );
                        Caret.NextLine( numeric1.Height + extraPixels );
                    }
                    break;
                #endregion

                #region Vector3
                case AttributeType.Vector3:
                    {
                        NumericUpDown numeric1 = this.numericUpDownPool.GetOrAdd( null );
                        NumericUpDown numeric2 = this.numericUpDownPool.GetOrAdd( null );
                        NumericUpDown numeric3 = this.numericUpDownPool.GetOrAdd( null );

                        numeric1.Bounds = new Rectangle( Caret.x, Caret.y, 80, controlHeight );
                        Caret.MoveHorizontally( 80 + extraPixels );
                        numeric2.Bounds = new Rectangle( Caret.x, Caret.y, 80, controlHeight );
                        Caret.MoveHorizontally( 80 + extraPixels );
                        numeric3.Bounds = new Rectangle( Caret.x, Caret.y, 80, controlHeight );

                        numeric1.AutoSize = true;
                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).x.Precision;
                        numeric1.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).x.Min );
                        numeric1.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).x.Max );

                        numeric2.AutoSize = true;
                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).y.Precision;
                        numeric2.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).y.Min );
                        numeric2.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).y.Max );

                        numeric3.AutoSize = true;
                        numeric3.ThousandsSeparator = true;
                        numeric3.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).z.Precision;
                        numeric3.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).z.Min );
                        numeric3.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).z.Max );


                        if ( xmlAttribute != null )
                        {
                            string[] values = xmlAttribute.Value.Split( ',' );
                            numeric1.Value = decimal.Parse( values[0] );
                            numeric2.Value = decimal.Parse( values[1] );
                            numeric3.Value = decimal.Parse( values[2] );

                            ((PooledControlTagInfo)numeric1.Tag).RelatedTo = xmlAttribute; // how to split here
                            xmlAttribute.CurrentViewControl_Value = numeric1;
                            ((PooledControlTagInfo)numeric2.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = numeric2;
                            ((PooledControlTagInfo)numeric3.Tag).RelatedTo = xmlAttribute;
                            xmlAttribute.CurrentViewControl_Value = numeric3;
                        }
                        else
                        {
                            numeric1.Value = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).x.Default );
                            numeric2.Value = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).y.Default );
                            numeric3.Value = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).z.Default );
                        }

                        controls.Add( numeric1 );
                        controls.Add( numeric2 );
                        controls.Add( numeric3 );
                        Caret.NextLine( numeric1.Height + extraPixels );
                    }
                    break;
                #endregion

                default:
                    ArcenDebugging.LogSingleLine( $"ERROR: Unknown type {metaAttribute.Type} in Metadata attribute key: {metaAttribute.Key}", Verbosity.DoNotShow );
                    break;
            }

        }

        #region GetFolderListForDropdown
        private static List<string> GetFolderListForDropdown( string folderSource )
        {
            List<string> listOfFolders = new List<string>();
            string[] dirs = Directory.GetDirectories( ProgramPermanentSettings.MainPath + @"\" + folderSource );
            foreach ( string folder in dirs )
                listOfFolders.Add( Path.GetFileNameWithoutExtension( folder ) );
            return listOfFolders;
        }
        #endregion

        #region LineBreak
        private static void MoveCaretBasedOnLineBreakBefore( MetaAttribute_Base metaAttribute, int height ) // for now they'll remain separated 
        {
            switch ( metaAttribute.LinebreakBefore )
            {
                case LineBreakType.Always:
                    if ( !justInsertedLineBreak )
                    {
                        Caret.NextLine( height + extraPixels );
                        justInsertedLineBreak = true;
                    }
                    else
                        justInsertedLineBreak = false;
                    break;
                case LineBreakType.PreferNot:
                    if ( MainWindow.Instance.RightSplitContainer.Panel2.ClientSize.Width - Caret.x < 160 )
                    {
                        Caret.NextLine( height + extraPixels );
                        justInsertedLineBreak = true;
                    }
                    else
                        justInsertedLineBreak = false;
                    break;
            }

        }
        private static void MoveCaretBasedOnLineBreakAfter( MetaAttribute_Base metaAttribute, int height )
        {
            switch ( metaAttribute.LinebreakAfter )
            {
                case LineBreakType.Always:
                    Caret.NextLine( height + extraPixels );
                    justInsertedLineBreak = true;
                    break;
                case LineBreakType.PreferNot:
                    if ( MainWindow.Instance.RightSplitContainer.Panel2.ClientSize.Width - Caret.x < 160 )
                        Caret.NextLine( height + extraPixels );
                    justInsertedLineBreak = true;
                    break;
            }

        }
        #endregion

        #region StringEntityTranscriber
        private static string StringEntityTranscriberFromXml( string text )
        {
            text = text.Replace( "&lt;", "<" );
            text = text.Replace( "&gt;", ">" );
            text = text.Replace( "&amp;", "&" );
            text = text.Replace( "&apos;", "'" ); // might be ignorable
            text = text.Replace( "&quot;", "\"" );
            return text;
        }

        private static string StringEntityTranscriberToXml( string text )
        {
            text = text.Replace( "<", "&lt;" );
            text = text.Replace( ">", "&gt;" );
            text = text.Replace( "&", "&amp;" );
            text = text.Replace( "'", "&apos;" );
            text = text.Replace( "\"", "&quot;" );
            return text;
        }
        #endregion

        #region CalculateBounds
        private static Rectangle CalculateBounds( string text, Graphics graphics )
        {
            SizeF size = graphics.MeasureString( text, MainWindow.Instance.RightSplitContainer.Panel2.Font );
            return new Rectangle( Caret.x, Caret.y, (int)Math.Ceiling( size.Width ), (int)Math.Ceiling( size.Height ) );
        }
        private static Rectangle CalculateBounds( string text, int width, Graphics graphics )
        {
            SizeF size = graphics.MeasureString( text, MainWindow.Instance.RightSplitContainer.Panel2.Font, width );
            return new Rectangle( Caret.x, Caret.y, (int)Math.Ceiling( size.Width ), (int)Math.Ceiling( size.Height ) );
        }
        #endregion

        #region Events
        private void OpenCheckedBoxList_ButtonClickEvent( object? sender, EventArgs e )
        {
            checkedListBox.Visible = true;
        }
        private void OpenCheckedBoxList_ButtonLeaveEvent( object? sender, EventArgs e )
        {
            checkedListBox.Visible = false;
        }
        #endregion
    }
}