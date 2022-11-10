using ArcenXE.Universal;
using ArcenXE.Utilities;
using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Visualization.Utilities;
using CheckBox = System.Windows.Forms.CheckBox;
using System.Diagnostics;

namespace ArcenXE.Visualization
{
    public class XmlVisualizer
    {
        private readonly static List<Control> controlsToAdd = new List<Control>(); // remove and revert to single add for controls?

        private readonly static SuperBasicPool<Label> genericLabelPool = new SuperBasicPool<Label>();
        private readonly static SuperBasicPool<Label> attributeLabelPool = new SuperBasicPool<Label>();
        private readonly static SuperBasicPool<TextBox> textBoxPool = new SuperBasicPool<TextBox>();
        private readonly static SuperBasicPool<ComboBox> comboBoxPool = new SuperBasicPool<ComboBox>();
        private readonly static SuperBasicPool<CheckBox> checkBoxPool = new SuperBasicPool<CheckBox>();
        private readonly static SuperBasicPool<NumericUpDown> numericUpDownPool = new SuperBasicPool<NumericUpDown>();
        private readonly static SuperBasicPool<NumericUpDown> subNodesNumericPool = new SuperBasicPool<NumericUpDown>();
        private readonly static SuperBasicPool<Button> openOrDoCLBButtonEvent_Pool = new SuperBasicPool<Button>();
        private readonly static SuperBasicPool<Button> addRequestedSubNodesPlusButtonEvent_Pool = new SuperBasicPool<Button>();
        private readonly static SuperBasicPool<Button> openCheckedListBoxDropdownButtonEvent_Pool = new SuperBasicPool<Button>();

        private readonly static CheckedListBox checkedListBoxDropdown;
        private readonly static CheckedListBox checkedListBoxPlusButton;

        private readonly static Dictionary<string, NumericUpDown> subNodesNumericTempHold = new Dictionary<string, NumericUpDown>();

        private readonly Bitmap plusIcon = new Bitmap( ProgramPermanentSettings.AssetsPath + @"Icons\iconoir\plus\plus32.png" );
        private readonly Bitmap addSubNodesPlusIcon = new Bitmap( ProgramPermanentSettings.AssetsPath + @"Icons\iconoir\plus\plus32Blue.png" );

        private static Button? lastAttributeButtonClicked;

        private readonly static ToolTip toolTip = new ToolTip();

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

            lastAttributeButtonClicked = null; //todo

            toolTip.AutoPopDelay = 0;
            toolTip.InitialDelay = 0;
            toolTip.ReshowDelay = 0;
        }

        private const int EXTRA_PIXELS_W = 18; // should use the genericHeight calculated for the used font
        private const int EXTRA_PIXELS_H = 8;

        private const int AMOUNT_OF_LINES_TO_DISPLAY_IN_CLB = 7;
        private const int TEXTBOX_HEIGHT = 22;

        private static int maxHeightInCurrentRow = 0;
        private static int genericHeightBasedOnFontUsed;
        private static DateTime timeOfVisStart = DateTime.UnixEpoch;

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
            Control.ControlCollection controls = MainWindow.Instance.VisControls.Controls;

            foreach ( Control control in controls )
            {
                PooledControlTagInfo? pooledControlTagData = control.Tag as PooledControlTagInfo;
                if ( pooledControlTagData == null )
                    continue;

                switch ( control ) // move upper if add different ControlTags
                {
                    case Label label when control is Label:
                        label.Font = MainWindow.Instance.VisControls.Font; //set to default; to be moved in separate SetToDefaults() method
                        label.Text = string.Empty;
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
            //controlsToAdd.Clear();
            checkedListBoxDropdown.Items.Clear();
            checkedListBoxPlusButton.Items.Clear();
            subNodesNumericTempHold.Clear();
            lastAttributeButtonClicked = null;
        }
        #endregion

        public void OuterVisualizeSelectedNode( IEditedXmlNodeOrComment editedXmlNodeOrComment, MetadataNodeLayer? metaNodeLayer, bool forceClearVis = false, UnionNode? parentUnionNode = null, Caret? caret = null )
        {
            //Stopwatch sw = Stopwatch.StartNew();
            timeOfVisStart = DateTime.Now;
            if ( forceClearVis )
            {
                MessagePumpOverriding.SuspendDrawing( MainWindow.Instance.VisControls );
                MainWindow.Instance.SuspendLayout();
                MainWindow.Instance.VisControls.SuspendLayout();
                this.ReturnAllToPoolAndReset( caret );
            }
            Graphics graphics = MainWindow.Instance.VisControls.CreateGraphics();

            using ( graphics )
            {
                //Stopwatch sw1 = Stopwatch.StartNew();
                VisualizeSelectedNode( editedXmlNodeOrComment, metaNodeLayer, graphics, forceClearVis, parentUnionNode, caret );
                //ArcenDebugging.LogSingleLine( $"VisNode sw = {sw1.ElapsedMilliseconds}", Verbosity.DoNotShow );
                //sw1.Reset();
            }
            if ( forceClearVis )
            {
                MessagePumpOverriding.ResumeDrawing( MainWindow.Instance.VisControls );
                MainWindow.Instance.ResumeLayout();
                MainWindow.Instance.VisControls.ResumeLayout();
            }
            //sw.Stop();
            //ArcenDebugging.LogSingleLine( $"Outer sw = {sw.ElapsedMilliseconds}", Verbosity.DoNotShow );
        }


        public void VisualizeSelectedNode( IEditedXmlNodeOrComment editedXmlNodeOrComment, MetadataNodeLayer? metaNodeLayer, Graphics graphics, bool forceClearVis = false, UnionNode? parentUnionNode = null, Caret? caret = null )
        {
            //ArcenDebugging.StartStopwatch();
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
            Control.ControlCollection controls = MainWindow.Instance.VisControls.Controls;
            UnionNode currentUnionNode = new UnionNode( metaNodeLayer );

            if ( parentUnionNode != null )
            {
                currentUnionNode.ParentUnionNode = parentUnionNode;
                parentUnionNode.UnionSubNodes.Add( currentUnionNode );
            }
            else
                currentMetaDoc.RelatedTopUnionNode ??= currentUnionNode;

            metaNodeLayer.RelatedUnionNode = currentUnionNode;
            //ArcenDebugging.LogSingleLine( "First init: " + ArcenDebugging.GetStopwatchMilliseconds() + "ms", Verbosity.DoNotShow );
            //ArcenDebugging.ResetStopwatch();
            //ArcenDebugging.StartStopwatch();

            int labelHeight = 0;
            SizeF generalSize = graphics.MeasureString( "A1BCDEtest0", MainWindow.Instance.VisControls.Font );
            genericHeightBasedOnFontUsed = Math.Max( (int)Math.Ceiling( generalSize.Height ), TEXTBOX_HEIGHT );
            //ArcenDebugging.LogSingleLine( "MeasureString and 2 Math func: " + ArcenDebugging.GetStopwatchMilliseconds() + "ms", Verbosity.DoNotShow ); // <1ms
            //ArcenDebugging.ResetStopwatch();

            //ArcenDebugging.StartStopwatch();
            if ( item is EditedXmlComment comment )
            {
                #region Comment
                TextBox textBox = textBoxPool.GetOrAdd( ( newTextBox ) =>
                {
                    newTextBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                } );
                //SizeF size = graphics.MeasureString( comment.Data, MainWindow.Instance.visControls.Font );

                textBox.Height = genericHeightBasedOnFontUsed;
                textBox.Width = 80;
                textBox.Bounds = new Rectangle( caret.x, caret.y, textBox.Width + 5, textBox.Height );
                textBox.Text = comment.Data;

                comment.CurrentViewControl = textBox;

                currentUnionNode.XmlNodeOrComment = comment;
                comment.RelatedUnionNode = currentUnionNode;
                currentUnionNode.Controls.Add( textBox );
                ((PooledControlTagInfo)textBox.Tag).RelatedUnionElement = currentUnionNode;

                controls.Add( textBox );

                caret.NextLine( textBox.Height + 5 );
                #endregion
                //ArcenDebugging.LogSingleLine( "Comment: " + ArcenDebugging.GetStopwatchMilliseconds() + "ms", Verbosity.DoNotShow );
                //ArcenDebugging.ResetStopwatch();
            }
            else if ( item is EditedXmlNode node ) // loop over metadata
            {
                #region TopNode
                if ( node.NodeCentralID != null ) // top node 
                {
                    Label label = genericLabelPool.GetOrAdd( null );
                    string toWrite = "Top Node type selected: " + node.XmlNodeTagName;

                    label.Font = new Font( MainWindow.Instance.Font, FontStyle.Bold );
                    SizeF size = graphics.MeasureString( toWrite, MainWindow.Instance.Font );

                    label.Height = genericHeightBasedOnFontUsed;
                    label.Width = (int)Math.Ceiling( size.Width );
                    label.Bounds = new Rectangle( caret.x, caret.y, label.Width + 5, label.Height );
                    label.Text = toWrite;

                    node.CurrentViewControl = label;

                    currentUnionNode.XmlNodeOrComment = node;
                    node.RelatedUnionNode = currentUnionNode;
                    currentUnionNode.Controls.Add( label );
                    string? s = node.NodeCentralID.GetEffectiveValue();
                    if ( s != null )
                        currentUnionNode.TopNodeData = new UnionTopNodeAttribute( s, node.NodeCentralID );
                    ((PooledControlTagInfo)label.Tag).RelatedUnionElement = currentUnionNode;
                    //Stopwatch sw = Stopwatch.StartNew();
                    controls.Add( label );
                    //ArcenDebugging.LogSingleLine( "TopNode: " + sw.ElapsedMilliseconds + "ms", Verbosity.DoNotShow );
                    //sw.Stop();
                    caret.NextLine( label.Height + 2 );
                }
                #endregion

                #region Attributes
                bool insertedToVis = false;
                //Stopwatch sw = Stopwatch.StartNew();
                //sw.Reset();
                foreach ( KeyValuePair<string, MetaAttribute_Base> metaAttributePair in metaAttributes ) // read from MetaDoc and lookup in xmldata
                {
                    //sw.Start();
                    // 1 MetaAttribute describes 1 EditedXmlAttribute
                    UnionAttribute unionAttribute = new UnionAttribute( metaAttributePair );
                    if ( node.Attributes.TryGetValue( metaAttributePair.Value.Key, out EditedXmlAttribute? xmlAttribute ) && !xmlAttribute.IsDeleted )
                    {
                        //XML Attribute value to be printed in VisElementByType
                        labelHeight = PrintLabelToVis( controls, metaAttributePair, node, xmlAttribute, unionAttribute, graphics, caret );
                        VisElementByType( controls, currentMetaDoc, metaAttributePair.Value, xmlAttribute, currentUnionNode, unionAttribute, graphics, caret, labelHeight );
                        ExecuteToUndoStack( metaAttributePair.Value );
                        insertedToVis = true;
                    }
                    else if ( metaAttributePair.Value.IsRequired )
                    {
                        // add the empty field on Vis
                        labelHeight = PrintLabelToVis( controls, metaAttributePair, node, null, unionAttribute, graphics, caret );
                        VisElementByType( controls, currentMetaDoc, metaAttributePair.Value, null, currentUnionNode, unionAttribute, graphics, caret, labelHeight );
                        ExecuteToUndoStack( metaAttributePair.Value );
                        insertedToVis = true;
                    }
                    else
                    {
                        // will be listed at the bottom when pressing the PLUS button
                        unionAttribute.MetaAttribute = metaAttributePair;
                        insertedToVis = false;
                    }
                    currentUnionNode.UnionAttributes.Add( unionAttribute );
                    if ( insertedToVis )
                        MoveCaretBasedOnLineBreakAfter( metaAttributePair.Value, caret, maxHeightInCurrentRow );
                    //ArcenDebugging.LogSingleLine( (insertedToVis ? "T" : "F") + $" MetaAttribute \"{metaAttributePair.Key}\" loop: " + sw.ElapsedMilliseconds + "ms", Verbosity.DoNotShow );
                    //ArcenDebugging.LogSingleLine( (insertedToVis ? "T" : "F") + $" MetaAttribute \"{metaAttributePair.Key}\" loop: " + (1000 * (double)sw.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
                    //sw.Reset();
                    insertedToVis = false;
                }
                #endregion

                caret.NextLine( labelHeight + EXTRA_PIXELS_H );

                #region AttributesPlusButton
                caret.MoveHorizontally( 5 );

                Button attributesPlusButton = openOrDoCLBButtonEvent_Pool.GetOrAdd( ( newButton ) =>
                {
                    newButton.Click += new EventHandler( OpenOrDoCLB_ButtonClickEvent );
                } );
                attributesPlusButton.Font = MainWindow.Instance.VisControls.Font;
                attributesPlusButton.Text = "Add Attributes (" + checkedListBoxPlusButton.Items.Count + " Avail)";
                attributesPlusButton.Bounds = new Rectangle( caret.x, caret.y, attributesPlusButton.Text.Length * 6, genericHeightBasedOnFontUsed );
                ((PooledControlTagInfo)attributesPlusButton.Tag).RelatedUnionElement = currentUnionNode;
                controls.Add( attributesPlusButton );
                caret.MoveHorizontally( genericHeightBasedOnFontUsed );

                checkedListBoxPlusButton.Visible = false; //needs to be pooled
                CheckedListBoxTagData tagData = new CheckedListBoxTagData( currentUnionNode, metaAttributes, node, this );
                checkedListBoxPlusButton.Tag = tagData;

                controls.Add( checkedListBoxPlusButton );
                #endregion

                #region SubNodesVis
                caret.SetFixedMinX( 20 );
                caret.NextLine( (int)(genericHeightBasedOnFontUsed * 1.5f) );
                {
                    MetadataNodeLayer? subNodeLayer = null;
                    foreach ( IEditedXmlNodeOrComment child in node.ChildNodes )
                    {
                        if ( child is EditedXmlNode childNode )
                        {
                            Label label = genericLabelPool.GetOrAdd( null );
                            string toWrite = "Sub Node: " + childNode.XmlNodeTagName;

                            label.Font = new Font( label.Font, FontStyle.Bold );
                            //SizeF size = graphics.MeasureString( toWrite, label.Font );

                            label.Height = genericHeightBasedOnFontUsed;
                            label.Width = toWrite.Length * 7;
                            label.Bounds = new Rectangle( caret.x, caret.y, label.Width + 5, label.Height );
                            label.Text = toWrite;
                            node.CurrentViewControl = label;

                            currentUnionNode.XmlNodeOrComment = node;
                            node.RelatedUnionNode = currentUnionNode;
                            currentUnionNode.Controls.Add( label );
                            ((PooledControlTagInfo)label.Tag).RelatedUnionElement = currentUnionNode;

                            controls.Add( label );
                            caret.NextLine( labelHeight );

                            // get subNode name (actual tag, not the attribute "name") and use that as key
                            if ( childNode.XmlNodeTagName == string.Empty || !metaNodeLayer.SubNodes.TryGetValue( childNode.XmlNodeTagName, out subNodeLayer ) )
                                ArcenDebugging.LogSingleLine( "Sub Node's XmlNodeTagName wasn't found.", Verbosity.DoNotShow );
                        }
                        if ( subNodeLayer == null )
                            ArcenDebugging.LogSingleLine( "SUB NODE LAYER NULL", Verbosity.DoNotShow );
                        this.VisualizeSelectedNode( child, subNodeLayer, graphics, parentUnionNode: currentUnionNode, caret: caret ); // todo: check if this is the reason for the slowdown when loading subnodes in vis. Refactor so it can be called only once?
                    }
                }
                #endregion

                int extraWidth = 0;
                if ( /*!readOnlyVis && */metaNodeLayer.SubNodes.Count > 0 ) // if the metadata contains subnodes, give the possibility to add them
                {
                    #region SubtitleLabelAddSubNodes
                    {
                        Label addSubNodesLabel = genericLabelPool.GetOrAdd( null );
                        addSubNodesLabel.Text = "Add new sub nodes:";
                        SizeF size = graphics.MeasureString( addSubNodesLabel.Text, MainWindow.Instance.VisControls.Font );
                        addSubNodesLabel.Height = Math.Max( genericHeightBasedOnFontUsed, (int)Math.Ceiling( size.Height ) );
                        addSubNodesLabel.Width = (int)Math.Ceiling( size.Width );
                        addSubNodesLabel.TextAlign = ContentAlignment.MiddleLeft;
                        addSubNodesLabel.Bounds = new Rectangle( caret.x, caret.y, addSubNodesLabel.Width + 5, addSubNodesLabel.Height );
                        controls.Add( addSubNodesLabel );
                    }
                    #endregion

                    foreach ( KeyValuePair<string, MetadataNodeLayer> kv in metaNodeLayer.SubNodes )
                    {
                        caret.NextLine( genericHeightBasedOnFontUsed + EXTRA_PIXELS_H );

                        #region LabelWithSubNodeName
                        Label label = genericLabelPool.GetOrAdd( null );
                        //SizeF size = graphics.MeasureString( kv.Key, MainWindow.Instance.visControls.Font );
                        //label.Height = Math.Max( genericHeightBasedOnFontUsed, (int)Math.Ceiling( size.Height ) );
                        label.Height = genericHeightBasedOnFontUsed;
                        label.Width = kv.Key.Length * 7;
                        label.TextAlign = ContentAlignment.MiddleLeft;
                        label.Bounds = new Rectangle( caret.x, caret.y, label.Width + 5, label.Height );
                        label.Text = kv.Key;
                        ((PooledControlTagInfo)label.Tag).RelatedUnionElement = currentUnionNode;
                        controls.Add( label );
                        #endregion

                        caret.MoveHorizontally( label.Width + 3 );

                        #region SubNodeNumerical
                        NumericUpDown subNodeNumeric = subNodesNumericPool.GetOrAdd( null );
                        subNodeNumeric.Bounds = new Rectangle( caret.x, caret.y, genericHeightBasedOnFontUsed * 2, genericHeightBasedOnFontUsed );
                        subNodeNumeric.Minimum = 0;
                        subNodeNumeric.Maximum = 999;
                        subNodeNumeric.Value = 900;
                        subNodeNumeric.AutoSize = false;
                        subNodeNumeric.Value = 0;
                        subNodeNumeric.ThousandsSeparator = true;
                        subNodeNumeric.DecimalPlaces = 0;
                        ((PooledControlTagInfo)subNodeNumeric.Tag).RelatedUnionElement = currentUnionNode;
                        //UnionAttribute unionAttribute = new UnionAttribute( new KeyValuePair<string, MetaAttribute_Base>(kv.Key, kv.Value.AttributesData.TryGetValue() );
                        //LinkDataAndExecuteCommonActions( controls, subNodePlusButton, currentUnionNode, )
                        subNodesNumericTempHold.Add( kv.Key, subNodeNumeric );
                        controls.Add( subNodeNumeric );
                        #endregion

                        extraWidth = subNodeNumeric.Width;
                    }

                    caret.MoveHorizontally( extraWidth + EXTRA_PIXELS_W );

                    #region AddSubNodePlusButton
                    Button subNodePlusButton = addRequestedSubNodesPlusButtonEvent_Pool.GetOrAdd( ( newButton ) =>
                    {
                        newButton.Click += new EventHandler( AddNewSubNode_ButtonClick ); //todo event that creates a new subnode and adds it where necessary
                    } );

                    subNodePlusButton.Image = addSubNodesPlusIcon;
                    subNodePlusButton.Text = string.Empty;
                    subNodePlusButton.Bounds = new Rectangle( caret.x, caret.y, genericHeightBasedOnFontUsed, genericHeightBasedOnFontUsed ); //maybe use a fixed size?
                    ((PooledControlTagInfo)subNodePlusButton.Tag).RelatedUnionElement = currentUnionNode;
                    //UnionAttribute unionAttribute = new UnionAttribute( metaNodeLayer.SubNodes.TryGetValue( );
                    //LinkDataAndExecuteCommonActions( controls, subNodePlusButton, currentUnionNode, )
                    subNodePlusButton.Visible = true;
                    controls.Add( subNodePlusButton );
                    #endregion
                }
            }
            //controls.AddRange( controlsToAdd.ToArray() );
            ArcenDebugging.LogSingleLine( "Total node + subnodes: " + ArcenDebugging.GetStopwatchMilliseconds() + "ms", Verbosity.DoNotShow );
            ArcenDebugging.ResetStopwatch();
        }

        #region NameLabel
        private static int PrintLabelToVis( Control.ControlCollection controls, KeyValuePair<string, MetaAttribute_Base> pair, EditedXmlNode node,
                                            EditedXmlAttribute? xmlAttribute, UnionAttribute uAttribute, Graphics graphics, Caret caret )
        {
            Label label = attributeLabelPool.GetOrAdd( ( newLabel ) =>
            {
#pragma warning disable CS8622 //The label will never be null
                newLabel.Click += new EventHandler( OpenSmallMenuOnLabelRightClick );
                newLabel.DoubleClick += new EventHandler( OpenSmallMenuOnLabelDoubleClick );
                newLabel.MouseHover += new EventHandler( ShowTooltipOnLabelHover );
#pragma warning restore CS8622
            } );
            SizeF size = graphics.MeasureString( pair.Key, MainWindow.Instance.VisControls.Font );
            label.Height = Math.Max( genericHeightBasedOnFontUsed, (int)Math.Ceiling( size.Height ) );
            label.Width = (int)Math.Ceiling( size.Width );
            label.TextAlign = ContentAlignment.MiddleLeft;

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
            uAttribute.MetaAttribute = pair;
            uAttribute.Controls.Add( label );
            ((PooledControlTagInfo)label.Tag).RelatedUnionElement = uAttribute;

            GetHeightAndMoveCaretRight( label, caret, label.Width + 3 - EXTRA_PIXELS_W );
            return label.Height;
        }
        #endregion

        private void VisElementByType( Control.ControlCollection controls, MetadataDocument currentMetaDoc, MetaAttribute_Base metaAttribute, EditedXmlAttribute? xmlAttribute,
                                       UnionNode currentUnionNode, UnionAttribute uAttribute, Graphics graphics, Caret caret, int controlHeight )
        {
            //Stopwatch sw = Stopwatch.StartNew();
            switch ( metaAttribute.Type )
            {
                #region Bool
                case AttributeType.Bool:
                case AttributeType.BoolInt:
                    {
                        CheckBox boxBool = checkBoxPool.GetOrAdd( ( newCheckBox ) =>
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
                                string? s = xmlAttribute.GetEffectiveValue();
                                if ( s != null )
                                {
                                    if ( int.Parse( s ) == 1 )
                                        boxBool.Checked = true;
                                    if ( int.Parse( s ) == 0 )
                                        boxBool.Checked = true;
                                }
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
                        TextBox textBox = textBoxPool.GetOrAdd( ( newTextBox ) =>
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
                        TextBox textBox = textBoxPool.GetOrAdd( ( newTextBox ) =>
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
                        ComboBox comboBox = comboBoxPool.GetOrAdd( ( newComboBox ) =>
                        {
                            newComboBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            newComboBox.SelectedIndexChanged += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        comboBox.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_ArbitraryString)metaAttribute).ContentWidthPx, controlHeight );
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                        comboBox.Items.Add( "Empty (To deselect any choice)" );
                        //Stopwatch stopwatch = Stopwatch.StartNew();
                        foreach ( string option in ((MetaAttribute_ArbitraryString)metaAttribute).Options )
                            comboBox.Items.Add( option );
                        //ArcenDebugging.LogSingleLine( $" Adding all strings to comboBox: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
                        //stopwatch.Reset();
                        if ( xmlAttribute != null )
                        {
                            //stopwatch.Start();
                            comboBox.SelectedIndex = comboBox.FindStringExact( xmlAttribute.GetEffectiveValue() );
                            //ArcenDebugging.LogSingleLine( $" FindingExactString: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
                            //stopwatch.Reset();
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
                        NumericUpDown numeric = numericUpDownPool.GetOrAdd( ( newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        numeric.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Int)metaAttribute).ContentWidthPx, controlHeight );
                        numeric.ThousandsSeparator = true;
                        numeric.DecimalPlaces = 0;
                        numeric.Maximum = ((MetaAttribute_Int)metaAttribute).Max;
                        numeric.Minimum = ((MetaAttribute_Int)metaAttribute).Min;

                        if ( xmlAttribute != null )
                        {
                            string? s = xmlAttribute.GetEffectiveValue();
                            if ( s != null )
                            {
                                numeric.Value = int.Parse( s );
                                LinkDataAndExecuteCommonActions( controls, numeric, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                            }
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
                        NumericUpDown numeric = numericUpDownPool.GetOrAdd( ( newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
                        numeric.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Float)metaAttribute).ContentWidthPx, controlHeight ); // width set by meta

                        numeric.ThousandsSeparator = true;
                        numeric.DecimalPlaces = ((MetaAttribute_Float)metaAttribute).Precision;
                        if ( ((MetaAttribute_Float)metaAttribute).Min < decimal.MinValue )
                            numeric.Minimum = decimal.MinValue;
                        else
                            numeric.Minimum = ((MetaAttribute_Float)metaAttribute).Min;

                        if ( ((MetaAttribute_Float)metaAttribute).Max < decimal.MaxValue )
                            numeric.Maximum = decimal.MaxValue;
                        else
                            numeric.Maximum = ((MetaAttribute_Float)metaAttribute).Max;

                        if ( xmlAttribute != null )
                        {
                            string? s = xmlAttribute.GetEffectiveValue();
                            if ( s != null )
                            {
                                numeric.Value = decimal.Parse( s );
                                LinkDataAndExecuteCommonActions( controls, numeric, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                            }
                        }
                        else
                        {
                            numeric.Value = ((MetaAttribute_Float)metaAttribute).Default;
                            LinkDataAndExecuteCommonActions( controls, numeric, currentUnionNode, uAttribute, null, metaAttribute );
                        }
                        GetHeightAndMoveCaretRight( numeric, caret, metaAttribute.ContentWidthPx );
                    }
                    break;
                #endregion

                #region ArbitraryNode
                case AttributeType.ArbitraryNode:
                    {

                        //Stopwatch stopwatch = Stopwatch.StartNew();
                        ComboBox comboBox = comboBoxPool.GetOrAdd( ( newComboBox ) =>
                        {
                            newComboBox.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            newComboBox.SelectedIndexChanged += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            newComboBox.Enter += new EventHandler( FillDropdownWithNodesOnEnter );
                        } );
                        //ArcenDebugging.LogSingleLine( $" GetOrAdd from Pool: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
                        //stopwatch.Reset();
                        //stopwatch.Start();
                        comboBox.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_ArbitraryNode)metaAttribute).ContentWidthPx, controlHeight );
                        comboBox.DropDownStyle = ComboBoxStyle.DropDownList;
                        comboBox.Items.Add( "Empty (To deselect any choice)" );
                        //move to event the part below?
                        if ( xmlAttribute != null )
                        {
                            //stopwatch.Start();
                            comboBox.SelectedIndex = comboBox.FindStringExact( xmlAttribute.GetEffectiveValue() );
                            //ArcenDebugging.LogSingleLine( $" FindingExactString: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
                            //stopwatch.Reset();
                            //stopwatch.Start();
                            LinkDataAndExecuteCommonActions( controls, comboBox, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                            //ArcenDebugging.LogSingleLine( $" LinkDataAndExecuteCommonActions: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
                            //stopwatch.Reset();

                        }
                        else
                        {
                            //stopwatch.Start();
                            comboBox.Items.Add( ((MetaAttribute_ArbitraryNode)metaAttribute).Default );
                            comboBox.SelectedIndex = 1;
                            //ArcenDebugging.LogSingleLine( $" FindingExactString: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
                            //stopwatch.Reset();
                            //stopwatch.Start();
                            LinkDataAndExecuteCommonActions( controls, comboBox, currentUnionNode, uAttribute, null, metaAttribute );
                            //ArcenDebugging.LogSingleLine( $" LinkDataAndExecuteCommonActions: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
                            //stopwatch.Reset();
                        }
                        //stopwatch.Start();
                        GetHeightAndMoveCaretRight( comboBox, caret, metaAttribute.ContentWidthPx );
                        //ArcenDebugging.LogSingleLine( $" GetHeightAndMoveCaretRight: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
                        //stopwatch.Reset();
                    }
                    break;
                #endregion

                #region NodeList
                case AttributeType.NodeList:
                    {
                        Button openListButton = openCheckedListBoxDropdownButtonEvent_Pool.GetOrAdd( ( newButton ) =>
                        {
                            newButton.Click += new EventHandler( OpenCheckedListBoxDropdown_ButtonClickEvent );
                            newButton.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
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
                        Button openListButton = openCheckedListBoxDropdownButtonEvent_Pool.GetOrAdd( ( newButton ) =>
                        {
                            newButton.Click += new EventHandler( OpenCheckedListBoxDropdown_ButtonClickEvent );
                            newButton.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                        } );
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
                        NumericUpDown numeric1 = numericUpDownPool.GetOrAdd( ( newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.x;
                        } );
                        NumericUpDown numeric2 = numericUpDownPool.GetOrAdd( ( newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.y;
                        } );

                        numeric1.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Point)metaAttribute).ContentWidthPx, controlHeight );
                        caret.MoveHorizontally( ((MetaAttribute_Point)metaAttribute).ContentWidthPx + EXTRA_PIXELS_W );
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
                            string? s = xmlAttribute.GetEffectiveValue();
                            if ( s != null )
                            {
                                string[] values = s.Split( ',' );
                                numeric1.Value = int.Parse( values[0] );
                                numeric2.Value = int.Parse( values[1] );
                                LinkDataMultipleControls( controls, tempControls, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                            }
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
                        NumericUpDown numeric1 = numericUpDownPool.GetOrAdd( ( newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.x;
                        } );
                        NumericUpDown numeric2 = numericUpDownPool.GetOrAdd( ( newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.y;
                        } );
                        numeric1.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx, controlHeight );
                        caret.MoveHorizontally( ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx + EXTRA_PIXELS_W );
                        numeric2.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector2)metaAttribute).ContentWidthPx, controlHeight );

                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = ((MetaAttribute_Vector2)metaAttribute).x.Precision;
                        if ( ((MetaAttribute_Vector2)metaAttribute).x.Min < decimal.MinValue )
                            numeric1.Minimum = decimal.MinValue;
                        else
                            numeric1.Minimum = ((MetaAttribute_Vector2)metaAttribute).x.Min;
                        if ( ((MetaAttribute_Vector2)metaAttribute).x.Max > decimal.MaxValue )
                            numeric1.Maximum = decimal.MaxValue;
                        else
                            numeric1.Maximum = ((MetaAttribute_Vector2)metaAttribute).x.Max;

                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = ((MetaAttribute_Vector2)metaAttribute).y.Precision;
                        if ( ((MetaAttribute_Vector2)metaAttribute).y.Min < decimal.MinValue )
                            numeric2.Minimum = decimal.MinValue;
                        else
                            numeric2.Minimum = ((MetaAttribute_Vector2)metaAttribute).y.Min;
                        if ( ((MetaAttribute_Vector2)metaAttribute).y.Max > decimal.MaxValue )
                            numeric2.Maximum = decimal.MaxValue;
                        else
                            numeric2.Maximum = ((MetaAttribute_Vector2)metaAttribute).y.Max;

                        Control[] tempControls = new Control[2];
                        tempControls[0] = numeric1;
                        tempControls[1] = numeric2;

                        if ( xmlAttribute != null )
                        {
                            string? s = xmlAttribute.GetEffectiveValue();
                            if ( s != null )
                            {
                                string[] values = s.Split( ',' );
                                numeric1.Value = decimal.Parse( values[0] );
                                numeric2.Value = decimal.Parse( values[1] );
                                LinkDataMultipleControls( controls, tempControls, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                            }
                        }
                        else
                        {
                            numeric1.Value = ((MetaAttribute_Vector2)metaAttribute).x.Default;
                            numeric2.Value = ((MetaAttribute_Vector2)metaAttribute).y.Default;
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
                        NumericUpDown numeric1 = numericUpDownPool.GetOrAdd( ( newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.x;
                        } );
                        NumericUpDown numeric2 = numericUpDownPool.GetOrAdd( ( newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.y;
                        } );
                        NumericUpDown numeric3 = numericUpDownPool.GetOrAdd( ( newNumeric ) =>
                        {
                            newNumeric.LostFocus += new EventHandler( CallValidatorAfterFocusLostOrIndexChanged );
                            ((PooledControlTagInfo)newNumeric.Tag).ControlsCoordinate = Coordinate.z;
                        } );

                        numeric1.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx, controlHeight );
                        caret.MoveHorizontally( ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx + EXTRA_PIXELS_W );
                        numeric2.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx, controlHeight );
                        caret.MoveHorizontally( ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx + EXTRA_PIXELS_W );
                        numeric3.Bounds = new Rectangle( caret.x, caret.y, ((MetaAttribute_Vector3)metaAttribute).ContentWidthPx, controlHeight );

                        numeric1.ThousandsSeparator = true;
                        numeric1.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).x.Precision;
                        if ( ((MetaAttribute_Vector3)metaAttribute).x.Min < decimal.MinValue )
                            numeric1.Minimum = decimal.MinValue;
                        else
                            numeric1.Minimum = ((MetaAttribute_Vector3)metaAttribute).x.Min;
                        if ( ((MetaAttribute_Vector3)metaAttribute).x.Max > decimal.MaxValue )
                            numeric1.Maximum = decimal.MaxValue;
                        else
                            numeric1.Maximum = ((MetaAttribute_Vector3)metaAttribute).x.Max;

                        numeric2.ThousandsSeparator = true;
                        numeric2.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).y.Precision;
                        if ( ((MetaAttribute_Vector3)metaAttribute).y.Min < decimal.MinValue )
                            numeric2.Minimum = decimal.MinValue;
                        else
                            numeric2.Minimum = ((MetaAttribute_Vector3)metaAttribute).y.Min;
                        if ( ((MetaAttribute_Vector3)metaAttribute).y.Max > decimal.MaxValue )
                            numeric2.Maximum = decimal.MaxValue;
                        else
                            numeric2.Maximum = ((MetaAttribute_Vector3)metaAttribute).y.Max;

                        numeric3.ThousandsSeparator = true;
                        numeric3.DecimalPlaces = ((MetaAttribute_Vector3)metaAttribute).z.Precision;
                        if ( ((MetaAttribute_Vector3)metaAttribute).z.Min < decimal.MinValue )
                            numeric3.Minimum = decimal.MinValue;
                        else
                            numeric3.Minimum = ((MetaAttribute_Vector3)metaAttribute).z.Min;
                        if ( ((MetaAttribute_Vector3)metaAttribute).z.Max > decimal.MaxValue )
                            numeric3.Maximum = decimal.MaxValue;
                        else
                            numeric3.Maximum = ((MetaAttribute_Vector3)metaAttribute).z.Max;

                        Control[] tempControls = new Control[3];
                        tempControls[0] = numeric1;
                        tempControls[1] = numeric2;
                        tempControls[2] = numeric3;

                        if ( xmlAttribute != null )
                        {
                            string? s = xmlAttribute.GetEffectiveValue();
                            if ( s != null )
                            {
                                string[] values = s.Split( ',' );
                                numeric1.Value = decimal.Parse( values[0] );
                                numeric2.Value = decimal.Parse( values[1] );
                                numeric3.Value = decimal.Parse( values[2] );
                                LinkDataMultipleControls( controls, tempControls, currentUnionNode, uAttribute, xmlAttribute, metaAttribute );
                            }

                        }
                        else
                        {
                            numeric1.Value = ((MetaAttribute_Vector3)metaAttribute).x.Default;
                            numeric2.Value = ((MetaAttribute_Vector3)metaAttribute).y.Default;
                            numeric3.Value = ((MetaAttribute_Vector3)metaAttribute).z.Default;
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
            //ArcenDebugging.LogSingleLine( $"{metaAttribute.Type} - VisElementByType: " + (1000 * (double)sw.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
        }

        private void ExecuteToUndoStack( MetaAttribute_Base metaAttribute ) //todo
        {
            switch ( metaAttribute.Type )
            {
                //create delegate based on type and add to undo stack
            }
        }

        private static void GetHeightAndMoveCaretRight( Control control, Caret caret, int width )
        {
            if ( control.Height > maxHeightInCurrentRow )
                maxHeightInCurrentRow = control.Height;
            caret.MoveHorizontally( width + EXTRA_PIXELS_W );
        }

        #region Linking Data in UnionNode
        private static void LinkDataMultipleControls( Control.ControlCollection controls, Control[] controlsToAdd, UnionNode currentUNode, UnionAttribute uAttribute,
                                                      EditedXmlAttribute? xmlAttribute, MetaAttribute_Base metaAttribute )
        {
            for ( int i = 0; i < controlsToAdd.Length; i++ )
                if ( i + 1 == controlsToAdd.Length )
                    LinkDataAndExecuteCommonActions( controls, controlsToAdd[i], currentUNode, uAttribute, xmlAttribute, metaAttribute, false );
                else
                    LinkDataAndExecuteCommonActions( controls, controlsToAdd[i], currentUNode, uAttribute, xmlAttribute, metaAttribute );
        }

        private static void LinkDataAndExecuteCommonActions( Control.ControlCollection controls, Control control, UnionNode currentUNode, UnionAttribute uAttribute,
                                                             EditedXmlAttribute? xmlAttribute, MetaAttribute_Base metaAttribute, bool addToUNodeListOfAttributes = true )
        {
            //Stopwatch sw = Stopwatch.StartNew();
            ControlTagInfo tagData = (ControlTagInfo)control.Tag;
            //string debug = string.Empty;
            uAttribute.Controls.Add( control );
            //ArcenDebugging.LogSingleLine( $" LinkDataAndExecuteCommonActions (Init): " + (1000 * (double)sw.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
            //sw.Reset();
            //sw.Start();
            if ( addToUNodeListOfAttributes )
            {
                uAttribute.MetaAttribute = KeyValuePair.Create( metaAttribute.Key, metaAttribute );
                metaAttribute.RelatedUnionAttribute = uAttribute;

                uAttribute.XmlAttribute = xmlAttribute; // duplicate assignment - done in PrintLabelToVis()
                if ( xmlAttribute != null )
                    xmlAttribute.RelatedUnionAttribute = uAttribute;

                currentUNode.UnionAttributes.Add( uAttribute );
            }
            tagData.RelatedUnionElement = uAttribute;
            //ArcenDebugging.LogSingleLine( $" LinkDataAndExecuteCommonActions (Added to UNode attributes: {addToUNodeListOfAttributes}): " + (1000 * (double)sw.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
            //sw.Reset();
            //sw.Start();
            controls.Add( control );
            //ArcenDebugging.LogSingleLine( $" LinkDataAndExecuteCommonActions (controls.add): " + (1000 * (double)sw.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
            //sw.Reset();
            //sw.Start();
            if ( xmlAttribute != null )
            {
                xmlAttribute.CurrentViewControl_Value = control;
                //debug += "Attribute name: " + xmlAttribute.Name;
            }
            //debug += " metaAttribute.Type: " + metaAttribute.Type;
            //ArcenDebugging.LogSingleLine( $" LinkDataAndExecuteCommonActions (small code): " + (1000 * (double)sw.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
            //sw.Reset();
            //sw.Start();
            //currentUNode.CheckDataIntegrity( debug );
            tagData.ClearErrorProvider( control );
            //ArcenDebugging.LogSingleLine( $" LinkDataAndExecuteCommonActions (ClearErrorProvider): " + (1000 * (double)sw.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
            //sw.Reset();
            //sw.Start();
            XmlValidator.Validate( control );
            //ArcenDebugging.LogSingleLine( $" LinkDataAndExecuteCommonActions (XmlValidator): " + (1000 * (double)sw.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
            //sw.Reset();
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
                        caret.NextLine( height + EXTRA_PIXELS_H );
                        justInsertedLineBreak = true;
                    }
                    else
                        justInsertedLineBreak = false;
                    break;
                case LineBreakType.PreferNot:
                    if ( MainWindow.Instance.VisControls.ClientSize.Width - (caret.x + (upcomingCaretHorizonatalMove * widthMultiplier) + (metaAttribute.ContentWidthPx * widthMultiplier)) < 5 )
                    {
                        caret.NextLine( height + EXTRA_PIXELS_H );
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
                    caret.NextLine( height + EXTRA_PIXELS_H );
                    justInsertedLineBreak = true;
                    break;
                case LineBreakType.PreferNot:
                    if ( MainWindow.Instance.VisControls.ClientSize.Width - caret.x < 20 ) // might remove this check?
                    {
                        caret.NextLine( height + EXTRA_PIXELS_H );
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
        //    SizeF size = graphics.MeasureString( text, MainWindow.Instance.visControls.Font );
        //    return new Rectangle( caret.x, Caret.y, (int)Math.Ceiling( size.Width ), (int)Math.Ceiling( size.Height ) );
        //}
        //private static Rectangle CalculateBounds( string text, int width, Graphics graphics )
        //{
        //    SizeF size = graphics.MeasureString( text, MainWindow.Instance.visControls.Font, width );
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

        private static void OpenOrDoCLB_ButtonClickEvent( object? sender, EventArgs e )
        {
            // get all missing attributes from parent union node and populate checkedListBoxPlusButton from that (check if there are any controls on the att to understand if it's in vis or not)
            Button? button = sender as Button;
            if ( button == null )
            {
                ArcenDebugging.LogSingleLine( "Button in OpenCheckedListBoxPlusButton_ButtonClickEvent null!", Verbosity.DoNotShow );
                return;
            }
            PooledControlTagInfo pooledControlTag = (PooledControlTagInfo)button.Tag;
            UnionNode? uNode = pooledControlTag.RelatedUnionElement as UnionNode;
            if ( uNode == null )
            {
                ArcenDebugging.LogSingleLine( "uNode in OpenCheckedListBoxPlusButton_ButtonClickEvent null!", Verbosity.DoNotShow );
                return;
            }
            bool isOpen = pooledControlTag.IsOpen;
            if ( isOpen )
            {
                #region Close CLB and add Attributes to node
                //CheckedListBox? checkedListBox = sender as CheckedListBox;
                //if ( checkedListBox == null )
                //{
                //    ArcenDebugging.LogSingleLine( "checkedListBox in CloseCheckedListBoxPlusButton_CBLLeaveEvent is null!", Verbosity.DoNotShow );
                //    return;
                //}
                if ( checkedListBoxPlusButton.CheckedItems.Count == 0 ) //remove/refactor?
                {
                    checkedListBoxPlusButton.Visible = false;
                    ArcenDebugging.LogSingleLine( "checkedListBoxPlusButton.CheckedItems.Count = 0", Verbosity.DoNotShow );
                    return;
                }
                CheckedListBoxTagData tagData = (CheckedListBoxTagData)checkedListBoxPlusButton.Tag;
                if ( tagData == null )
                {
                    ArcenDebugging.LogSingleLine( "ERROR: tagData is NULL in CloseCheckedListBoxPlusButton_CLBLostFocusEvent", Verbosity.ShowAsError );
                    return;
                }

                foreach ( string attName in checkedListBoxPlusButton.CheckedItems )
                {
                    EditedXmlAttribute att = new EditedXmlAttribute();
                    att.Name = attName;
                    // Need the default value which is type dependant
                    switch ( uNode.MetaLayer.AttributesData[attName].Type )
                    {
                        case AttributeType.Bool:
                            att.TempValue = ((MetaAttribute_Bool)uNode.MetaLayer.AttributesData[attName]).Default.ToString();
                            break;
                        case AttributeType.BoolInt:
                            att.TempValue = ((MetaAttribute_BoolInt)uNode.MetaLayer.AttributesData[attName]).Default.ToString();
                            break;
                        case AttributeType.String:
                            att.TempValue = ((MetaAttribute_String)uNode.MetaLayer.AttributesData[attName]).Default;
                            break;
                        case AttributeType.StringMultiLine:
                            att.TempValue = ((MetaAttribute_StringMultiline)uNode.MetaLayer.AttributesData[attName]).Default;
                            break;
                        case AttributeType.ArbitraryString:
                            att.TempValue = ((MetaAttribute_ArbitraryString)uNode.MetaLayer.AttributesData[attName]).Default;
                            break;
                        case AttributeType.Int:
                            att.TempValue = ((MetaAttribute_Int)uNode.MetaLayer.AttributesData[attName]).Default.ToString();
                            break;
                        case AttributeType.Float:
                            att.TempValue = ((MetaAttribute_Float)uNode.MetaLayer.AttributesData[attName]).Default.ToString();
                            break;
                        case AttributeType.ArbitraryNode:
                            att.TempValue = ((MetaAttribute_ArbitraryNode)uNode.MetaLayer.AttributesData[attName]).Default;
                            break;
                        case AttributeType.NodeList:
                            foreach ( string nodeForList in ((MetaAttribute_NodeList)uNode.MetaLayer.AttributesData[attName]).Defaults )
                                att.TempValue = nodeForList + ",";
                            att.TempValue = att.TempValue?.Remove( att.TempValue.Length - 1 );
                            break;
                        case AttributeType.FolderList:
                            foreach ( string folderForList in ((MetaAttribute_FolderList)uNode.MetaLayer.AttributesData[attName]).Defaults )
                                att.TempValue = folderForList + ",";
                            att.TempValue = att.TempValue?.Remove( att.TempValue.Length - 1 );
                            break;
                        case AttributeType.Point:
                            att.TempValue = ((MetaAttribute_Point)uNode.MetaLayer.AttributesData[attName]).x.Default.ToString();
                            break;
                        case AttributeType.Vector2:
                            att.TempValue = ((MetaAttribute_Vector2)uNode.MetaLayer.AttributesData[attName]).x.Default.ToString();
                            break;
                        case AttributeType.Vector3:
                            att.TempValue = ((MetaAttribute_Vector3)uNode.MetaLayer.AttributesData[attName]).x.Default.ToString();
                            break;
                        default:
                            ArcenDebugging.LogSingleLine( $"Error: uNode.MetaLayer.AttributesData[attName].Type {uNode.MetaLayer.AttributesData[attName].Type} in CloseCheckedListBoxPlusButton_CLBLostFocusEvent() not recognized",
                                                          Verbosity.ShowAsError );
                            break;
                    }
                    if ( tagData.Node == null )
                    {
                        ArcenDebugging.LogSingleLine( "Error: tagData.Node is NULL in CloseCheckedListBoxPlusButton_CLBLostFocusEvent()", Verbosity.ShowAsError );
                        continue;
                    }
                    if ( tagData.Node.Attributes.ContainsKey( attName ) ) //means that it was previously deleted
                    {
                        ArcenDebugging.LogSingleLine( "Key Already present ", Verbosity.DoNotShow );
                        tagData.Node.Attributes[attName].IsDeleted = false;
                        //if ( MainWindow.Instance.XmlElementCurrentlyBeingEdited is EditedXmlNode node )
                        //    node.ChildNodes.Add( newSubNode );
                    }
                    else
                    {
                        ArcenDebugging.LogSingleLine( "new Key", Verbosity.DoNotShow );

                        tagData.Node.Attributes.Add( attName, att );
                    }
                }
                checkedListBoxPlusButton.Visible = false;
                UnionNode? topUNode = tagData.UNode;
                while ( topUNode.ParentUnionNode != null )
                    topUNode = topUNode.ParentUnionNode;
                if ( topUNode.XmlNodeOrComment != null )
                    tagData.Vis.OuterVisualizeSelectedNode( topUNode.XmlNodeOrComment, topUNode.MetaDocument.TopLevelNode, forceClearVis: true );
                else
                    ArcenDebugging.LogSingleLine( "uNode.XmlNodeOrComment null in CloseCheckedListBoxPlusButton_CBLLeaveEvent", Verbosity.DoNotShow );
                #endregion
                isOpen = false;
            }
            else
            {
                checkedListBoxPlusButton.Items.Clear();
                foreach ( UnionAttribute uAtt in uNode.UnionAttributes )
                    if ( uAtt.Controls.Count == 0 )
                        checkedListBoxPlusButton.Items.Add( uAtt.MetaAttribute.Key );

                checkedListBoxPlusButton.Bounds = new Rectangle( button.Bounds.X + button.Bounds.Width, button.Bounds.Y + button.Bounds.Height,
                                                     300, genericHeightBasedOnFontUsed * AMOUNT_OF_LINES_TO_DISPLAY_IN_CLB );
                checkedListBoxPlusButton.Height = genericHeightBasedOnFontUsed * AMOUNT_OF_LINES_TO_DISPLAY_IN_CLB;
                checkedListBoxPlusButton.Visible = true;
                checkedListBoxPlusButton.BringToFront();
                checkedListBoxPlusButton.Focus();
                isOpen = true;
            }
            pooledControlTag.IsOpen = isOpen;
        }

        private static void CloseCheckedListBoxPlusButton_CLBLostFocusEvent( object? sender, EventArgs e )
        {
            CheckedListBox? checkedListBox = sender as CheckedListBox;
            if ( checkedListBox == null )
            {
                ArcenDebugging.LogSingleLine( "checkedListBox in CloseCheckedListBoxPlusButton_CBLLeaveEvent is null!", Verbosity.DoNotShow );
                return;
            }
            if ( checkedListBoxPlusButton.CheckedItems.Count == 0 )
            {
                checkedListBoxPlusButton.Visible = false;
                ArcenDebugging.LogSingleLine( "checkedListBoxPlusButton.CheckedItems.Count = 0", Verbosity.DoNotShow );
                return;
            }
            CheckedListBoxTagData tagData = (CheckedListBoxTagData)checkedListBox.Tag;
            if ( tagData == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR: tagData is NULL in CloseCheckedListBoxPlusButton_CLBLostFocusEvent", Verbosity.ShowAsError );
                return;
            }

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
                    default:
                        ArcenDebugging.LogSingleLine( $"Error: tagData.MetaAttributes[attName].Type {tagData.MetaAttributes[attName].Type} in CloseCheckedListBoxPlusButton_CLBLostFocusEvent() not recognized",
                                                      Verbosity.ShowAsError );
                        break;
                }
                if ( tagData.Node == null )
                {
                    ArcenDebugging.LogSingleLine( "Error: tagData.Node is NULL in CloseCheckedListBoxPlusButton_CLBLostFocusEvent()", Verbosity.ShowAsError );
                    continue;
                }
                if ( tagData.Node.Attributes.ContainsKey( attName ) ) //means that it was previously deleted
                {
                    ArcenDebugging.LogSingleLine( "Key Already present ", Verbosity.DoNotShow );
                    tagData.Node.Attributes[attName].IsDeleted = false;
                    //if ( MainWindow.Instance.XmlElementCurrentlyBeingEdited is EditedXmlNode node )
                    //    node.ChildNodes.Add( newSubNode );
                }
                else
                {
                    ArcenDebugging.LogSingleLine( "new Key", Verbosity.DoNotShow );

                    tagData.Node.Attributes.Add( attName, att );
                }
            }
            checkedListBoxPlusButton.Visible = false;
            UnionNode? uNode = tagData.UNode;
            while ( uNode.ParentUnionNode != null )
                uNode = uNode.ParentUnionNode;
            if ( uNode.XmlNodeOrComment != null )
                tagData.Vis.OuterVisualizeSelectedNode( uNode.XmlNodeOrComment, uNode.MetaDocument.TopLevelNode, forceClearVis: true );
            else
                ArcenDebugging.LogSingleLine( "uNode.XmlNodeOrComment null in CloseCheckedListBoxPlusButton_CBLLeaveEvent", Verbosity.DoNotShow );
        }
        #endregion

        #region SmallLabelToolBar
        private static void OpenSmallMenuOnLabelRightClick( object sender, EventArgs e )
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if ( me.Button == MouseButtons.Right )
                CreateLabelMenu( (Control)sender );
        }

        private static void OpenSmallMenuOnLabelDoubleClick( object sender, EventArgs e )
        {
            MouseEventArgs me = (MouseEventArgs)e;
            if ( me.Button == MouseButtons.Left )
                CreateLabelMenu( (Control)sender );
        }

        private static void CreateLabelMenu( Control control )
        {
            int targetX = control.Bounds.X;
            int targetY = control.Bounds.Y + control.Height;
            /*Control parent = control.Parent;
            while ( parent != null )
            {
                targetX += parent.Bounds.X;
                targetY += parent.Bounds.Y;
                parent = parent.Parent;
            }
            //targetX += MainWindow.Instance.Bounds.X; // increases the X by too much
            targetY += MainWindow.Instance.Bounds.Y;
            targetX += 10; // manually adjusting the coordinates
            //targetY -= MainWindow.Instance.RectangleToScreen( MainWindow.Instance.ClientRectangle ).Top - MainWindow.Instance.Top; // removing the tile height;*/

            LabelMenu labelMenu = new LabelMenu( control, targetX, targetY );
            labelMenu.Show();
        }
        #endregion

        #region SubNodeButton
        private void AddNewSubNode_ButtonClick( object sender, EventArgs e )
        {
            foreach ( KeyValuePair<string, NumericUpDown> kv in subNodesNumericTempHold )
            {
                if ( kv.Value.Value <= 0 )
                    continue;
                for ( int i = 0; i < kv.Value.Value; i++ )
                {
                    EditedXmlNode newSubNode = new EditedXmlNode();
                    newSubNode.XmlNodeTagName = kv.Key;
                    //newSubNode.Attributes 
                    if ( MainWindow.Instance.XmlElementCurrentlyBeingEdited is EditedXmlNode node )
                        node.ChildNodes.Add( newSubNode );
                }
            }
            MainWindow.Instance.CallXmlVisualizer( MainWindow.Instance.XmlElementCurrentlyBeingEdited );
        }
        #endregion

        private static void ShowTooltipOnLabelHover( object sender, EventArgs e )
        {
            Control control = (Control)sender;
            UnionAttribute? uAttribute = ((ControlTagInfo)control.Tag).RelatedUnionElement as UnionAttribute;
            if ( uAttribute != null )
                toolTip.SetToolTip( control, uAttribute.MetaAttribute.Value.Tooltip );
        }

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
            //ExecuteToUndoStack( (UnionNode( (PooledControlTagInfo)control.Tag ).RelatedUnionElement) )
            XmlValidator.Validate( control );
        }

        private void FillDropdownWithNodesOnEnter( object sender, EventArgs e ) //todo
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();
            ComboBox comboBox = (ComboBox)sender;
            UnionAttribute? uAttribute = ((ControlTagInfo)comboBox.Tag).RelatedUnionElement as UnionAttribute;
            MetaAttribute_Base metaAttribute;
            if ( uAttribute == null )
                return;
            metaAttribute = uAttribute.MetaAttribute.Value;

            XmlDataTable? nodeSourceTable = XmlRootFolders.GetXmlDataTableByName( ((MetaAttribute_ArbitraryNode)metaAttribute).NodeSource );
            if ( nodeSourceTable == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR: nodeSourceTable is NULL in VisElementByType() - AttributeType.ArbitraryNode!", Verbosity.ShowAsError );
                return;
            }
            //ArcenDebugging.LogSingleLine( $" Get XMLDataTable: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
            //stopwatch.Reset();
            //stopwatch.Start();
            List<TopNodesCaching.TopNode>? topNodesCache = TopNodesCaching.GetAllNodesForDataTable( nodeSourceTable.MetaDoc );
            if ( topNodesCache == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR: topNodes is NULL in VisElementByType() - AttributeType.ArbitraryNode!", Verbosity.ShowAsError );
                return;
            }
            //ArcenDebugging.LogSingleLine( $" Get allNodes from Table: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
            //stopwatch.Reset();
            //stopwatch.Start();
            foreach ( TopNodesCaching.TopNode? item in topNodesCache )
                if ( item != null )
                    comboBox.Items.Add( item.CentralID );
            //ArcenDebugging.LogSingleLine( $" Adding all strings to comboBox: " + (1000 * (double)stopwatch.ElapsedTicks / Stopwatch.Frequency) + "ms", Verbosity.DoNotShow );
            //stopwatch.Reset();
        }
        #endregion

        #region XmlValidator
        private static class XmlValidator
        {
            public static void Validate( Control control )
            {
                double timeToWaitBeforeValidating = 0.4f;
                if ( timeOfVisStart == DateTime.UnixEpoch || (DateTime.Now - timeOfVisStart).TotalSeconds < timeToWaitBeforeValidating )
                    return;

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
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.BoolInt:
                        {
                            string error = ((MetaAttribute_BoolInt)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.String:
                        {
                            string error = ((MetaAttribute_String)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.StringMultiLine:
                        {
                            string error = ((MetaAttribute_StringMultiline)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.ArbitraryString:
                        {
                            string error = ((MetaAttribute_ArbitraryString)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Int:
                        {
                            string error = ((MetaAttribute_Int)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Float:
                        {
                            string error = ((MetaAttribute_Float)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.ArbitraryNode:
                        {
                            string error = ((MetaAttribute_ArbitraryNode)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.NodeList:
                        {
                            string error = ((MetaAttribute_NodeList)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.FolderList:
                        {
                            string error = ((MetaAttribute_FolderList)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Point:
                        {
                            string error = ((MetaAttribute_Point)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Vector2:
                        {
                            string error = ((MetaAttribute_Vector2)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
                            else
                                errorProvider.SetError( control, string.Empty );
                        }
                        break;
                    case AttributeType.Vector3:
                        {
                            string error = ((MetaAttribute_Vector3)uAtt.MetaAttribute.Value).DoValidate( uAtt.XmlAttribute, tagData.ControlsCoordinate );
                            if ( error != string.Empty )
                                errorProvider.SetError( control, error );
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
                //uAtt.XmlAttribute.Type = uAtt.MetaAttribute.Value.Type;

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
                    //uAtt.XmlAttribute.Type = AttributeType.Unknown;
                    uAtt.XmlAttribute.TempValue = null;
                }
            }
        }
        #endregion
    }
}