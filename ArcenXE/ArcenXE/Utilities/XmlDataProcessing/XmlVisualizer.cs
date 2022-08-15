using ArcenXE.Universal;
using ArcenXE.Utilities.MetadataProcessing;
using System.Windows.Forms;
using System.Xml.Linq;

namespace ArcenXE.Utilities.XmlDataProcessing
{
    public class XmlVisualizer
    {
        private readonly static SuperBasicPool<Label> labelPool = new SuperBasicPool<Label>();
        private readonly static SuperBasicPool<TextBox> textBoxPool = new SuperBasicPool<TextBox>();
        private readonly static SuperBasicPool<ComboBox> comboBoxPool = new SuperBasicPool<ComboBox>();
        private readonly static SuperBasicPool<CheckBox> checkBoxPool = new SuperBasicPool<CheckBox>();
        private readonly static SuperBasicPool<NumericUpDown> numericUpDownPool = new SuperBasicPool<NumericUpDown>();
        private readonly static SuperBasicPool<Button> buttonOpenCheckedBoxListEventPool = new SuperBasicPool<Button>();

        private readonly static CheckedListBox checkedListBoxDropdown; //todo: defaults method - use static constructor 
        private readonly static CheckedListBox checkedListBoxPlusButton;

        private readonly static ToolTip toolTip = new ToolTip(); //todo
        private readonly static Button plusButton;

        static XmlVisualizer()
        {
            checkedListBoxDropdown = new CheckedListBox();
            checkedListBoxDropdown.LostFocus += new EventHandler( CloseCheckedBoxListDropdown_CBLLeaveEvent );
            checkedListBoxDropdown.SelectionMode = SelectionMode.One;
            checkedListBoxDropdown.CheckOnClick = true;
            checkedListBoxDropdown.Tag = new ControlTagInfo( checkedListBoxDropdown );

            checkedListBoxPlusButton = new CheckedListBox();
            checkedListBoxPlusButton.LostFocus += new EventHandler( CloseCheckedListBoxPlusButton_CBLLeaveEvent );
            checkedListBoxPlusButton.SelectionMode = SelectionMode.One;
            checkedListBoxPlusButton.CheckOnClick = true;
            checkedListBoxPlusButton.Tag = new ControlTagInfo( checkedListBoxDropdown );

            plusButton = new Button();
            Bitmap icon = new Bitmap( ProgramPermanentSettings.AssetsPath + @"Icons\iconoir\plus\plus32.png" );
            plusButton.Image = icon;
            plusButton.Text = string.Empty;
            plusButton.Click += new EventHandler( OpenCheckedListBoxPlusButton_ButtonClickEvent );
        }

        private static bool justInsertedLineBreak = false; //can't be static for multiple XmlVis

        private const int extraPixels = 18;
        private int genericHeightBasedOnFontUsed;

        #region Caret
        private static class Caret //can't be static for multiple XmlVis
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
                PooledControlTagInfo? pooledControlTagData = control.Tag as PooledControlTagInfo;
                if ( pooledControlTagData == null )
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
                        comboBox.Items.Clear();
                        break;
                    case CheckBox checkBox when control is CheckBox:
                        checkBox.Checked = false;
                        break;
                    case NumericUpDown numericUpDown when control is NumericUpDown:
                        break;
                }
                pooledControlTagData.ReturnToPool();
            }
            Caret.Reset();
            controls.Clear();
            checkedListBoxDropdown.Items.Clear();
            checkedListBoxPlusButton.Items.Clear();
        }
        #endregion

        public void VisualizeSelectedNode( IEditedXmlNodeOrComment editedXmlNodeOrComment, bool forceClearVis = false, UnionNode? parentUnionNode = null )
        {
            if ( forceClearVis )
                this.ReturnAllToPool();

            IEditedXmlNodeOrComment item = editedXmlNodeOrComment;
            Caret.x = 0; // new item, reset caret to start line

            MetadataDocument? currentMetaDoc = MetadataStorage.CurrentVisMetadata;
            if ( currentMetaDoc == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR: CurrentVisMetadata is NULL in VisualizeSelectedNode()!", Verbosity.ShowAsError );
                return;
            }

            Dictionary<string, MetaAttribute_Base>? metaAttributes = MetadataStorage.CurrentVisMetadata?.TopLevelNode?.AttributesData;
            if ( metaAttributes == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR: Metadata attributes are NULL in VisualizeSelectedNode()!", Verbosity.ShowAsError );
                return;
            }

            Dictionary<string, MetaAttribute_Base> addableAttributes = new Dictionary<string, MetaAttribute_Base>();
            Graphics graphics = MainWindow.Instance.RightSplitContainer.Panel2.CreateGraphics();
            Control.ControlCollection controls = MainWindow.Instance.RightSplitContainer.Panel2.Controls;
            UnionNode currentUnionNode = new UnionNode( currentMetaDoc );
            if ( parentUnionNode != null )
                currentUnionNode.ParentUnionNode = parentUnionNode;

            using ( graphics )
            {
                int labelHeight = 0;
                SizeF generalSize = graphics.MeasureString( "A1BCDEtest0", MainWindow.Instance.RightSplitContainer.Panel2.Font );
                genericHeightBasedOnFontUsed = (int)Math.Ceiling( generalSize.Height );

                if ( item is EditedXmlComment comment )
                {
                    #region Comment
                    TextBox textBox = textBoxPool.GetOrAdd( ( TextBox newTextBox ) =>
                    {
                        newTextBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                    } );
                    SizeF size = graphics.MeasureString( comment.Data, MainWindow.Instance.RightSplitContainer.Panel2.Font );

                    textBox.Height = (int)Math.Ceiling( size.Height );
                    textBox.Width = (int)Math.Ceiling( size.Width );
                    textBox.Bounds = new Rectangle( Caret.x, Caret.y, textBox.Width + 5, textBox.Height );
                    textBox.Text = comment.Data;

                    comment.CurrentViewControl = textBox;

                    currentUnionNode.XmlNodeOrComment = comment;
                    currentUnionNode.Controls.Add( textBox );
                    ((PooledControlTagInfo)textBox.Tag).RelatedUnionElement = currentUnionNode;

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
                        { //this code has issues // ?? what issues?
                            Label label = labelPool.GetOrAdd( null );
                            string toWrite = "Top Node Selected: " + node.NodeCentralID.ValueOnDisk;

                            label.Font = new Font( label.Font, FontStyle.Bold );
                            SizeF size = graphics.MeasureString( toWrite, label.Font );

                            label.Height = (int)Math.Ceiling( size.Height );
                            label.Width = (int)Math.Ceiling( size.Width );
                            label.Bounds = new Rectangle( Caret.x, Caret.y, label.Width + 5, label.Height );
                            label.Text = toWrite;

                            node.CurrentViewControl = label;

                            currentUnionNode.XmlNodeOrComment = item;
                            currentUnionNode.Controls.Add( label );
                            currentUnionNode.NodeData = new UnionTopNodeAttribute( node.NodeCentralID.GetEffectiveValue(), node.NodeCentralID );
                            ((PooledControlTagInfo)label.Tag).RelatedUnionElement = currentUnionNode;

                            controls.Add( label );

                            Caret.NextLine( label.Height + 2 );
                        }
                        #endregion

                        bool insertedToVis = false;
                        foreach ( KeyValuePair<string, MetaAttribute_Base> pair in metaAttributes ) // read from MetaDoc and lookup in xmldata
                        {
                            // 1 MetaAttribute describes 1 EditedXmlAttribute                            

                            if ( node.Attributes.TryGetValue( pair.Value.Key, out EditedXmlAttribute? xmlAttribute ) )
                            {
                                //XML Attribute value to be printed in VisElementByType
                                UnionAttribute unionAttribute = new UnionAttribute( pair.Value );
                                labelHeight = PrintLabelToVis( controls, pair, node, xmlAttribute, unionAttribute, graphics );
                                VisElementByType( controls, currentMetaDoc, pair.Value, xmlAttribute, currentUnionNode, unionAttribute, graphics, labelHeight );
                                // if type == nodelist or folderlist -- new method //todo
                                //CheckedListBoxTagData tagData;
                                //tagData.metaAttributes = metaAttribute;
                                //tagData.vis = this;
                                //tagData.node = node;
                                //checkedListBoxDropdown.Tag = tagData;
                                currentUnionNode.UnionAttributes.Add( unionAttribute );
                                insertedToVis = true;
                            }
                            else
                            {
                                if ( pair.Value.IsRequired )
                                {
                                    // add the empty field on Vis
                                    UnionAttribute unionAttribute = new UnionAttribute( pair.Value );
                                    labelHeight = PrintLabelToVis( controls, pair, node, null, unionAttribute, graphics );
                                    VisElementByType( controls, currentMetaDoc, pair.Value, null, currentUnionNode, unionAttribute, graphics, labelHeight );
                                    currentUnionNode.UnionAttributes.Add( unionAttribute );
                                    insertedToVis = true;
                                }
                                else
                                {
                                    // add to dictionary of addable fields which will be listed at the bottom when pressing the PLUS button
                                    addableAttributes.Add( pair.Key, pair.Value );
                                    checkedListBoxPlusButton.Items.Add( pair.Key );
                                    insertedToVis = false;
                                }
                            }
                            if ( insertedToVis )
                                MoveCaretBasedOnLineBreakAfter( pair.Value, labelHeight );
                            insertedToVis = false;
                        }

                        foreach ( IEditedXmlNodeOrComment child in node.ChildNodes )
                            this.VisualizeSelectedNode( child, parentUnionNode: currentUnionNode );

                        Caret.NextLine( labelHeight );

                        #region PlusButton
                        Caret.NextLine( labelHeight );
                        Caret.MoveHorizontally( 5 );

                        plusButton.Bounds = new Rectangle( Caret.x, Caret.y, genericHeightBasedOnFontUsed, genericHeightBasedOnFontUsed ); //maybe use a fixed size?

                        controls.Add( plusButton );
                        Caret.MoveHorizontally( genericHeightBasedOnFontUsed );

                        checkedListBoxPlusButton.Visible = false;
                        checkedListBoxPlusButton.Bounds = new Rectangle( Caret.x, Caret.y, 300, genericHeightBasedOnFontUsed * checkedListBoxPlusButton.Items.Count );

                        CheckedListBoxTagData tagData;
                        tagData.metaAttributes = metaAttributes;
                        tagData.vis = this;
                        tagData.node = node;
                        checkedListBoxPlusButton.Tag = tagData;

                        controls.Add( checkedListBoxPlusButton );
                        #endregion
                    }
                }
            }
        }

        #region NameLabel
        private int PrintLabelToVis( Control.ControlCollection controls, KeyValuePair<string, MetaAttribute_Base> pair, EditedXmlNode node, EditedXmlAttribute? xmlAttribute, UnionAttribute uAttribute, Graphics graphics )
        {
            Label label = labelPool.GetOrAdd( ( Label newLabel ) =>
            {
                newLabel.Click += new EventHandler( OpenSmallMenuOnLabelRightClick );
                newLabel.DoubleClick += new EventHandler( OpenSmallMenuOnLabelDoubleClick );
            } );
            SizeF size = graphics.MeasureString( pair.Key, MainWindow.Instance.RightSplitContainer.Panel2.Font );
            label.Height = (int)Math.Ceiling( size.Height );
            label.Width = (int)Math.Ceiling( size.Width );

            MoveCaretBasedOnLineBreakBefore( pair.Value, label.Width + 3, label.Height );

            label.Bounds = new Rectangle( Caret.x, Caret.y, label.Width + 5, label.Height );
            label.Text = pair.Key;

            controls.Add( label );
            node.CurrentViewControl = label;
            if ( xmlAttribute != null )
            {
                xmlAttribute.CurrentViewControl_Name = label;
                uAttribute.XmlAttribute = xmlAttribute;
            }
            uAttribute.Controls.Add( label );

            Caret.MoveHorizontally( label.Width + 3 );
            return label.Height;
        }
        #endregion

        private void VisElementByType( Control.ControlCollection controls, MetadataDocument currentMetaDoc, MetaAttribute_Base metaAttribute, EditedXmlAttribute? xmlAttribute,
                                       UnionNode currentUnionNode, UnionAttribute uAttribute, Graphics graphics, int controlHeight )
        {
            switch ( metaAttribute.Type )
            {
                #region Bool
                case AttributeType.Bool:
                case AttributeType.BoolInt:
                    {
                        CheckBox boxBool = checkBoxPool.GetOrAdd( ( CheckBox newCheckBox ) =>
                        {
                            newCheckBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        boxBool.Bounds = new Rectangle( Caret.x, Caret.y, controlHeight, controlHeight );
                        boxBool.Text = string.Empty;
                        boxBool.CheckAlign = ContentAlignment.MiddleCenter;

                        if ( xmlAttribute != null )
                        {
                            if ( metaAttribute.Type == AttributeType.Bool )
                            {
                                //boxBool.Checked = bool.Parse( xmlAttribute.GetEffectiveValue() );
                                bool.TryParse( xmlAttribute.GetEffectiveValue(), out bool result );
                                boxBool.Checked = result;
                            }
                            if ( metaAttribute.Type == AttributeType.BoolInt )
                            {
                                if ( int.Parse( xmlAttribute.GetEffectiveValue() ) == 1 )
                                    boxBool.Checked = true;
                                if ( int.Parse( xmlAttribute.GetEffectiveValue() ) == 0 )
                                    boxBool.Checked = true;
                            }
                            LinkDataAndExecuteCommonActions( controls, boxBool, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            if ( metaAttribute.Type == AttributeType.Bool && ((MetaAttribute_Bool)metaAttribute).Default == true )
                                boxBool.Checked = true;
                            else if ( metaAttribute.Type == AttributeType.BoolInt && ((MetaAttribute_BoolInt)metaAttribute).Default == 1 )
                                boxBool.Checked = true;
                            else
                                boxBool.Checked = false;
                            LinkDataAndExecuteCommonActions( controls, boxBool, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( controlHeight + extraPixels );
                    }
                    break;
                #endregion

                #region String
                case AttributeType.String:
                    {
                        TextBox textBox = textBoxPool.GetOrAdd( ( TextBox newTextBox ) =>
                        {
                            newTextBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        textBox.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_String)metaAttribute).ContentWidthPx, controlHeight );
                        textBox.MaxLength = ((MetaAttribute_String)metaAttribute).MaxLength;
                        //textboxes don't have a MinLength property, check it at doc validation time ( used for hex colours )

                        if ( xmlAttribute != null )
                        {
                            textBox.Text = xmlAttribute.GetEffectiveValue();
                            LinkDataAndExecuteCommonActions( controls, textBox, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            textBox.Text = ((MetaAttribute_String)metaAttribute).Default;
                            LinkDataAndExecuteCommonActions( controls, textBox, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( ((MetaAttribute_String)metaAttribute).ContentWidthPx + extraPixels );
                    }
                    break;
                #endregion

                #region StringMultiLine
                case AttributeType.StringMultiLine:
                    {
                        TextBox textBox = textBoxPool.GetOrAdd( ( TextBox newTextBox ) =>
                        {
                            newTextBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
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
                            textBox.Text = xmlAttribute.GetEffectiveValue();
                            LinkDataAndExecuteCommonActions( controls, textBox, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            textBox.Text = ((MetaAttribute_StringMultiline)metaAttribute).Default;
                            LinkDataAndExecuteCommonActions( controls, textBox, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( ((MetaAttribute_StringMultiline)metaAttribute).ContentWidthPx + extraPixels );
                    }
                    break;
                #endregion

                #region ArbitraryString
                case AttributeType.ArbitraryString:
                    {
                        ComboBox comboBox = comboBoxPool.GetOrAdd( ( ComboBox newComboBox ) =>
                        {
                            newComboBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        comboBox.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_ArbitraryString)metaAttribute).ContentWidthPx, controlHeight );
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                        foreach ( string option in ((MetaAttribute_ArbitraryString)metaAttribute).Options )
                            comboBox.Items.Add( option );

                        if ( xmlAttribute != null )
                        {
                            xmlAttribute.CurrentViewControl_Value = comboBox;
                            LinkDataAndExecuteCommonActions( controls, comboBox, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            comboBox.SelectedText = ((MetaAttribute_ArbitraryString)metaAttribute).Default;
                            LinkDataAndExecuteCommonActions( controls, comboBox, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( ((MetaAttribute_ArbitraryString)metaAttribute).ContentWidthPx + extraPixels );
                    }
                    break;
                #endregion

                #region Int
                case AttributeType.Int:
                    {
                        NumericUpDown numeric = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        numeric.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Int)metaAttribute).ContentWidthPx, controlHeight );
                        numeric.ThousandsSeparator = true;
                        numeric.DecimalPlaces = 0;
                        numeric.Maximum = ((MetaAttribute_Int)metaAttribute).Max;
                        numeric.Minimum = ((MetaAttribute_Int)metaAttribute).Min;

                        if ( xmlAttribute != null )
                        {
                            numeric.Value = int.Parse( xmlAttribute.GetEffectiveValue() );
                            LinkDataAndExecuteCommonActions( controls, numeric, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );

                        }
                        else
                        {
                            numeric.Value = ((MetaAttribute_Int)metaAttribute).Default;
                            LinkDataAndExecuteCommonActions( controls, numeric, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( ((MetaAttribute_Int)metaAttribute).ContentWidthPx + extraPixels );
                    }
                    break;
                #endregion

                #region Float
                case AttributeType.Float:
                    {
                        NumericUpDown numeric = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        numeric.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Float)metaAttribute).ContentWidthPx, controlHeight ); // width set by meta

                        numeric.ThousandsSeparator = true;
                        numeric.DecimalPlaces = ((MetaAttribute_Float)metaAttribute).Precision;
                        numeric.Maximum = Convert.ToDecimal( ((MetaAttribute_Float)metaAttribute).Max );
                        numeric.Minimum = Convert.ToDecimal( ((MetaAttribute_Float)metaAttribute).Min );

                        if ( xmlAttribute != null )
                        {
                            numeric.Value = decimal.Parse( xmlAttribute.GetEffectiveValue() );
                            LinkDataAndExecuteCommonActions( controls, numeric, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            numeric.Value = Convert.ToDecimal( ((MetaAttribute_Float)metaAttribute).Default );
                            LinkDataAndExecuteCommonActions( controls, numeric, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( ((MetaAttribute_Float)metaAttribute).ContentWidthPx + extraPixels );
                    }
                    break;
                #endregion

                #region ArbitraryNode
                case AttributeType.ArbitraryNode:
                    {
                        ComboBox comboBox = comboBoxPool.GetOrAdd( ( ComboBox newComboBox ) =>
                        {
                            newComboBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            newComboBox.SelectedIndexChanged += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        comboBox.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_ArbitraryNode)metaAttribute).ContentWidthPx, controlHeight );
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;

                        XmlDataTable? nodeSourceTable = XmlRootFolders.GetXmlDataTableByName( ((MetaAttribute_ArbitraryNode)metaAttribute).NodeSource );
                        if ( nodeSourceTable == null )
                            return;

                        List<TopNodesCaching.TopNode>? topNodesCache = TopNodesCaching.GetAllNodesForDataTable( nodeSourceTable.MetaDoc );
                        if ( topNodesCache == null )
                        {
                            ArcenDebugging.LogSingleLine( "ERROR: topNodes is NULL in VisElementByType() - AttributeType.ArbitraryNode!", Verbosity.ShowAsError );
                            return;
                        }
                        foreach ( TopNodesCaching.TopNode? item in topNodesCache )
                            if ( item != null )
                                comboBox.Items.Add( item.CentralID );

                        if ( xmlAttribute != null )
                        {
                            comboBox.SelectedIndex = comboBox.FindStringExact( xmlAttribute.GetEffectiveValue() );
                            LinkDataAndExecuteCommonActions( controls, comboBox, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else if ( ((MetaAttribute_ArbitraryNode)metaAttribute).Default != string.Empty )
                        {
                            comboBox.Items.Add( ((MetaAttribute_ArbitraryNode)metaAttribute).Default );
                            comboBox.SelectedIndex = 0;
                            LinkDataAndExecuteCommonActions( controls, comboBox, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( ((MetaAttribute_ArbitraryNode)metaAttribute).ContentWidthPx + extraPixels );
                    }
                    break;
                #endregion

                #region NodeList
                case AttributeType.NodeList:
                    {
                        Button openListButton = buttonOpenCheckedBoxListEventPool.GetOrAdd( (Action<Button>?)(( Button newButton ) =>
                        {
                            newButton.Click += new EventHandler( OpenCheckedBoxListDropdown_ButtonClickEvent );
                            newButton.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        }) );
                        openListButton.Bounds = new Rectangle( Caret.x, Caret.y, controlHeight, controlHeight );
                        Bitmap icon = new Bitmap( ProgramPermanentSettings.AssetsPath + @"Icons\iconoir\arrowDown\arrowDown18.png" );
                        openListButton.Image = icon;
                        openListButton.Text = string.Empty;

                        controls.Add( openListButton );
                        Caret.MoveHorizontally( controlHeight );

                        checkedListBoxDropdown.Visible = false;
                        checkedListBoxDropdown.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_NodeList)metaAttribute).ContentWidthPx, controlHeight );

                        XmlDataTable? table = XmlRootFolders.GetXmlDataTableByName( ((MetaAttribute_NodeList)metaAttribute).NodeSource );
                        if ( table == null )
                            return;

                        List<TopNodesCaching.TopNode>? topNodesCache = TopNodesCaching.GetAllNodesForDataTable( table.MetaDoc );
                        if ( topNodesCache == null )
                        {
                            ArcenDebugging.LogSingleLine( "ERROR: topNodes is NULL in VisElementByType() - AttributeType.ArbitraryNode!", Verbosity.ShowAsError );
                            return;
                        }
                        foreach ( TopNodesCaching.TopNode? item in topNodesCache )
                            if ( item != null )
                                checkedListBoxDropdown.Items.Add( item.CentralID );

                        if ( xmlAttribute != null )
                        {
                            string[]? alreadySelectedNodes = xmlAttribute.GetEffectiveValue()?.Split( ',' );
                            if ( alreadySelectedNodes == null )
                                return;

                            foreach ( string alreadySelectedNode in alreadySelectedNodes )
                                checkedListBoxDropdown.SelectedIndices.Add( checkedListBoxDropdown.FindStringExact( alreadySelectedNode ) );

                            LinkDataAndExecuteCommonActions( controls, checkedListBoxDropdown, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            int i = 0;
                            foreach ( string defaultString in ((MetaAttribute_NodeList)metaAttribute).Defaults )
                                if ( defaultString != string.Empty )
                                {
                                    checkedListBoxDropdown.Items.Add( defaultString );
                                    checkedListBoxDropdown.SelectedIndices.Add( i );
                                    i++;
                                }
                            LinkDataAndExecuteCommonActions( controls, checkedListBoxDropdown, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( controlHeight + extraPixels );
                    }
                    break;
                #endregion

                #region FolderList
                case AttributeType.FolderList:
                    {
                        Button openListButton = buttonOpenCheckedBoxListEventPool.GetOrAdd( (Action<Button>?)(( Button newButton ) =>
                        {
                            newButton.Click += new EventHandler( OpenCheckedBoxListDropdown_ButtonClickEvent );
                            newButton.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        }) );
                        openListButton.Bounds = new Rectangle( Caret.x, Caret.y, controlHeight, controlHeight );
                        Bitmap icon = new Bitmap( ProgramPermanentSettings.AssetsPath + @"Icons\iconoir\arrowDown\arrowDown18.png" );
                        openListButton.Image = icon;
                        openListButton.Text = string.Empty;

                        controls.Add( openListButton );
                        Caret.MoveHorizontally( controlHeight );

                        checkedListBoxDropdown.Visible = false;
                        checkedListBoxDropdown.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_FolderList)metaAttribute).ContentWidthPx, controlHeight );
                        checkedListBoxDropdown.SelectionMode = SelectionMode.One;


                        List<string> folders = GetFolderListForDropdown( ((MetaAttribute_FolderList)metaAttribute).FolderSource );
                        foreach ( string folder in folders )
                        {
                            checkedListBoxDropdown.Items.Add( folder );
                            ((MetaAttribute_FolderList)metaAttribute).FolderPaths.Add( folder );
                        }

                        if ( xmlAttribute != null )
                        {
                            string[]? alreadySelectedFolders = xmlAttribute.GetEffectiveValue()?.Split( "," );
                            if ( alreadySelectedFolders == null )
                                return;

                            foreach ( string alreadySelectedFolder in alreadySelectedFolders )
                                checkedListBoxDropdown.SelectedIndices.Add( checkedListBoxDropdown.FindStringExact( alreadySelectedFolder ) );

                            LinkDataAndExecuteCommonActions( controls, checkedListBoxDropdown, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            int i = 0;
                            foreach ( string defaultFolder in ((MetaAttribute_FolderList)metaAttribute).Defaults )
                                if ( defaultFolder != string.Empty )
                                {
                                    checkedListBoxDropdown.Items.Add( defaultFolder );
                                    checkedListBoxDropdown.SelectedIndices.Add( i );
                                    i++;
                                }
                            LinkDataAndExecuteCommonActions( controls, checkedListBoxDropdown, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( controlHeight + extraPixels );
                    }
                    break;
                #endregion

                #region Point
                case AttributeType.Point:
                    {
                        NumericUpDown numeric1 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        NumericUpDown numeric2 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );

                        numeric1.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Point)metaAttribute).ContentWidthPx, controlHeight );
                        Caret.MoveHorizontally( ((MetaAttribute_Point)metaAttribute).ContentWidthPx + extraPixels );
                        numeric2.Bounds = new Rectangle( Caret.x, Caret.y, 80, controlHeight );

                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = 0;

                        numeric1.Minimum = ((MetaAttribute_Point)metaAttribute).x.Min;
                        numeric1.Maximum = ((MetaAttribute_Point)metaAttribute).x.Max;

                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = 0;

                        numeric2.Minimum = ((MetaAttribute_Point)metaAttribute).y.Min;
                        numeric2.Maximum = ((MetaAttribute_Point)metaAttribute).y.Max;

                        Control[] tempControls = new Control[2];
                        tempControls[0] = numeric1;
                        tempControls[1] = numeric2;

                        if ( xmlAttribute != null )
                        {
                            string[] values = xmlAttribute.GetEffectiveValue().Split( ',' );
                            numeric1.Value = int.Parse( values[0] );
                            numeric2.Value = int.Parse( values[1] );

                            LinkDataMultipleControls( controls, tempControls, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            numeric1.Value = ((MetaAttribute_Point)metaAttribute).x.Default;
                            numeric2.Value = ((MetaAttribute_Point)metaAttribute).y.Default;
                            LinkDataMultipleControls( controls, tempControls, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( ((MetaAttribute_Point)metaAttribute).ContentWidthPx + extraPixels );
                    }
                    break;
                #endregion

                #region Vector2
                case AttributeType.Vector2:
                    {
                        NumericUpDown numeric1 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        NumericUpDown numeric2 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        numeric1.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx, controlHeight );
                        Caret.MoveHorizontally( ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx + extraPixels );
                        numeric2.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx, controlHeight );

                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = ((MetaAttribute_Vector2)metaAttribute).x.Precision;
                        numeric1.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).x.Min );
                        numeric1.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).x.Max );

                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = ((MetaAttribute_Vector2)metaAttribute).y.Precision;
                        numeric2.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).y.Min );
                        numeric2.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).y.Max );

                        Control[] tempControls = new Control[2];
                        tempControls[0] = numeric1;
                        tempControls[1] = numeric2;

                        if ( xmlAttribute != null )
                        {
                            string[] values = xmlAttribute.GetEffectiveValue().Split( ',' );
                            numeric1.Value = decimal.Parse( values[0] );
                            numeric2.Value = decimal.Parse( values[1] );
                            LinkDataMultipleControls( controls, tempControls, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            numeric1.Value = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).x.Default );
                            numeric2.Value = Convert.ToDecimal( ((MetaAttribute_Vector2)metaAttribute).y.Default );
                            LinkDataMultipleControls( controls, tempControls, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx + extraPixels );
                    }
                    break;
                #endregion

                #region Vector3
                case AttributeType.Vector3:
                    {
                        NumericUpDown numeric1 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        NumericUpDown numeric2 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        NumericUpDown numeric3 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );

                        numeric1.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx, controlHeight );
                        Caret.MoveHorizontally( ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx + extraPixels );
                        numeric2.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx, controlHeight );
                        Caret.MoveHorizontally( ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx + extraPixels );
                        numeric3.Bounds = new Rectangle( Caret.x, Caret.y, ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx, controlHeight );

                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).x.Precision;
                        numeric1.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).x.Min );
                        numeric1.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).x.Max );

                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).y.Precision;
                        numeric2.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).y.Min );
                        numeric2.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).y.Max );

                        numeric3.ThousandsSeparator = true;
                        numeric3.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).z.Precision;
                        numeric3.Minimum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).z.Min );
                        numeric3.Maximum = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).z.Max );

                        Control[] tempControls = new Control[2];
                        tempControls[0] = numeric1;
                        tempControls[1] = numeric2;
                        tempControls[2] = numeric3;

                        if ( xmlAttribute != null )
                        {
                            string[] values = xmlAttribute.GetEffectiveValue().Split( ',' );
                            numeric1.Value = decimal.Parse( values[0] );
                            numeric2.Value = decimal.Parse( values[1] );
                            numeric3.Value = decimal.Parse( values[2] );
                            LinkDataMultipleControls( controls, tempControls, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            numeric1.Value = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).x.Default );
                            numeric2.Value = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).y.Default );
                            numeric3.Value = Convert.ToDecimal( ((MetaAttribute_Vector3)metaAttribute).z.Default );
                            LinkDataMultipleControls( controls, tempControls, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        Caret.MoveHorizontally( ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx + extraPixels );
                    }
                    break;
                #endregion

                default:
                    ArcenDebugging.LogSingleLine( $"ERROR: Unknown type {metaAttribute.Type} in Metadata attribute key: {metaAttribute.Key}", Verbosity.DoNotShow );
                    break;
            }
        }

        #region Liniking Data in UnionNode
        private static void LinkDataMultipleControls( Control.ControlCollection controls, Control[] controlsToAdd, UnionNode currentUNode, UnionAttribute uAttribute,
                                                      EditedXmlAttribute? xmlAttribute, MetaAttribute_Base metaAttribute )
        {
            for ( int i = 0; i < controlsToAdd.Length; i++ )
            {
                if ( i + 1 == controlsToAdd.Length )
                    LinkDataAndExecuteCommonActions( controls, controlsToAdd[i], currentUNode, uAttribute, xmlAttribute, metaAttribute, false );
                else
                    LinkDataAndExecuteCommonActions( controls, controlsToAdd[i], currentUNode, uAttribute, xmlAttribute, metaAttribute );
            }
        }

        private static void LinkDataAndExecuteCommonActions( Control.ControlCollection controls, Control control, UnionNode currentUNode, UnionAttribute uAttribute,
                                                             EditedXmlAttribute? xmlAttribute, MetaAttribute_Base metaAttribute, bool addToUNodeListOfAttributes = true )
        {
            PooledControlTagInfo tagData = (PooledControlTagInfo)control.Tag;
            tagData.RelatedUnionElement = uAttribute;

            uAttribute.Controls.Add( control );

            if ( addToUNodeListOfAttributes )
            {
                uAttribute.MetaAttribute = metaAttribute;
                uAttribute.XmlAttribute = xmlAttribute; // duplicate assignment - done in PrintLabelToVis()
                currentUNode.UnionAttributes.Add( uAttribute );
            }

            controls.Add( control );
            if ( xmlAttribute != null )
                xmlAttribute.CurrentViewControl_Value = control;

            XmlValidator.Validate( control );
        }
        #endregion

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
        private static void MoveCaretBasedOnLineBreakBefore( MetaAttribute_Base metaAttribute, int upcomingCaretHorizonatalMove, int height ) // for now they'll remain separated 
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
                    if ( MainWindow.Instance.RightSplitContainer.Panel2.ClientSize.Width - (Caret.x + upcomingCaretHorizonatalMove + metaAttribute.ContentWidthPx) < 5 )
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
            if ( metaAttribute.Type is AttributeType.StringMultiLine )
                height *= ((MetaAttribute_StringMultiline)metaAttribute).ShowLines;
            switch ( metaAttribute.LinebreakAfter )
            {
                case LineBreakType.Always:
                    Caret.NextLine( height + extraPixels );
                    justInsertedLineBreak = true;
                    break;
                case LineBreakType.PreferNot:
                    if ( MainWindow.Instance.RightSplitContainer.Panel2.ClientSize.Width - Caret.x < 40 )
                    {
                        Caret.NextLine( height + extraPixels );
                        justInsertedLineBreak = true;
                    }
                    else
                        justInsertedLineBreak = false;
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

        #region CheckedBoxLists
        private void OpenCheckedBoxListDropdown_ButtonClickEvent( object? sender, EventArgs e )
        {
            checkedListBoxDropdown.Visible = true;
            checkedListBoxDropdown.Focus();
        }

        private static void CloseCheckedBoxListDropdown_CBLLeaveEvent( object? sender, EventArgs e ) => checkedListBoxDropdown.Visible = false;

        private static void OpenCheckedListBoxPlusButton_ButtonClickEvent( object? sender, EventArgs e )
        {
            checkedListBoxPlusButton.Visible = true;
            checkedListBoxPlusButton.Focus();
        }

        private static void CloseCheckedListBoxPlusButton_CBLLeaveEvent( object? sender, EventArgs e ) //todo: test
        {
            CheckedListBoxTagData tagData = (CheckedListBoxTagData)checkedListBoxPlusButton.Tag;

            foreach ( string attName in checkedListBoxPlusButton.CheckedItems )
            {
                EditedXmlAttribute att = new EditedXmlAttribute();
                att.Name = attName;
                // Need the default value which is type dependant
                switch ( tagData.metaAttributes[attName].Type )
                {
                    case AttributeType.Bool:
                        att.TempValue = ((MetaAttribute_Bool)tagData.metaAttributes[attName]).Default.ToString();
                        break;
                    case AttributeType.BoolInt:
                        att.TempValue = ((MetaAttribute_BoolInt)tagData.metaAttributes[attName]).Default.ToString();
                        break;
                    case AttributeType.String:
                        att.TempValue = ((MetaAttribute_String)tagData.metaAttributes[attName]).Default;
                        break;
                    case AttributeType.StringMultiLine:
                        att.TempValue = ((MetaAttribute_StringMultiline)tagData.metaAttributes[attName]).Default;
                        break;
                    case AttributeType.ArbitraryString:
                        att.TempValue = ((MetaAttribute_ArbitraryString)tagData.metaAttributes[attName]).Default;
                        break;
                    case AttributeType.Int:
                        att.TempValue = ((MetaAttribute_Int)tagData.metaAttributes[attName]).Default.ToString();
                        break;
                    case AttributeType.Float:
                        att.TempValue = ((MetaAttribute_Float)tagData.metaAttributes[attName]).Default.ToString();
                        break;
                    case AttributeType.ArbitraryNode:
                        att.TempValue = ((MetaAttribute_ArbitraryNode)tagData.metaAttributes[attName]).Default;
                        break;
                    case AttributeType.NodeList:
                        foreach ( string nodeForList in ((MetaAttribute_NodeList)tagData.metaAttributes[attName]).Defaults )
                            att.TempValue = nodeForList + ",";
                        att.TempValue = att.TempValue?.Remove( att.TempValue.Length - 1 );
                        break;
                    case AttributeType.FolderList:
                        foreach ( string folderForList in ((MetaAttribute_FolderList)tagData.metaAttributes[attName]).Defaults )
                            att.TempValue = folderForList + ",";
                        att.TempValue = att.TempValue?.Remove( att.TempValue.Length - 1 );
                        break;
                    case AttributeType.Point:
                        att.TempValue = ((MetaAttribute_Point)tagData.metaAttributes[attName]).x.Default.ToString();
                        break;
                    case AttributeType.Vector2:
                        att.TempValue = ((MetaAttribute_Vector2)tagData.metaAttributes[attName]).x.Default.ToString();
                        break;
                    case AttributeType.Vector3:
                        att.TempValue = ((MetaAttribute_Vector3)tagData.metaAttributes[attName]).x.Default.ToString();
                        break;
                }
                tagData.node.Attributes.Add( attName, att );
            }
            checkedListBoxPlusButton.Visible = false;

            tagData.vis.VisualizeSelectedNode( tagData.node, true );
        }
        #endregion

        #region SmallLabelToolBar
        private static void OpenSmallMenuOnLabelRightClick( object? sender, EventArgs e )
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if ( me.Button == MouseButtons.Right )
            {
                LabelMenu labelMenu = new LabelMenu( (Control)sender );
                labelMenu.Show();
            }
        }

        private static void OpenSmallMenuOnLabelDoubleClick( object? sender, EventArgs e )
        {
            MouseEventArgs me = (MouseEventArgs)e;
            SplitContainer container = MainWindow.Instance.RightSplitContainer;
            if ( me.Button == MouseButtons.Left )
            {
                /*Point temp = new Point();
                temp.X += MainWindow.Instance.Location.X + MainWindow.Instance.BigSplitContainer.Panel1.Width + container.Panel1.Width + container.SplitterWidth + ((Label)sender).Location.X;
                Rectangle screenRectangle = MainWindow.Instance.RectangleToScreen( MainWindow.Instance.ClientRectangle );
                int titleHeight = screenRectangle.Top - MainWindow.Instance.Top;
                temp.Y += MainWindow.Instance.Location.Y + MainWindow.Instance.toolStrip1.Height + MainWindow.Instance.toolStrip2.Height + ((Label)sender).Location.Y + titleHeight;
                LabelMenu labelMenu = new LabelMenu( temp.X, temp.Y );*/

                LabelMenu labelMenu = new LabelMenu( (Control)sender );
                labelMenu.Show();
                ArcenDebugging.LogSingleLine( $"container.Location = {container.Location}\nMainWindow.Instance.Location = {MainWindow.Instance.Location}\nMainWindow.Instance.PointToScreen( MainWindow.Instance.Location ) = {MainWindow.Instance.PointToScreen( MainWindow.Instance.Location )}", Verbosity.DoNotShow );
            }
        }
        #endregion

        private void CallValidatorAfterFocusLostOrIndexChanged( object? sender, EventArgs e )
        {
            Control? control = sender as Control;
            if ( control == null )
            {
                ArcenDebugging.LogSingleLine( "Control received via CallValidatorAfterFocusLost Event is null!", Verbosity.DoNotShow );
                return;
            }
            XmlValidator.Validate( control );
        }
        #endregion

        #region XmlValidator
        private static class XmlValidator
        {
            private static readonly PoolWithReference<ErrorProvider> errorProviderPool = new PoolWithReference<ErrorProvider>();

            private static void ClearAndReturnToPool()
            {
                //todo: clear?
                errorProviderPool.ReturnAllToPool();
            }

            public static void Validate( Control control )
            {
                ClearAndReturnToPool();

                ErrorProvider errorProvider = errorProviderPool.GetOrAdd();
                errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
                errorProvider.SetIconAlignment( control, ErrorIconAlignment.TopRight );

                // how to recognize which control is the sender object? use ControlTagInfo
                PooledControlTagInfo tagData = (PooledControlTagInfo)control.Tag;
                if ( tagData.RelatedUnionElement == null )
                {
                    ArcenDebugging.LogSingleLine( "MetaAttribute stored in Control's Tag Data is null! Can't do validation!", Verbosity.DoNotShow );
                    return;
                }
                UnionAttribute uAtt = (UnionAttribute)tagData.RelatedUnionElement;

                if ( uAtt.XmlAttribute == null )
                {
                    ArcenDebugging.LogSingleLine( "Edited Element stored in Control's Tag Data is null! Can't do validation!", Verbosity.DoNotShow );
                    return;
                }

                SetEditedAttributeTempValue( control, uAtt );

                switch ( uAtt.XmlAttribute.Type )
                {
                    #region All Cases
                    case AttributeType.Bool:
                        {
                            string error = ((MetaAttribute_Bool)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.BoolInt:
                        {
                            string error = ((MetaAttribute_BoolInt)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.String:
                        {
                            string error = ((MetaAttribute_String)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.StringMultiLine:
                        {
                            string error = ((MetaAttribute_StringMultiline)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.ArbitraryString:
                        {
                            string error = ((MetaAttribute_ArbitraryString)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Int:
                        {
                            string error = ((MetaAttribute_Int)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Float:
                        {
                            string error = ((MetaAttribute_Float)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.ArbitraryNode:
                        {
                            string error = ((MetaAttribute_ArbitraryNode)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.NodeList:
                        {
                            string error = ((MetaAttribute_NodeList)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.FolderList:
                        {
                            string error = ((MetaAttribute_FolderList)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Point:
                        {
                            string error = ((MetaAttribute_Point)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Vector2:
                        {
                            string error = ((MetaAttribute_Vector2)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Vector3:
                        {
                            string error = ((MetaAttribute_Vector3)uAtt.MetaAttribute).DoValidate( uAtt.XmlAttribute );
                            if ( error != string.Empty )
                            {
                                errorProvider.SetError( control, error );
                            }
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                        #endregion
                }
            }

            private static void SetEditedAttributeTempValue( Control control, UnionAttribute uAtt )
            {
                uAtt.XmlAttribute ??= new EditedXmlAttribute();
                uAtt.XmlAttribute.Name = uAtt.MetaAttribute.Key;
                uAtt.XmlAttribute.Type = uAtt.MetaAttribute.Type;
                uAtt.XmlAttribute.TempValue = control.Text;

                string textOrValueFromControl = string.Empty;
                switch ( control )
                {
                    case TextBox textBox when control is TextBox:
                        textOrValueFromControl = textBox.Text;
                        break;
                    case ComboBox comboBox when control is ComboBox:
                        textOrValueFromControl = comboBox.Text;
                        break;
                    case CheckBox checkBox when control is CheckBox:
                        textOrValueFromControl = checkBox.Checked.ToString().ToLowerInvariant();
                        break;
                    case NumericUpDown numericUpDown when control is NumericUpDown:
                        textOrValueFromControl = numericUpDown.Value.ToString();
                        break;
                }
                if ( textOrValueFromControl != string.Empty )
                    uAtt.XmlAttribute.TempValue = textOrValueFromControl;
            }
        }
        #endregion
    }

    public struct CheckedListBoxTagData
    {
        public Dictionary<string, MetaAttribute_Base> metaAttributes;
        public EditedXmlNode node;
        public XmlVisualizer vis;
    }
}