using System.Xml;
using ArcenXE.Utilities.XmlDataProcessing;
using ArcenXE.Universal;

namespace ArcenXE.Utilities.MetadataProcessing.BooleanLogic
{
    public static class BooleanLogicCheckerTreeParser
    {
        public static BooleanLogicCheckerTree? ProcessMetadataConditionalOuterShell( XmlElement element )
        {
            XmlNode? RootLogicalGroup = null;

            if ( element.ChildNodes.Count > 0 )
            {
                foreach ( XmlElement child in element.ChildNodes )
                    switch ( child.NodeType )
                    {
                        case XmlNodeType.Element:
                            if ( RootLogicalGroup == null )
                                RootLogicalGroup = child;
                            else
                                ArcenDebugging.LogSingleLine( "INFO: There should be only one \"or_group\" or \"and_group\" at the first depth of a conditional node in "
                                    + element.BaseURI + ". Only the first one will be used!", Verbosity.DoNotShow );
                            break;
                        case XmlNodeType.Comment: //ignore it
                            break;
                    }
            }
            else
            {
                ArcenDebugging.LogSingleLine( "ERROR: The conditional node \"" + element.GetAttribute( "name" ) + "\" has no Logical Groups underneath.", Verbosity.DoNotShow );
                return null;
            }
            if ( RootLogicalGroup != null ) // setting up the outer structure of the conditional node without the actual types inside
            {
                string rootName = RootLogicalGroup.Name.ToLowerInvariant();
                if ( rootName == "or_group" )
                {
                    BooleanLogicCheckerTree.OrGroup orGroup = new BooleanLogicCheckerTree.OrGroup();
                    return new BooleanLogicCheckerTree( element.GetAttribute( "name" ), orGroup );
                }
                else if ( rootName == "and_group" )
                {
                    BooleanLogicCheckerTree.AndGroup andGroup = new BooleanLogicCheckerTree.AndGroup();
                    return new BooleanLogicCheckerTree( element.GetAttribute( "name" ), andGroup );
                }
                else
                {
                    ArcenDebugging.LogSingleLine( "ERROR in ProcessMetadataConditionalOuterShell(): unknown RootLogicalGroup type: " + RootLogicalGroup.Name, Verbosity.ShowAsError );
                    return null;
                }
            }
            else
            {
                ArcenDebugging.LogSingleLine( "ERROR in ProcessMetadataConditionalOuterShell(): missing or unrecognized LogicGroup inside the conditional " + element.GetAttribute( "name" ), Verbosity.ShowAsError );
                return null;
            }
        }

        public static void ProcessMetadataConditionals( XmlElement element, BooleanLogicCheckerTree.LogicGroup logicGroup, Dictionary<string, MetaAttribute_Base> attributesData, bool firstRun = false )
        {
            XmlNode? RootLogicalGroup = null;
            if ( firstRun ) // this has to be done only the first call, not in the recursive ones
            {
                if ( element.ChildNodes.Count > 0 )
                {
                    foreach ( XmlElement child in element.ChildNodes )
                        switch ( child.NodeType )
                        {
                            case XmlNodeType.Element:
                                if ( RootLogicalGroup == null )
                                    RootLogicalGroup = child;
                                else
                                    ArcenDebugging.LogSingleLine( "INFO: There should be only one \"or_group\" or \"and_group\" at the first depth of the conditional named: "
                                        + element.GetAttribute( "name" ) + ". Only the first one will be used!", Verbosity.DoNotShow );
                                break;
                            case XmlNodeType.Comment: //ignore it
                                break;
                        }
                }
                else
                {
                    ArcenDebugging.LogSingleLine( "ERROR: The conditional node \"" + element.GetAttribute( "name" ) + "\" has no Logical Groups underneath.", Verbosity.DoNotShow );
                    return;
                }
            }
            else
                RootLogicalGroup = element;

            if ( RootLogicalGroup == null )
            {
                ArcenDebugging.LogSingleLine( "ERROR in ProcessMetadataConditional(): missing or unrecognized LogicGroup inside the conditional " + element.GetAttribute( "name" ), Verbosity.ShowAsError );
                return;
            }

            foreach ( XmlElement child in RootLogicalGroup.ChildNodes )
            {
                if ( child.NodeType == XmlNodeType.Element )
                {
                    string childName = child.Name.ToLowerInvariant();
                    if ( childName == "type" )
                    {
                        XmlAttributeCollection attributes = child.Attributes;
                        string? metadataAttributeKey = attributes.GetNamedItem( "attribute" )?.Value?.ToLowerInvariant();
                        if ( metadataAttributeKey != null )
                        {
                            attributesData.TryGetValue( metadataAttributeKey, out MetaAttribute_Base? attribute );
                            if ( attribute != null )
                            {
                                AttributeType type = attribute.Type;

                                BooleanLogicType logicType;
                                string? condType = attributes.GetNamedItem( "condition_type" )?.Value;
                                if ( condType != null )
                                    logicType = (BooleanLogicType)Enum.Parse( typeof( BooleanLogicType ), condType );
                                else
                                {
                                    ArcenDebugging.LogSingleLine( "ERROR: condition_type inside one of the types in " + element.GetAttribute( "name" ) + " is null!", Verbosity.DoNotShow );
                                    return;
                                }

                                string? valueString = attributes.GetNamedItem( "value" )?.Value;
                                if ( valueString == null )
                                {
                                    ArcenDebugging.LogSingleLine( "ERROR: value inside one of the types in " + element.GetAttribute( "name" ) + " is null!", Verbosity.DoNotShow );
                                    return;
                                }

                                switch ( type )
                                {
                                    #region Bool
                                    case AttributeType.Bool:
                                        logicGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.BoolChecker( bool.Parse( valueString ), logicType, () =>
                                        {
                                            if ( MainWindow.Instance.XmlElementCurrentlyBeingEdited is EditedXmlNode node )
                                            {
                                                if ( node.Attributes.TryGetValue( attribute.Key, out EditedXmlAttribute? att ) && att.ValueOnDisk != null )
                                                    return bool.Parse( att.ValueOnDisk );
                                                else
                                                {
                                                    ArcenDebugging.LogSingleLine( "ERROR: Failed to retrieve attribute with Key: " + attribute.Key
                                                                                  + "This should't happen at all!", Verbosity.DoNotShow );
                                                    return false;
                                                }
                                            }
                                            else
                                            {
                                                ArcenDebugging.LogSingleLine( "ERROR: Failed to retrieve CurrentlyEditedNode from Main Thread. Where did it go?", Verbosity.DoNotShow );
                                                return false;
                                            }
                                        } ) );
                                        break;
                                    #endregion

                                    #region String
                                    case AttributeType.String:
                                    case AttributeType.StringMultiLine:
                                    case AttributeType.ArbitraryString:
                                    case AttributeType.ArbitraryNode:
                                    case AttributeType.NodeList:
                                    case AttributeType.FolderList:
                                        logicGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.StringChecker( valueString, logicType, () =>
                                        {
                                            EditedXmlNode? node = MainWindow.Instance.XmlElementCurrentlyBeingEdited as EditedXmlNode;
                                            if ( node != null )
                                            {
                                                if ( node.Attributes.TryGetValue( attribute.Key, out EditedXmlAttribute? att ) && att.ValueOnDisk != null )
                                                    return att.ValueOnDisk;
                                                else
                                                {
                                                    ArcenDebugging.LogSingleLine( "ERROR: Failed to retrieve attribute with Key: " + attribute.Key
                                                                                  + "This should't happen at all!", Verbosity.DoNotShow );
                                                    return string.Empty;
                                                }
                                            }
                                            else
                                            {
                                                ArcenDebugging.LogSingleLine( "ERROR: Failed to retrieve CurrentlyEditedNode from Main Thread. Where did it go?", Verbosity.DoNotShow );
                                                return string.Empty;
                                            }
                                        } ) );
                                        break;
                                    #endregion

                                    #region Int
                                    case AttributeType.BoolInt:
                                    case AttributeType.Int:
                                    case AttributeType.Point:
                                        logicGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.IntChecker( int.Parse( valueString ), logicType, () =>
                                        {
                                            EditedXmlNode? node = MainWindow.Instance.XmlElementCurrentlyBeingEdited as EditedXmlNode;
                                            if ( node != null )
                                            {
                                                if ( node.Attributes.TryGetValue( attribute.Key, out EditedXmlAttribute? att ) && att.ValueOnDisk != null )
                                                {
                                                    return int.Parse( att.ValueOnDisk );
                                                }
                                                else
                                                {
                                                    ArcenDebugging.LogSingleLine( "ERROR: Failed to retrieve attribute with Key: " + attribute.Key
                                                                                  + "This should't happen at all!", Verbosity.DoNotShow );
                                                    return int.MinValue;
                                                }
                                            }
                                            else
                                            {
                                                ArcenDebugging.LogSingleLine( "ERROR: Failed to retrieve CurrentlyEditedNode from Main Thread. Where did it go?", Verbosity.DoNotShow );
                                                return int.MinValue;
                                            }
                                        } ) );
                                        break;
                                    #endregion

                                    #region Float
                                    case AttributeType.Float:
                                    case AttributeType.Vector2:
                                    case AttributeType.Vector3:
                                        if ( FloatExtensions.TryParsePrecise( valueString, out float val ) )
                                        {
                                            logicGroup.DirectCheckers.Add( new BooleanLogicCheckerTree.FloatChecker( val, logicType, () =>
                                            {
                                                EditedXmlNode? node = MainWindow.Instance.XmlElementCurrentlyBeingEdited as EditedXmlNode;
                                                if ( node != null )
                                                {
                                                    if ( node.Attributes.TryGetValue( attribute.Key, out EditedXmlAttribute? att ) && att.ValueOnDisk != null )
                                                    {
                                                        if ( FloatExtensions.TryParsePrecise( att.ValueOnDisk, out float value ) )
                                                        {
                                                            return value;
                                                        }
                                                        else
                                                        {
                                                            ArcenDebugging.LogSingleLine( "ERROR: Failed to parse att.Value to a Float", Verbosity.DoNotShow );
                                                            return float.NaN;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        ArcenDebugging.LogSingleLine( "ERROR: Failed to retrieve attribute with Key: " + attribute.Key
                                                                                      + "This should't happen at all!", Verbosity.DoNotShow );
                                                        return float.NaN;
                                                    }
                                                }
                                                else
                                                {
                                                    ArcenDebugging.LogSingleLine( "ERROR: Failed to retrieve CurrentlyEditedNode from Main Thread. Where did it go?", Verbosity.DoNotShow );
                                                    return float.NaN;
                                                }
                                            } ) );

                                        }
                                        else
                                        {
                                            ArcenDebugging.LogSingleLine( "ERROR: Failed to parse valueString to a Float", Verbosity.DoNotShow );
                                            return;
                                        }
                                        break;
                                    #endregion

                                    default:
                                        ArcenDebugging.LogSingleLine( "Unknown AttributeType: " + type.ToString(), Verbosity.DoNotShow );
                                        break;
                                }
                            }
                            else
                            {
                                ArcenDebugging.LogSingleLine( "ERROR: attribute with name " + metadataAttributeKey + " in " + element.GetAttribute( "name" ) 
                                                              + " is null! This should't happen at all!", Verbosity.DoNotShow );
                                return;
                            }
                        }
                        else
                        {
                            ArcenDebugging.LogSingleLine( "ERROR: attribute inside one of the types in " + element.GetAttribute( "name" ) + " is null!", Verbosity.DoNotShow );
                            return;
                        }
                    }
                    else
                    {
                        if ( childName == "or_group" )
                        {
                            BooleanLogicCheckerTree.OrGroup group = new BooleanLogicCheckerTree.OrGroup();
                            ProcessMetadataConditionals( child, group, attributesData );
                            logicGroup.SubGroups.Add( group );
                        }
                        else if ( childName == "and_group" )
                        {
                            BooleanLogicCheckerTree.AndGroup group = new BooleanLogicCheckerTree.AndGroup();
                            ProcessMetadataConditionals( child, group, attributesData );
                            logicGroup.SubGroups.Add( group );
                        }
                        else
                        {
                            ArcenDebugging.LogSingleLine( "Error in ProcessMetadataConditional(): unknown child type: " + child.Name +
                                ".\nThis element will be ignored.", Verbosity.DoNotShow );
                            return;
                        }
                    }
                }
            }
        }
    }
}

