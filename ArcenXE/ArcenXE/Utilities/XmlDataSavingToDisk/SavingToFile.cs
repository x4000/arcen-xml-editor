using ArcenXE.Universal;
using System.Numerics;

namespace ArcenXE.Utilities.XmlDataSavingToDisk
{
    public static class SavingToFile
    {
        public static bool TrySave( IEditedXmlNodeOrComment? nodeOrComment, bool isBackupFile )
        {
            //ArcenDebugging.LogSingleLine( $"Step 1", Verbosity.DoNotShow );
            if ( nodeOrComment == null )
            {
                ArcenDebugging.LogSingleLine( $"XmlElementCurrentlyBeingEdited is null. Can't save this xml to file!", Verbosity.DoNotShow );
                return false;
            }

            XmlWriter xmlOutput = new XmlWriter( true );
            if ( nodeOrComment.IsComment )
            {
                //todo
            }
            else
            {
                //ArcenDebugging.LogSingleLine( $"Step 2", Verbosity.DoNotShow );
                bool justInsertedLineBreak = false;
                EditedXmlNode node = (EditedXmlNode)nodeOrComment;
                if ( node.IsRootOnly )
                {
                    // loop node's attributes
                    //ArcenDebugging.LogSingleLine( $"Step 3.a", Verbosity.DoNotShow );
                    xmlOutput.StartOpenNode( "root", false ).NewLine( XmlLeadingWhitespace.None );
                    foreach ( KeyValuePair<string, EditedXmlAttribute> att in node.Attributes )
                        ReadAttributeFromEditedData( xmlOutput, att, ref justInsertedLineBreak, false );
                    xmlOutput.FinishOpenNode( true );
                    xmlOutput.CloseNode( "root", false, true );
                }
                else
                {
                    // loop top node's attributes, then do the same for all the subnodes
                    //ArcenDebugging.LogSingleLine( $"Step 3.b", Verbosity.DoNotShow );
                    if ( node.RelatedUnionNode == null )
                    {
                        ArcenDebugging.LogSingleLine( $"RelatedUnionNode inside EditedXmlNode {node.NodeCentralID?.GetEffectiveValue() ?? node.XmlNodeTagName} is null. Can't save this node to file!", Verbosity.DoNotShow );
                        return false;
                    }
                    xmlOutput.StartOpenNode( "root", true ).NewLine( XmlLeadingWhitespace.None );
                    // loop over all the current top nodes and add the outerxml to the file for the non selected nodes
                    foreach ( KeyValuePair<uint, IEditedXmlNodeOrComment> kv in MainWindow.Instance.CurrentXmlTopNodesForVis )
                    {
                        if ( nodeOrComment.UID != kv.Key )
                        {
                            xmlOutput.NewLine( XmlLeadingWhitespace.IncludeAfter );
                            xmlOutput.AddCompleteNode( kv.Value.OuterXml );
                        }
                        else
                        {
                            xmlOutput.NewLine( XmlLeadingWhitespace.None );

                            #region ModifiedNode
                            XmlWriter editedNodeOutput = new XmlWriter( false );
                            //extra indent to match other nodes
                            editedNodeOutput.IncrementWhitespace();
                            editedNodeOutput.StartOpenNode( node.RelatedUnionNode.MetaDocument.NodeName, false );
                            foreach ( KeyValuePair<string, EditedXmlAttribute> att in node.Attributes )
                            {
                                if ( node.ChildNodes.Count > 0 && att.Key == "name" ) //subnode container, it doesn't need a newline after the only attribute it prints
                                    ReadAttributeFromEditedData( editedNodeOutput, att, ref justInsertedLineBreak, true );
                                else
                                    ReadAttributeFromEditedData( editedNodeOutput, att, ref justInsertedLineBreak, false );
                            }
                            editedNodeOutput.FinishOpenNode( true );
                            foreach ( EditedXmlNode subNode in node.ChildNodes.Cast<EditedXmlNode>() )
                            {
                                editedNodeOutput.StartOpenNode( subNode.XmlNodeTagName, false );
                                foreach ( KeyValuePair<string, EditedXmlAttribute> sAtt in subNode.Attributes )
                                {
                                    //ArcenDebugging.LogSingleLine( $"sAtt.Key = {sAtt.Key}!", Verbosity.DoNotShow );
                                    ReadAttributeFromEditedData( editedNodeOutput, sAtt, ref justInsertedLineBreak, true );
                                }
                                editedNodeOutput.FinishOpenNodeAndSelfClose();
                            }
                            editedNodeOutput.CloseNode( node.RelatedUnionNode.MetaDocument.NodeName, true, false );
                            #endregion

                            xmlOutput.AddCompleteNode( editedNodeOutput.GetFinishedXmlDocument() );
                        }
                        //ArcenDebugging.LogSingleLine( finalStringToWrite, Verbosity.DoNotShow );
                    }
                    xmlOutput.NewLine( XmlLeadingWhitespace.None );
                    xmlOutput.CloseNode( "root", false, true );
                }

                //write to file
                if ( node.RelatedUnionNode == null )
                {
                    ArcenDebugging.LogSingleLine( $"RelatedUnionNode inside Edited Node {node} is null. Can't save this node to file!", Verbosity.DoNotShow );
                    return false;
                }
                //add file name field in vis
                if ( isBackupFile )
                    File.WriteAllText( ProgramPermanentSettings.MainPath + @"\" + node.RelatedUnionNode.MetaDocument.MetadataFolder + @"\" +
                                      node.RelatedUnionNode.MetaDocument.MetadataFolder + ".tmp", xmlOutput.GetFinishedXmlDocument() );
                else
                    File.WriteAllText( ProgramPermanentSettings.MainPath + @"\" + node.RelatedUnionNode.MetaDocument.MetadataFolder + @"\" +
                                       node.RelatedUnionNode.MetaDocument.MetadataFolder + ".xml", xmlOutput.GetFinishedXmlDocument() );
                //ArcenDebugging.LogSingleLine( $"Step 5", Verbosity.DoNotShow );
                //ArcenDebugging.LogSingleLine( $"{node.RelatedUnionNode.MetaDocument.MetadataFolder}", Verbosity.DoNotShow );
            }
            return true;
        }

        private static bool ReadAttributeFromEditedData( XmlWriter xmlOutput, KeyValuePair<string, EditedXmlAttribute> att, ref bool justInsertedLineBreak, bool skipAttributeLeadOut = false )
        {
            //ArcenDebugging.LogSingleLine( $"Step 3.b.1", Verbosity.DoNotShow );
            MetaAttribute_Base? metaAttribute = att.Value.RelatedUnionAttribute?.MetaAttribute.Value;
            if ( metaAttribute == null )
            {
                ArcenDebugging.LogSingleLine( $"RelatedUnionAttribute inside Edited Attribute {att.Key} is null. Can't save this attribute to file!", Verbosity.DoNotShow );
                return false;
            }
            //ArcenDebugging.LogSingleLine( $"Before calc Att = {att.Key} \t\tjustInsertedLineBreak = {justInsertedLineBreak}", Verbosity.DoNotShow );
            string? effectiveValue = att.Value.GetEffectiveValue();
            if ( effectiveValue == null )
            {
                ArcenDebugging.LogSingleLine( $"GetEffectiveValue() inside Edited Attribute {att.Key} returned null. Can't save this attribute to file!", Verbosity.DoNotShow );
                return false;
            }
            //ArcenDebugging.LogSingleLine( $"att.Key = {att.Key}\t\teffectiveValue = {effectiveValue}", Verbosity.DoNotShow );
            //ArcenDebugging.LogSingleLine( $"Step 3.b.2", Verbosity.DoNotShow );
            XmlAttLeadInOut leadIn = CalculateLineBreakBefore( metaAttribute, ref justInsertedLineBreak, xmlOutput );
            //ArcenDebugging.LogSingleLine( $"After calc Att = {att.Key} \t\tjustInsertedLineBreak = {justInsertedLineBreak}", Verbosity.DoNotShow );
            switch ( att.Value.Type )
            {
                #region All Cases
                case AttributeType.Bool:
                    xmlOutput.BoolAttribute( leadIn, att.Key, bool.Parse( effectiveValue ) );
                    break;
                case AttributeType.BoolInt:
                case AttributeType.Int:
                    xmlOutput.IntAttribute( leadIn, att.Key, int.Parse( effectiveValue ) );
                    break;
                case AttributeType.String:
                case AttributeType.StringMultiLine:
                case AttributeType.ArbitraryString:
                case AttributeType.ArbitraryNode:
                case AttributeType.NodeList:
                case AttributeType.FolderList:
                    xmlOutput.StringAttribute( leadIn, att.Key, effectiveValue );
                    break;
                case AttributeType.Float:
                    xmlOutput.FloatAttribute( leadIn, att.Key, float.Parse( effectiveValue ), ((MetaAttribute_Float)metaAttribute).MinimumDigits );
                    break;
                case AttributeType.Point:
                    {
                        string? coordinates = effectiveValue;
                        if ( coordinates != null )
                        {
                            string[] splitCoord = coordinates.Split( "," );
                            xmlOutput.PointAttribute( leadIn, att.Key, ArcenPoint.Create( int.Parse( splitCoord[0] ), int.Parse( splitCoord[1] ) ) );
                        }
                    }
                    break;
                case AttributeType.Vector2:
                    {
                        string? coordinates = effectiveValue;
                        if ( coordinates != null )
                        {
                            string[] splitCoord = coordinates.Split( "," );
                            xmlOutput.Vector2Attribute( leadIn, att.Key, new Vector2( float.Parse( splitCoord[0] ), float.Parse( splitCoord[1] ) ),
                                                    ((MetaAttribute_Vector2)metaAttribute).x.MinimumDigits );
                        }
                    }
                    break;
                case AttributeType.Vector3:
                    {
                        string? coordinates = effectiveValue;
                        if ( coordinates != null )
                        {
                            string[] splitCoord = coordinates.Split( "," );
                            xmlOutput.Vector3Attribute( leadIn, att.Key, new Vector3( float.Parse( splitCoord[0] ), float.Parse( splitCoord[1] ), float.Parse( splitCoord[2] ) ),
                                                    ((MetaAttribute_Vector3)metaAttribute).x.MinimumDigits );
                        }
                    }
                    break;
                default:
                    ArcenDebugging.LogSingleLine( $"Unknown attribute type {att.Value.Type} during saving to xml", Verbosity.DoNotShow );
                    break;
                    #endregion
            }
            //ArcenDebugging.LogSingleLine( $"Step 4", Verbosity.DoNotShow );
            if ( leadIn == XmlAttLeadInOut.Linebreak )
                xmlOutput.IncrementPixelsOnCurrentLine( (ushort)metaAttribute.ContentWidthPx );
            if ( !skipAttributeLeadOut )
            {
                XmlAttLeadInOut leadInOut = CalculateLineBreakAfter( metaAttribute, ref justInsertedLineBreak );
                if ( leadInOut == XmlAttLeadInOut.Linebreak )
                    xmlOutput.HandleAttributeLeadInOut( leadInOut );
            }
            //ArcenDebugging.LogSingleLine( $"End calc Att = {att.Key} \t\tjustInsertedLineBreak = {justInsertedLineBreak}", Verbosity.DoNotShow );
            return true;
        }

        private static XmlAttLeadInOut CalculateLineBreakBefore( MetaAttribute_Base metaAttribute, ref bool justInsertedLineBreak, XmlWriter xmlWriter )
        {
            bool requireLineBreak = xmlWriter.CalculateIfNewLineIsRequired( (ushort)metaAttribute.ContentWidthPx );

            switch ( metaAttribute.LinebreakBefore )
            {
                case LineBreakType.Always:
                    if ( !justInsertedLineBreak )
                    {
                        justInsertedLineBreak = true;
                        return XmlAttLeadInOut.Linebreak;
                    }
                    else
                    {
                        //skipLeadInSpace = true;
                        justInsertedLineBreak = false;
                        return XmlAttLeadInOut.Space;
                    }
                case LineBreakType.PreferNot:
                    if ( requireLineBreak )
                    {
                        justInsertedLineBreak = true;
                        return XmlAttLeadInOut.Linebreak;
                    }
                    if ( justInsertedLineBreak )
                    {
                        justInsertedLineBreak = false;
                        return XmlAttLeadInOut.None;
                    }
                    justInsertedLineBreak = false;
                    return XmlAttLeadInOut.Space;
            }
            return XmlAttLeadInOut.Space;
        }

        private static XmlAttLeadInOut CalculateLineBreakAfter( MetaAttribute_Base metaAttribute, ref bool justInsertedLineBreak )
        {
            switch ( metaAttribute.LinebreakAfter )
            {
                case LineBreakType.Always:
                    justInsertedLineBreak = true;
                    return XmlAttLeadInOut.Linebreak;
                case LineBreakType.PreferNot:
                    justInsertedLineBreak = false;
                    return XmlAttLeadInOut.Space;
            }
            return XmlAttLeadInOut.Space;
        }
    }
}
