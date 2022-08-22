using ArcenXE.Universal;
using ArcenXE.Utilities.MetadataProcessing;
using CheckBox = System.Windows.Forms.CheckBox;

namespace ArcenXE.Utilities.XmlDataProcessing
{
    public class XmlVisualizer
    {
        private readonly static SuperBasicPool<Label> labelPool = new SuperBasicPool<Label>();
        private readonly static SuperBasicPool<TextBox> textBoxPool = new SuperBasicPool<TextBox>();
        private readonly static SuperBasicPool<ComboBox> comboBoxPool = new SuperBasicPool<ComboBox>();
        private readonly static SuperBasicPool<CheckBox> checkBoxPool = new SuperBasicPool<CheckBox>();
        private readonly static SuperBasicPool<NumericUpDown> numericUpDownPool = new SuperBasicPool<NumericUpDown>();
        private readonly static SuperBasicPool<Button> openCheckedListBoxPlusButtonEvent_Pool = new SuperBasicPool<Button>();
        private readonly static SuperBasicPool<Button> openCheckedListBoxDropdownButtonEvent_Pool = new SuperBasicPool<Button>();

        private readonly static CheckedListBox checkedListBoxDropdown; //todo: defaults method - use static constructor 
        private readonly static CheckedListBox checkedListBoxPlusButton;

        private readonly static ToolTip toolTip = new ToolTip(); //todo

        static XmlVisualizer()
        {
            checkedListBoxDropdown = new CheckedListBox();
            checkedListBoxDropdown.LostFocus += new EventHandler( CloseCheckedBoxListDropdown_CLBLeaveEvent );
            checkedListBoxDropdown.SelectionMode = SelectionMode.One;
            checkedListBoxDropdown.CheckOnClick = true;
            checkedListBoxDropdown.Tag = new ControlTagInfo( checkedListBoxDropdown );

            checkedListBoxPlusButton = new CheckedListBox();
            checkedListBoxPlusButton.LostFocus += new EventHandler( CloseCheckedListBoxPlusButton_CLBLostFocusEvent );
            checkedListBoxPlusButton.SelectionMode = SelectionMode.One;
            checkedListBoxPlusButton.CheckOnClick = true;
            checkedListBoxPlusButton.Tag = new ControlTagInfo( checkedListBoxDropdown ); //merge with struct tag
        }

        private static DateTime timeOfVisStart = DateTime.UnixEpoch;
        private const int extraPixels = 18; // should use the genericHeight calculated for the used font
        private static int genericHeightBasedOnFontUsed;
        private static int maxHeightInCurrentRow = 0;
        private const int amountOfValuesToDisplayInCLB = 7;

        #region Caret
        public class Caret
        {
            public int x = 0, y = 0, fixedMinX = 0; //make private setters
            public void MoveHorizontally( int amount ) => x += amount;
            public void SetFixedMinX( int x ) => fixedMinX = x;
            public void NextLine( int amount )
            {
                y += amount;
                x = fixedMinX;
                maxHeightInCurrentRow = 0;
            }
            public void Reset()
            {
                x = 0;
                y = 0;
                maxHeightInCurrentRow = 0;
            }
        }
        #endregion

        #region ReturnAllToPool
        public void ReturnAllToPoolAndReset( Caret? caret )
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
            if ( caret != null )
            {
                caret.SetFixedMinX( 0 );
                caret.Reset();
            }
            controls.Clear();
            checkedListBoxDropdown.Items.Clear();
            checkedListBoxPlusButton.Items.Clear();
        }
        #endregion

        public void VisualizeSelectedNode( IEditedXmlNodeOrComment editedXmlNodeOrComment, MetadataNodeLayer? metaNodeLayer, bool forceClearVis = false, UnionNode? parentUnionNode = null, Caret? caret = null )
        {
            timeOfVisStart = DateTime.Now;
            if ( forceClearVis )
            {
                MessagePumpOverriding.SuspendDrawing( MainWindow.Instance.RightSplitContainer.Panel2 );
                this.ReturnAllToPoolAndReset( caret );
            }

            caret ??= new Caret();
            caret.x = caret.fixedMinX; // new item, reset caret to start line

            IEditedXmlNodeOrComment item = editedXmlNodeOrComment;

            MetadataDocument? currentMetaDoc = MetadataStorage.CurrentVisMetadata;
            if ( currentMetaDoc == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR: CurrentVisMetadata is NULL in VisualizeSelectedNode()!", Verbosity.ShowAsError );
                return;
            }
            if ( metaNodeLayer == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR: Metadata Node Layer in VisualizeSelectedNode() is NULL!", Verbosity.ShowAsError );
                return;
            }

            Dictionary<string, MetaAttribute_Base> metaAttributes = metaNodeLayer.AttributesData;
            Graphics graphics = MainWindow.Instance.RightSplitContainer.Panel2.CreateGraphics();
            Control.ControlCollection controls = MainWindow.Instance.RightSplitContainer.Panel2.Controls;
            UnionNode currentUnionNode = new UnionNode( metaNodeLayer );
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
                    textBox.Bounds = new Rectangle( caret.x, caret.y, textBox.Width + 5, textBox.Height );
                    textBox.Text = comment.Data;

                    comment.CurrentViewControl = textBox;

                    currentUnionNode.XmlNodeOrComment = comment;
                    currentUnionNode.Controls.Add( textBox );
                    ((PooledControlTagInfo)textBox.Tag).RelatedUnionElement = currentUnionNode;

                    controls.Add( textBox );

                    caret.NextLine( textBox.Height + 5 );
                    #endregion
                }
                else if ( item is EditedXmlNode node ) // loop over metadata
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
                        label.Bounds = new Rectangle( caret.x, caret.y, label.Width + 5, label.Height );
                        label.Text = toWrite;

                        node.CurrentViewControl = label;

                        currentUnionNode.XmlNodeOrComment = node;
                        currentUnionNode.Controls.Add( label );
                        currentUnionNode.NodeData = new UnionTopNodeAttribute( node.NodeCentralID.GetEffectiveValue(), node.NodeCentralID );
                        ((PooledControlTagInfo)label.Tag).RelatedUnionElement = currentUnionNode;

                        controls.Add( label );

                        caret.NextLine( label.Height + 2 );
                    }
                    #endregion

                    #region Attributes
                    bool insertedToVis = false;
                    foreach ( KeyValuePair<string, MetaAttribute_Base> pair in metaAttributes ) // read from MetaDoc and lookup in xmldata
                    {
                        // 1 MetaAttribute describes 1 EditedXmlAttribute
                        //ArcenDebugging.LogSingleLine( $"pair.value.Key = {pair.Value.Key}", Verbosity.DoNotShow );
                        UnionAttribute unionAttribute = new UnionAttribute( pair );
                        if ( node.Attributes.TryGetValue( pair.Value.Key, out EditedXmlAttribute? xmlAttribute ) )
                        {
                            //ArcenDebugging.LogSingleLine( $"xmlAttribute.Name = {xmlAttribute.Name}\t\txmlAttribute.EffectiveValue = {xmlAttribute.GetEffectiveValue()}", Verbosity.DoNotShow );
                            //XML Attribute value to be printed in VisElementByType
                            labelHeight = PrintLabelToVis( controls, pair, node, xmlAttribute, unionAttribute, graphics, caret );
                            VisElementByType( controls, currentMetaDoc, pair.Value, xmlAttribute, currentUnionNode, unionAttribute, graphics, caret, labelHeight );
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
                                labelHeight = PrintLabelToVis( controls, pair, node, null, unionAttribute, graphics, caret );
                                VisElementByType( controls, currentMetaDoc, pair.Value, null, currentUnionNode, unionAttribute, graphics, caret, labelHeight );
                                currentUnionNode.UnionAttributes.Add( unionAttribute );
                                insertedToVis = true;
                            }
                            else
                            {
                                // will be listed at the bottom when pressing the PLUS button
                                unionAttribute.MetaAttribute = pair;
                                currentUnionNode.UnionAttributes.Add( unionAttribute );
                                insertedToVis = false;
                            }
                        }
                        if ( insertedToVis )
                            MoveCaretBasedOnLineBreakAfter( pair.Value, caret, maxHeightInCurrentRow );
                        insertedToVis = false;
                    }
                    caret.NextLine( labelHeight );
                    #endregion

                    #region PlusButton
                    caret.MoveHorizontally( 5 );

                    Button plusButton = openCheckedListBoxPlusButtonEvent_Pool.GetOrAdd( ( Button newButton ) =>
                    {
                        newButton.Click += new EventHandler( OpenCheckedListBoxPlusButton_ButtonClickEvent );
                    } );

                    Bitmap icon = new Bitmap( ProgramPermanentSettings.AssetsPath + @"Icons\iconoir\plus\plus32.png" );
                    plusButton.Image = icon;
                    plusButton.Text = string.Empty;
                    plusButton.Bounds = new Rectangle( caret.x, caret.y, genericHeightBasedOnFontUsed, genericHeightBasedOnFontUsed ); //maybe use a fixed size?
                    ((PooledControlTagInfo)plusButton.Tag).RelatedUnionElement = currentUnionNode;

                    controls.Add( plusButton );
                    caret.MoveHorizontally( genericHeightBasedOnFontUsed );

                    checkedListBoxPlusButton.Visible = false; //needs to be pooled

                    CheckedListBoxTagData tagData;
                    tagData.UNode = currentUnionNode;
                    tagData.MetaAttributes = metaAttributes;
                    tagData.Vis = this;
                    tagData.Node = node;
                    checkedListBoxPlusButton.Tag = tagData;

                    controls.Add( checkedListBoxPlusButton );
                    #endregion

                    #region SubNodesVis
                    caret.SetFixedMinX( 20 );
                    caret.NextLine( (int)(genericHeightBasedOnFontUsed * 1.5f) );
                    MetadataNodeLayer? subNodeLayer = null;
                    foreach ( IEditedXmlNodeOrComment child in node.ChildNodes )
                    {
                        if ( child is EditedXmlNode childNode )
                        {
                            Label label = labelPool.GetOrAdd( null );
                            string toWrite = "Sub Node: " + childNode.XmlNodeTagName;

                            label.Font = new Font( label.Font, FontStyle.Bold );
                            SizeF size = graphics.MeasureString( toWrite, label.Font );

                            label.Height = (int)Math.Ceiling( size.Height );
                            label.Width = (int)Math.Ceiling( size.Width );
                            label.Bounds = new Rectangle( caret.x, caret.y, label.Width + 5, label.Height );
                            label.Text = toWrite;
                            controls.Add( label );
                            caret.NextLine( labelHeight );

                            // get subNode name (actual tag, not the attribute "name") and use that as key
                            if ( childNode.XmlNodeTagName == string.Empty || !metaNodeLayer.SubNodes.TryGetValue( childNode.XmlNodeTagName, out subNodeLayer ) )
                                ArcenDebugging.LogSingleLine( "Sub Node's XmlNodeTagName wasn't found.", Verbosity.DoNotShow );
                        }
                        if ( subNodeLayer == null )
                        {
                            ArcenDebugging.LogSingleLine( "SUB NODE LAYER NULL", Verbosity.DoNotShow );
                        }
                        this.VisualizeSelectedNode( child, subNodeLayer, parentUnionNode: currentUnionNode, caret: caret );
                    }
                    #endregion
                }
            }
            if ( forceClearVis )
                MessagePumpOverriding.ResumeDrawing( MainWindow.Instance.RightSplitContainer.Panel2 );
        }

        #region NameLabel
        private static int PrintLabelToVis( Control.ControlCollection controls, KeyValuePair<string, MetaAttribute_Base> pair, EditedXmlNode node,
                                            EditedXmlAttribute? xmlAttribute, UnionAttribute uAttribute, Graphics graphics, Caret caret )
        {
            Label label = labelPool.GetOrAdd( ( Label newLabel ) =>
            {
                newLabel.Click += new EventHandler( OpenSmallMenuOnLabelRightClick );
                newLabel.DoubleClick += new EventHandler( OpenSmallMenuOnLabelDoubleClick );
            } );
            SizeF size = graphics.MeasureString( pair.Key, MainWindow.Instance.RightSplitContainer.Panel2.Font );
            label.Height = (int)Math.Ceiling( size.Height );
            label.Width = (int)Math.Ceiling( size.Width );

            MoveCaretBasedOnLineBreakBefore( pair.Value, caret, label.Width + 3, maxHeightInCurrentRow );

            label.Bounds = new Rectangle( caret.x, caret.y, label.Width + 5, label.Height );
            label.Text = pair.Key;

            controls.Add( label );
            node.CurrentViewControl = label;
            if ( xmlAttribute != null )
            {
                xmlAttribute.CurrentViewControl_Name = label;
                uAttribute.XmlAttribute = xmlAttribute;
            }
            uAttribute.Controls.Add( label );

            caret.MoveHorizontally( label.Width + 3 );
            return label.Height;
        }
        #endregion

        private void VisElementByType( Control.ControlCollection controls, MetadataDocument currentMetaDoc, MetaAttribute_Base metaAttribute, EditedXmlAttribute? xmlAttribute,
                                       UnionNode currentUnionNode, UnionAttribute uAttribute, Graphics graphics, Caret caret, int controlHeight )
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
                        boxBool.Bounds = new Rectangle( caret.x, caret.y, controlHeight, controlHeight );
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
                        GetHeightAndMoveCaretRight( boxBool, caret, metaAttribute.ContentWidthPx );
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
                        textBox.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_String)metaAttribute).ContentWidthPx, controlHeight );
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
                        GetHeightAndMoveCaretRight( textBox, caret, metaAttribute.ContentWidthPx );
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
                        textBox.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_StringMultiline)metaAttribute).ContentWidthPx, controlHeight );
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
                        GetHeightAndMoveCaretRight( textBox, caret, metaAttribute.ContentWidthPx );
                    }
                    break;
                #endregion

                #region ArbitraryString
                case AttributeType.ArbitraryString:
                    {
                        ComboBox comboBox = comboBoxPool.GetOrAdd( ( ComboBox newComboBox ) =>
                        {
                            newComboBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            newComboBox.SelectedIndexChanged += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        comboBox.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_ArbitraryString)metaAttribute).ContentWidthPx, controlHeight );
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                        comboBox.Items.Add( "Empty (To deselect any choice)" );

                        foreach ( string option in ((MetaAttribute_ArbitraryString)metaAttribute).Options )
                            comboBox.Items.Add( option );

                        if ( xmlAttribute != null )
                        {
                            comboBox.SelectedIndex = comboBox.FindStringExact( xmlAttribute.GetEffectiveValue() );
                            LinkDataAndExecuteCommonActions( controls, comboBox, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                        }
                        else
                        {
                            comboBox.Items.Add( ((MetaAttribute_ArbitraryString)metaAttribute).Default );
                            comboBox.SelectedIndex = 1;
                            LinkDataAndExecuteCommonActions( controls, comboBox, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        GetHeightAndMoveCaretRight( comboBox, caret, metaAttribute.ContentWidthPx );
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
                        numeric.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Int)metaAttribute).ContentWidthPx, controlHeight );
                        numeric.ThousandsSeparator = true;
                        numeric.DecimalPlaces = 0;
                        numeric.Maximum = Convert.ToDecimal( ((MetaAttribute_Int)metaAttribute).Max );
                        numeric.Minimum = Convert.ToDecimal( ((MetaAttribute_Int)metaAttribute).Min );

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
                        GetHeightAndMoveCaretRight( numeric, caret, metaAttribute.ContentWidthPx );
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
                        numeric.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Float)metaAttribute).ContentWidthPx, controlHeight ); // width set by meta

                        numeric.ThousandsSeparator = true;
                        numeric.DecimalPlaces = ((MetaAttribute_Float)metaAttribute).Precision;
                        if ( ((MetaAttribute_Float)metaAttribute).Min < (float)decimal.MinValue )
                            numeric.Minimum = decimal.MinValue;
                        if ( ((MetaAttribute_Float)metaAttribute).Max < (float)decimal.MaxValue )
                            numeric.Maximum = decimal.MaxValue;

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
                        GetHeightAndMoveCaretRight( numeric, caret, metaAttribute.ContentWidthPx );
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
                        comboBox.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_ArbitraryNode)metaAttribute).ContentWidthPx, controlHeight );
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                        comboBox.Items.Add( "Empty (To deselect any choice)" );

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
                            comboBox.SelectedIndex = 1;
                            LinkDataAndExecuteCommonActions( controls, comboBox, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        GetHeightAndMoveCaretRight( comboBox, caret, metaAttribute.ContentWidthPx );
                    }
                    break;
                #endregion

                #region NodeList
                case AttributeType.NodeList:
                    {
                        Button openListButton = openCheckedListBoxDropdownButtonEvent_Pool.GetOrAdd( (Action<Button>?)(( Button newButton ) =>
                        {
                            newButton.Click += new EventHandler( OpenCheckedListBoxDropdown_ButtonClickEvent );
                            newButton.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        }) );
                        openListButton.Bounds = new Rectangle( caret.x, caret.y, controlHeight, controlHeight );
                        Bitmap icon = new Bitmap( ProgramPermanentSettings.AssetsPath + @"Icons\iconoir\arrowDown\arrowDown18.png" );
                        openListButton.Image = icon;
                        openListButton.Text = string.Empty;

                        controls.Add( openListButton );
                        caret.MoveHorizontally( controlHeight );

                        checkedListBoxDropdown.Visible = false;
                        checkedListBoxDropdown.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_NodeList)metaAttribute).ContentWidthPx, controlHeight );

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
                        checkedListBoxDropdown.Height *= checkedListBoxDropdown.Items.Count;

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
                        GetHeightAndMoveCaretRight( openListButton, caret, metaAttribute.ContentWidthPx );
                    }
                    break;
                #endregion

                #region FolderList
                case AttributeType.FolderList:
                    {
                        Button openListButton = openCheckedListBoxDropdownButtonEvent_Pool.GetOrAdd( (Action<Button>?)(( Button newButton ) =>
                        {
                            newButton.Click += new EventHandler( OpenCheckedListBoxDropdown_ButtonClickEvent );
                            newButton.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        }) );
                        openListButton.Bounds = new Rectangle( caret.x, caret.y, controlHeight, controlHeight );
                        Bitmap icon = new Bitmap( ProgramPermanentSettings.AssetsPath + @"Icons\iconoir\arrowDown\arrowDown18.png" );
                        openListButton.Image = icon;
                        openListButton.Text = string.Empty;

                        controls.Add( openListButton );
                        caret.MoveHorizontally( controlHeight );

                        checkedListBoxDropdown.Visible = false;
                        checkedListBoxDropdown.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_FolderList)metaAttribute).ContentWidthPx, controlHeight );
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
                        GetHeightAndMoveCaretRight( openListButton, caret, metaAttribute.ContentWidthPx );
                    }
                    break;
                #endregion

                #region Point
                case AttributeType.Point:
                    {
                        NumericUpDown numeric1 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.x;
                        } );
                        NumericUpDown numeric2 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.y;
                        } );

                        numeric1.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Point)metaAttribute).ContentWidthPx, controlHeight );
                        caret.MoveHorizontally( ((MetaAttribute_Point)metaAttribute).ContentWidthPx + extraPixels );
                        numeric2.Bounds = new Rectangle( caret.x, caret.y, 80, controlHeight );

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
                        GetHeightAndMoveCaretRight( numeric1, caret, 0 );
                        GetHeightAndMoveCaretRight( numeric2, caret, metaAttribute.ContentWidthPx );
                    }
                    break;
                #endregion

                #region Vector2
                case AttributeType.Vector2:
                    {
                        NumericUpDown numeric1 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.x;
                        } );
                        NumericUpDown numeric2 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.y;
                        } );
                        numeric1.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx, controlHeight );
                        caret.MoveHorizontally( ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx + extraPixels );
                        numeric2.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx, controlHeight );

                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = ((MetaAttribute_Vector2)metaAttribute).x.Precision;
                        if ( ((MetaAttribute_Vector2)metaAttribute).x.Min < (float)decimal.MinValue )
                            numeric1.Minimum = decimal.MinValue;
                        if ( ((MetaAttribute_Vector2)metaAttribute).x.Max > (float)decimal.MaxValue )
                            numeric1.Maximum = decimal.MaxValue;

                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = ((MetaAttribute_Vector2)metaAttribute).y.Precision;
                        if ( ((MetaAttribute_Vector2)metaAttribute).y.Min < (float)decimal.MinValue )
                            numeric2.Minimum = decimal.MinValue;
                        if ( ((MetaAttribute_Vector2)metaAttribute).y.Max > (float)decimal.MaxValue )
                            numeric2.Maximum = decimal.MaxValue;

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
                        GetHeightAndMoveCaretRight( numeric1, caret, 0 );
                        GetHeightAndMoveCaretRight( numeric2, caret, metaAttribute.ContentWidthPx );
                    }
                    break;
                #endregion

                #region Vector3
                case AttributeType.Vector3:
                    {
                        NumericUpDown numeric1 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.x;
                        } );
                        NumericUpDown numeric2 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.y;
                        } );
                        NumericUpDown numeric3 = numericUpDownPool.GetOrAdd( ( NumericUpDown newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.z;
                        } );

                        numeric1.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx, controlHeight );
                        caret.MoveHorizontally( ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx + extraPixels );
                        numeric2.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx, controlHeight );
                        caret.MoveHorizontally( ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx + extraPixels );
                        numeric3.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx, controlHeight );

                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).x.Precision;
                        if ( ((MetaAttribute_Vector3)metaAttribute).x.Min < (float)decimal.MinValue )
                            numeric1.Minimum = decimal.MinValue;
                        if ( ((MetaAttribute_Vector3)metaAttribute).x.Max > (float)decimal.MaxValue )
                            numeric1.Maximum = decimal.MaxValue;

                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).y.Precision;
                        if ( ((MetaAttribute_Vector3)metaAttribute).y.Min < (float)decimal.MinValue )
                            numeric2.Minimum = decimal.MinValue;
                        if ( ((MetaAttribute_Vector3)metaAttribute).y.Max > (float)decimal.MaxValue )
                            numeric2.Maximum = decimal.MaxValue;

                        numeric3.ThousandsSeparator = true;
                        numeric3.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).z.Precision;
                        if ( ((MetaAttribute_Vector3)metaAttribute).z.Min < (float)decimal.MinValue )
                            numeric3.Minimum = decimal.MinValue;
                        if ( ((MetaAttribute_Vector3)metaAttribute).z.Max > (float)decimal.MaxValue )
                            numeric3.Maximum = decimal.MaxValue;

                        Control[] tempControls = new Control[3];
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
                        GetHeightAndMoveCaretRight( numeric1, caret, 0 );
                        GetHeightAndMoveCaretRight( numeric2, caret, 0 );
                        GetHeightAndMoveCaretRight( numeric3, caret, metaAttribute.ContentWidthPx );
                    }
                    break;
                #endregion

                default:
                    ArcenDebugging.LogSingleLine( $"ERROR: Unknown type {metaAttribute.Type} in Metadata attribute key: {metaAttribute.Key}", Verbosity.DoNotShow );
                    break;
            }
        }

        private static void GetHeightAndMoveCaretRight( Control control, Caret caret, int width )
        {
            if ( control.Height > maxHeightInCurrentRow )
                maxHeightInCurrentRow = control.Height;
            caret.MoveHorizontally( width + extraPixels );
        }

        #region Linking Data in UnionNode
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
            ControlTagInfo tagData = (ControlTagInfo)control.Tag;
            tagData.RelatedUnionElement = uAttribute;

            uAttribute.Controls.Add( control );

            if ( addToUNodeListOfAttributes )
            {
                uAttribute.MetaAttribute = KeyValuePair.Create( metaAttribute.Key, metaAttribute );
                uAttribute.XmlAttribute = xmlAttribute; // duplicate assignment - done in PrintLabelToVis()
                currentUNode.UnionAttributes.Add( uAttribute );
            }

            controls.Add( control );

            if ( xmlAttribute != null )
            {
                xmlAttribute.CurrentViewControl_Value = control;
            }
            tagData.ClearErrorProvider( control );
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
        private static bool justInsertedLineBreak = false; //can't be static for multiple XmlVis
        private static void MoveCaretBasedOnLineBreakBefore( MetaAttribute_Base metaAttribute, Caret caret, int upcomingCaretHorizonatalMove, int height )
        {
            int widthMultiplier = 1;
            if ( metaAttribute.Type == AttributeType.Point || metaAttribute.Type == AttributeType.Vector2 )
                widthMultiplier = 2;
            if ( metaAttribute.Type == AttributeType.Vector3 )
                widthMultiplier = 3;

            switch ( metaAttribute.LinebreakBefore )
            {
                case LineBreakType.Always:
                    if ( !justInsertedLineBreak )
                    {
                        caret.NextLine( height + extraPixels );
                        justInsertedLineBreak = true;
                    }
                    else
                        justInsertedLineBreak = false;
                    break;
                case LineBreakType.PreferNot:
                    if ( MainWindow.Instance.RightSplitContainer.Panel2.ClientSize.Width - (caret.x + (upcomingCaretHorizonatalMove * widthMultiplier) + (metaAttribute.ContentWidthPx * widthMultiplier)) < 5 )
                    {
                        caret.NextLine( height + extraPixels );
                        justInsertedLineBreak = true;
                    }
                    else
                        justInsertedLineBreak = false;
                    break;
            }
        }

        private static void MoveCaretBasedOnLineBreakAfter( MetaAttribute_Base metaAttribute, Caret caret, int height )
        {
            switch ( metaAttribute.LinebreakAfter )
            {
                case LineBreakType.Always:
                    caret.NextLine( height + extraPixels );
                    justInsertedLineBreak = true;
                    break;
                case LineBreakType.PreferNot:
                    if ( MainWindow.Instance.RightSplitContainer.Panel2.ClientSize.Width - caret.x < 20 ) // might remove this check?
                    {
                        caret.NextLine( height + extraPixels );
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

        //#region CalculateBounds
        //private static Rectangle CalculateBounds( string text, Graphics graphics )
        //{
        //    SizeF size = graphics.MeasureString( text, MainWindow.Instance.RightSplitContainer.Panel2.Font );
        //    return new Rectangle( caret.x, Caret.y, (int)Math.Ceiling( size.Width ), (int)Math.Ceiling( size.Height ) );
        //}
        //private static Rectangle CalculateBounds( string text, int width, Graphics graphics )
        //{
        //    SizeF size = graphics.MeasureString( text, MainWindow.Instance.RightSplitContainer.Panel2.Font, width );
        //    return new Rectangle( Caret.x, Caret.y, (int)Math.Ceiling( size.Width ), (int)Math.Ceiling( size.Height ) );
        //}
        //#endregion

        #region Events

        #region CheckedBoxLists
        private void OpenCheckedListBoxDropdown_ButtonClickEvent( object? sender, EventArgs e )
        {
            checkedListBoxDropdown.Visible = true;
            checkedListBoxDropdown.Focus();
        }

        private static void CloseCheckedBoxListDropdown_CLBLeaveEvent( object? sender, EventArgs e ) => checkedListBoxDropdown.Visible = false;

        private static void OpenCheckedListBoxPlusButton_ButtonClickEvent( object? sender, EventArgs e )
        {
            // get all missing attributes from parent union node and populate checkedListBoxPlusButton from that (check if there are any controls on the att to understand if it's in vis or not)
            Button? button = sender as Button;
            if ( button == null )
            {
                ArcenDebugging.LogSingleLine( "Button in OpenCheckedListBoxPlusButton_ButtonClickEvent null!", Verbosity.DoNotShow );
                return;
            }
            UnionNode? uNode = ((PooledControlTagInfo)button.Tag).RelatedUnionElement as UnionNode;
            if ( uNode == null )
            {
                ArcenDebugging.LogSingleLine( "uNode in OpenCheckedListBoxPlusButton_ButtonClickEvent null!", Verbosity.DoNotShow );
                return;
            }
            checkedListBoxPlusButton.Items.Clear();
            foreach ( UnionAttribute uAtt in uNode.UnionAttributes )
                if ( uAtt.Controls.Count == 0 )
                    checkedListBoxPlusButton.Items.Add( uAtt.MetaAttribute.Key );

            CheckedListBoxTagData tagData = new CheckedListBoxTagData();
            if ( uNode.XmlNodeOrComment != null )
                tagData.Node = (EditedXmlNode)uNode.XmlNodeOrComment;
            else
                ArcenDebugging.LogSingleLine( "uNode.XmlNodeOrComment in OpenCheckedListBoxPlusButton_ButtonClickEvent is null", Verbosity.DoNotShow );

            tagData.UNode = uNode;
            tagData.MetaAttributes = uNode.MetaLayer.AttributesData;
            tagData.Vis = new XmlVisualizer();
            checkedListBoxPlusButton.Tag = tagData;

            checkedListBoxPlusButton.Bounds = new Rectangle( button.Bounds.X + button.Bounds.Width, button.Bounds.Y + button.Bounds.Height,
                                                 300, genericHeightBasedOnFontUsed * amountOfValuesToDisplayInCLB );
            checkedListBoxPlusButton.Height = genericHeightBasedOnFontUsed * amountOfValuesToDisplayInCLB;
            checkedListBoxPlusButton.Visible = true;
            checkedListBoxPlusButton.BringToFront();
            checkedListBoxPlusButton.Focus();
        }

        private static void CloseCheckedListBoxPlusButton_CLBLostFocusEvent( object? sender, EventArgs e ) //todo: test
        {
            CheckedListBox? checkedListBox = sender as CheckedListBox;
            if ( checkedListBox == null )
            {
                ArcenDebugging.LogSingleLine( "checkedListBox in CloseCheckedListBoxPlusButton_CBLLeaveEvent is null!", Verbosity.DoNotShow );
                return;
            }
            CheckedListBoxTagData tagData = (CheckedListBoxTagData)checkedListBox.Tag;

            foreach ( string attName in checkedListBoxPlusButton.CheckedItems )
            {
                EditedXmlAttribute att = new EditedXmlAttribute();
                att.Name = attName;
                // Need the default value which is type dependant
                switch ( tagData.MetaAttributes[attName].Type )
                {
                    case AttributeType.Bool:
                        att.TempValue = ((MetaAttribute_Bool)tagData.MetaAttributes[attName]).Default.ToString();
                        break;
                    case AttributeType.BoolInt:
                        att.TempValue = ((MetaAttribute_BoolInt)tagData.MetaAttributes[attName]).Default.ToString();
                        break;
                    case AttributeType.String:
                        att.TempValue = ((MetaAttribute_String)tagData.MetaAttributes[attName]).Default;
                        break;
                    case AttributeType.StringMultiLine:
                        att.TempValue = ((MetaAttribute_StringMultiline)tagData.MetaAttributes[attName]).Default;
                        break;
                    case AttributeType.ArbitraryString:
                        att.TempValue = ((MetaAttribute_ArbitraryString)tagData.MetaAttributes[attName]).Default;
                        break;
                    case AttributeType.Int:
                        att.TempValue = ((MetaAttribute_Int)tagData.MetaAttributes[attName]).Default.ToString();
                        break;
                    case AttributeType.Float:
                        att.TempValue = ((MetaAttribute_Float)tagData.MetaAttributes[attName]).Default.ToString();
                        break;
                    case AttributeType.ArbitraryNode:
                        att.TempValue = ((MetaAttribute_ArbitraryNode)tagData.MetaAttributes[attName]).Default;
                        break;
                    case AttributeType.NodeList:
                        foreach ( string nodeForList in ((MetaAttribute_NodeList)tagData.MetaAttributes[attName]).Defaults )
                            att.TempValue = nodeForList + ",";
                        att.TempValue = att.TempValue?.Remove( att.TempValue.Length - 1 );
                        break;
                    case AttributeType.FolderList:
                        foreach ( string folderForList in ((MetaAttribute_FolderList)tagData.MetaAttributes[attName]).Defaults )
                            att.TempValue = folderForList + ",";
                        att.TempValue = att.TempValue?.Remove( att.TempValue.Length - 1 );
                        break;
                    case AttributeType.Point:
                        att.TempValue = ((MetaAttribute_Point)tagData.MetaAttributes[attName]).x.Default.ToString();
                        break;
                    case AttributeType.Vector2:
                        att.TempValue = ((MetaAttribute_Vector2)tagData.MetaAttributes[attName]).x.Default.ToString();
                        break;
                    case AttributeType.Vector3:
                        att.TempValue = ((MetaAttribute_Vector3)tagData.MetaAttributes[attName]).x.Default.ToString();
                        break;
                }
                tagData.Node.Attributes.Add( attName, att );
            }
            checkedListBoxPlusButton.Visible = false;
            UnionNode? uNode = tagData.UNode;
            while ( uNode.ParentUnionNode != null )
                uNode = uNode.ParentUnionNode;
            if ( uNode.XmlNodeOrComment != null )
                tagData.Vis.VisualizeSelectedNode( uNode.XmlNodeOrComment, uNode.MetaDocument.TopLevelNode, true ); //topLevelNode correct?
            else
                ArcenDebugging.LogSingleLine( "uNode.XmlNodeOrComment null in CloseCheckedListBoxPlusButton_CBLLeaveEvent", Verbosity.DoNotShow );
        }
        #endregion

        #region SmallLabelToolBar
        private static void OpenSmallMenuOnLabelRightClick( object? sender, EventArgs e )
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if ( sender != null && me.Button == MouseButtons.Right )
            {
                LabelMenu labelMenu = new LabelMenu( (Control)sender );
                labelMenu.Show();
            }
        }

        private static void OpenSmallMenuOnLabelDoubleClick( object? sender, EventArgs e )
        {
            MouseEventArgs me = (MouseEventArgs)e;
            SplitContainer container = MainWindow.Instance.RightSplitContainer;
            if ( sender != null && me.Button == MouseButtons.Left )
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
            if ( control is ComboBox comboBox && comboBox.SelectedIndex == 0 )
                comboBox.SelectedIndex = -1;

            XmlValidator.Validate( control );
        }
        #endregion

        #region XmlValidator
        private static class XmlValidator
        {
            //private static readonly PoolWithReference<ErrorProvider> errorProviderPool = new PoolWithReference<ErrorProvider>();
            //private static void ClearAndReturnToPool()
            //{
            //    //todo: clear?

            //    errorProviderPool.ReturnAllToPool();
            //}

            public static void Validate( Control control )
            {
                double timeToWaitBeforeValidating = 0.4f;
                if ( timeOfVisStart == DateTime.UnixEpoch || (DateTime.Now - timeOfVisStart).TotalSeconds < timeToWaitBeforeValidating )
                    return;

                //ClearAndReturnToPool();

                ErrorProvider errorProvider = ((ControlTagInfo)control.Tag).RelatedErrorProvider;
                errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
                errorProvider.SetIconAlignment( control, ErrorIconAlignment.TopRight );

                // how to recognize which control is the sender object? use ControlTagInfo
                PooledControlTagInfo tagData = (PooledControlTagInfo)control.Tag;
                if ( tagData.RelatedUnionElement == null )
                {
                    ArcenDebugging.LogSingleLine( $"RelatedUnionElement stored in Control {control.GetType()}'s Tag Data is null! Can't do validation!", Verbosity.DoNotShow );
                    return;
                }

                UnionAttribute uAtt = (UnionAttribute)tagData.RelatedUnionElement;
                if ( uAtt.XmlAttribute == null )
                {
                    ArcenDebugging.LogSingleLine( $"Edited Attribute with key {uAtt.MetaAttribute.Key} of Type {uAtt.MetaAttribute.Value.Type} stored in Control {control.GetType()}'s Tag Data is null! Can't do validation!", Verbosity.DoNotShow );
                    return;
                }

                SetEditedAttributeTempValue( control, uAtt );

                switch ( uAtt.XmlAttribute.Type )
                {
                    #region All Cases
                    case AttributeType.Bool:
                        {
                            string error = ((MetaAttribute_Bool)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_BoolInt)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_String)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_StringMultiline)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_ArbitraryString)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_Int)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_Float)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_ArbitraryNode)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_NodeList)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_FolderList)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_Point)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_Vector2)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                            string error = ((MetaAttribute_Vector3)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
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
                uAtt.XmlAttribute.Type = uAtt.MetaAttribute.Value.Type;

                string? textOrValueFromControl = null;
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
                if ( textOrValueFromControl != null )
                    uAtt.XmlAttribute.TempValue = textOrValueFromControl;
            }

            private static void ClearEditedAttributeTempValue( UnionAttribute uAtt )
            {
                if ( uAtt.XmlAttribute != null )
                {
                    uAtt.XmlAttribute.Name = string.Empty;
                    uAtt.XmlAttribute.Type = AttributeType.Unknown;
                    uAtt.XmlAttribute.TempValue = null;
                }
            }
        }
        #endregion
    }

    public struct CheckedListBoxTagData
    {
        public UnionNode UNode;
        public Dictionary<string, MetaAttribute_Base> MetaAttributes;
        public EditedXmlNode Node; // Updated each time the plus button is pressed to match the node from which the CLB was called
        public XmlVisualizer Vis;
    }
}