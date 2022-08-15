using System.Xml;
using ArcenXE.Universal;
using ArcenXE.Utilities.XmlDataProcessing;

namespace ArcenXE.Utilities.MetadataProcessing
{
    public static class MetadataAttributeParser
    {
        public static void ProcessMetadataAttributes( XmlElement element, MetadataDocument doc, out MetaAttribute_Base? result )
        {
            XmlAttributeCollection originalAttributes = element.Attributes;

            if ( originalAttributes.Count > 0 )
            {
                Dictionary<string, MetaAttributeToBeRead> attributes = new Dictionary<string, MetaAttributeToBeRead>();
                //dump2.Add( " DUMP2 HEADER: " + element.GetAttribute( "key" ) );
                foreach ( XmlAttribute att in originalAttributes )
                {
                    MetaAttributeToBeRead xmlAttributeToRead = new MetaAttributeToBeRead
                    {
                        Name = att.Name.ToLowerInvariant(),
                        Value = att.Value
                    };
                    attributes[xmlAttributeToRead.Name] = xmlAttributeToRead;
                    //dump2.Add( att.Name + "  " + attributes.Count );
                }

                if ( !attributes.TryGetValue( "type", out MetaAttributeToBeRead? mainAttributeType ) )
                {
                    ArcenDebugging.LogSingleLine( "ERROR: Required attribute \"type\" in file " + doc.MetadataName + " is missing. You must provide one.", Verbosity.DoNotShow );
                    result = null;
                    return;
                }

                if ( mainAttributeType != null )
                    switch ( mainAttributeType.Value )
                    {
                        #region Bool
                        case "bool":
                            {
                                MetaAttribute_Bool metaAttribute_Bool = new MetaAttribute_Bool();
                                CommonAttributeMetaDataReader( metaAttribute_Bool, attributes, doc );
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_Bool.Default = bool.Parse( attribute.Value );
                                result = metaAttribute_Bool;
                            }
                            break;
                        #endregion
                        #region BoolInt
                        case "int-bool":
                            {
                                MetaAttribute_BoolInt metaAttribute_BoolInt = new MetaAttribute_BoolInt();
                                CommonAttributeMetaDataReader( metaAttribute_BoolInt, attributes, doc );
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_BoolInt.Default = int.Parse( attribute.Value );
                                result = metaAttribute_BoolInt;
                            }
                            break;
                        #endregion
                        #region String
                        case "string":
                            MetaAttribute_String metaAttribute_String = new MetaAttribute_String();
                            CommonAttributeMetaDataReader( metaAttribute_String, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_String.Default = attribute.Value;
                            }
                            {
                                if ( attributes.TryGetValue( "minlength", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_String.MinLength = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "maxlength", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_String.MaxLength = int.Parse( attribute.Value );
                            }
                            result = metaAttribute_String;

                            break;
                        #endregion
                        #region StringMultiLine
                        case "string-multiline":
                            MetaAttribute_StringMultiline metaAttribute_StringMultiline = new MetaAttribute_StringMultiline();
                            CommonAttributeMetaDataReader( metaAttribute_StringMultiline, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_StringMultiline.Default = attribute.Value;
                            }
                            {
                                if ( attributes.TryGetValue( "minlength", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_StringMultiline.MinLength = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "maxlength", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_StringMultiline.MaxLength = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "show_lines", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_StringMultiline.ShowLines = int.Parse( attribute.Value );
                            }
                            //throw new Exception( "Unknown attribute " + attribute.Name + " in type " + type );
                            result = metaAttribute_StringMultiline;
                            break;
                        #endregion
                        #region ArbitraryString
                        case "string-dropdown":
                            MetaAttribute_ArbitraryString metaAttribute_ArbitraryString = new MetaAttribute_ArbitraryString();
                            CommonAttributeMetaDataReader( metaAttribute_ArbitraryString, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_ArbitraryString.Default = attribute.Value;
                            }
                            XmlNodeList subNodes = element.ChildNodes; //"option" only
                            if ( subNodes.Count > 0 )
                                foreach ( XmlNode subNode in subNodes )
                                    if ( subNode.NodeType == XmlNodeType.Element )
                                        if ( subNode.Name.ToLowerInvariant() == "option" )
                                            metaAttribute_ArbitraryString.Options.Add( subNode.InnerText );
                                        else
                                            ArcenDebugging.LogSingleLine( "Why do we have a " + subNode.Name + " as subNode?", Verbosity.DoNotShow );
                                    else if ( subNode.NodeType != XmlNodeType.Comment ) // ignore comments
                                        ArcenDebugging.LogSingleLine( "Why do we have a " + subNode.NodeType + " directly under the element node?", Verbosity.DoNotShow );
                            result = metaAttribute_ArbitraryString;
                            break;
                        #endregion
                        #region Int
                        case "int-textbox":
                            MetaAttribute_Int metaAttribute_Int = new MetaAttribute_Int();
                            CommonAttributeMetaDataReader( metaAttribute_Int, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_Int.Default = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "min", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_Int.Min = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "max", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_Int.Max = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "minimum_digits", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_Int.MinimumDigits = int.Parse( attribute.Value );
                            }
                            result = metaAttribute_Int;
                            break;
                        #endregion
                        #region Float
                        case "float-textbox":
                            MetaAttribute_Float metaAttribute_Float = new MetaAttribute_Float();
                            CommonAttributeMetaDataReader( metaAttribute_Float, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                    if ( FloatExtensions.TryParsePrecise( attribute.Value, out float temp ) )
                                        metaAttribute_Float.Default = temp;
                            }
                            {
                                if ( attributes.TryGetValue( "min", out MetaAttributeToBeRead? attribute ) )
                                    if ( FloatExtensions.TryParsePrecise( attribute.Value, out float temp ) )
                                        metaAttribute_Float.Min = temp;
                            }
                            {
                                if ( attributes.TryGetValue( "max", out MetaAttributeToBeRead? attribute ) )
                                    if ( FloatExtensions.TryParsePrecise( attribute.Value, out float temp ) )
                                        metaAttribute_Float.Max = temp;
                            }
                            {
                                if ( attributes.TryGetValue( "precision", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_Float.Precision = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "minimum_digits", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_Float.MinimumDigits = int.Parse( attribute.Value );
                            }
                            result = metaAttribute_Float;
                            break;
                        #endregion
                        #region ArbitraryNode
                        case "node-dropdown":
                            MetaAttribute_ArbitraryNode metaAttribute_ArbitraryNode = new MetaAttribute_ArbitraryNode();
                            CommonAttributeMetaDataReader( metaAttribute_ArbitraryNode, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_ArbitraryNode.Default = attribute.Value;
                            }
                            {
                                if ( attributes.TryGetValue( "node_source", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_ArbitraryNode.NodeSource = attribute.Value;
                            }
                            result = metaAttribute_ArbitraryNode;
                            break;
                        #endregion
                        #region NodeList
                        case "node-list":
                            MetaAttribute_NodeList metaAttribute_NodeList = new MetaAttribute_NodeList();
                            CommonAttributeMetaDataReader( metaAttribute_NodeList, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] defaults = attribute.Value.Split( ',' );
                                    foreach ( string s in defaults )
                                        metaAttribute_NodeList.Defaults.Add( s );
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "node_source", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_NodeList.NodeSource = attribute.Value;
                            }
                            result = metaAttribute_NodeList;
                            break;
                        #endregion
                        #region FolderList
                        case "folder-list":
                            MetaAttribute_FolderList metaAttribute_FolderList = new MetaAttribute_FolderList();
                            CommonAttributeMetaDataReader( metaAttribute_FolderList, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "folder_source", out MetaAttributeToBeRead? attribute ) )
                                    metaAttribute_FolderList.FolderSource = attribute.Value;
                            }
                            result = metaAttribute_FolderList;
                            break;
                        #endregion
                        #region Point
                        case "point -textbox":
                            MetaAttribute_Point metaAttribute_Point = new MetaAttribute_Point();
                            CommonAttributeMetaDataReader( metaAttribute_Point, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] defaults = attribute.Value.Split( ',' );
                                    metaAttribute_Point.x.Default = int.Parse( defaults[0] );
                                    metaAttribute_Point.y.Default = int.Parse( defaults[1] );
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "min", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] minimums = attribute.Value.Split( ',' );
                                    metaAttribute_Point.x.Min = int.Parse( minimums[0] );
                                    metaAttribute_Point.y.Min = int.Parse( minimums[1] );
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "max", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] maximums = attribute.Value.Split( ',' );
                                    metaAttribute_Point.x.Max = int.Parse( maximums[0] );
                                    metaAttribute_Point.y.Max = int.Parse( maximums[1] );
                                }
                            }
                            result = metaAttribute_Point;
                            break;
                        #endregion
                        #region Vector2
                        case "vector2-textbox":
                            MetaAttribute_Vector2 metaAttribute_Vector2 = new MetaAttribute_Vector2();
                            CommonAttributeMetaDataReader( metaAttribute_Vector2, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] defaults = attribute.Value.Split( ',' );
                                    float[] values = new float[2];
                                    if ( FloatExtensions.TryParsePrecise( defaults[0], out values[0] ) )
                                        metaAttribute_Vector2.x.Default = values[0];
                                    if ( FloatExtensions.TryParsePrecise( defaults[1], out values[1] ) )
                                        metaAttribute_Vector2.y.Default = values[1];
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "min", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] minimums = attribute.Value.Split( ',' );
                                    float[] values = new float[2];
                                    if ( FloatExtensions.TryParsePrecise( minimums[0], out values[0] ) )
                                        metaAttribute_Vector2.x.Min = values[0];
                                    if ( FloatExtensions.TryParsePrecise( minimums[1], out values[1] ) )
                                        metaAttribute_Vector2.y.Min = values[1];
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "max", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] maximums = attribute.Value.Split( ',' );
                                    float[] values = new float[2];
                                    if ( FloatExtensions.TryParsePrecise( maximums[0], out values[0] ) )
                                        metaAttribute_Vector2.x.Max = values[0];
                                    if ( FloatExtensions.TryParsePrecise( maximums[1], out values[1] ) )
                                        metaAttribute_Vector2.y.Max = values[1];
                                }
                            }
                            result = metaAttribute_Vector2;
                            break;
                        #endregion
                        #region Vector3
                        case "vector3-textbox":
                            MetaAttribute_Vector3 metaAttribute_Vector3 = new MetaAttribute_Vector3();
                            CommonAttributeMetaDataReader( metaAttribute_Vector3, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] defaults = attribute.Value.Split( ',' );
                                    float[] values = new float[3];
                                    if ( FloatExtensions.TryParsePrecise( defaults[0], out values[0] ) )
                                        metaAttribute_Vector3.x.Default = values[0];
                                    if ( FloatExtensions.TryParsePrecise( defaults[1], out values[1] ) )
                                        metaAttribute_Vector3.y.Default = values[1];
                                    if ( FloatExtensions.TryParsePrecise( defaults[2], out values[2] ) )
                                        metaAttribute_Vector3.z.Default = values[2];
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "min", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] minimums = attribute.Value.Split( ',' );
                                    float[] values = new float[3];
                                    if ( FloatExtensions.TryParsePrecise( minimums[0], out values[0] ) )
                                        metaAttribute_Vector3.x.Min = values[0];
                                    if ( FloatExtensions.TryParsePrecise( minimums[1], out values[1] ) )
                                        metaAttribute_Vector3.y.Min = values[1];
                                    if ( FloatExtensions.TryParsePrecise( minimums[2], out values[2] ) )
                                        metaAttribute_Vector3.z.Min = values[2];
                                }
                            }

                            {
                                if ( attributes.TryGetValue( "max", out MetaAttributeToBeRead? attribute ) )
                                {
                                    string[] maximums = attribute.Value.Split( ',' );
                                    float[] values = new float[3];
                                    if ( FloatExtensions.TryParsePrecise( maximums[0], out values[0] ) )
                                        metaAttribute_Vector3.x.Max = values[0];
                                    if ( FloatExtensions.TryParsePrecise( maximums[1], out values[1] ) )
                                        metaAttribute_Vector3.y.Max = values[1];
                                    if ( FloatExtensions.TryParsePrecise( maximums[2], out values[2] ) )
                                        metaAttribute_Vector3.z.Max = values[2];
                                }
                            }
                            result = metaAttribute_Vector3;
                            break;
                        #endregion
                        default:
                            ArcenDebugging.LogSingleLine( "Unknown attribute type: " + mainAttributeType.Value, Verbosity.DoNotShow );
                            result = null;
                            return;

                    }
                else
                {
                    result = null;
                    ArcenDebugging.LogSingleLine( "No Main Attribute Type found at all. File name:" + doc.MetadataName + "\nElement: " + element.OuterXml, Verbosity.DoNotShow );
                }
                List<string> missing = new List<string>();
                foreach ( MetaAttributeToBeRead attribute in attributes.Values )
                    if ( !attribute.HasReadValue )
                        missing.Add( attribute.Name );
                if ( missing.Count > 0 )
                {
                    string missingToDisplay = string.Empty;
                    foreach ( string attributeName in missing )
                        missingToDisplay += "    " + attributeName + "\n";
                    ArcenDebugging.LogSingleLine( "Attributes:\n" + missingToDisplay + "haven't been read! File name:" + doc.MetadataName + "\nElement: " + element.OuterXml, Verbosity.DoNotShow );
                }
            }
            else
            {
                result = null;
                ArcenDebugging.LogSingleLine( "No attributes found at all. File name:" + doc.MetadataName + "\nElement: " + element.OuterXml, Verbosity.DoNotShow );
            }
        }

        private static void CommonAttributeMetaDataReader( MetaAttribute_Base metaAttribute, Dictionary<string, MetaAttributeToBeRead> attributes, MetadataDocument doc )
        {
            {
                if ( attributes.TryGetValue( "key", out MetaAttributeToBeRead? attribute ) )
                    metaAttribute.Key = attribute.Value;
                else
                    ArcenDebugging.LogSingleLine( "WARNING: Required attribute \"key\" in file " + doc.MetadataName + " is missing.", Verbosity.DoNotShow );
            }
            {
                if ( attributes.TryGetValue( "is_required", out MetaAttributeToBeRead? attribute ) ) 
                    metaAttribute.IsRequired = bool.Parse( attribute.Value );
                else { }
            }
            {
                if ( attributes.TryGetValue( "is_central_identifier", out MetaAttributeToBeRead? attribute ) ) // move to doc
                {
                    if ( attribute.Value.ToLowerInvariant() == "true" )
                    {
                        if ( !doc.IsDataCopyIdentifierAlreadyRead )
                        {
                            doc.CentralID = metaAttribute;
                            metaAttribute.IsCentralIdentifier = bool.Parse( attribute.Value );
                            doc.IsDataCopyIdentifierAlreadyRead = true;
                        }
                        else
                            ArcenDebugging.LogSingleLine( "There is more than 1 IsCentralIdentifier inside of metadata " + doc.MetadataName, Verbosity.DoNotShow );
                    }
                }
                else { } //do nothing, because this field is optional (there has to be 1 per file)
            }
            {
                if ( attributes.TryGetValue( "is_partial_identifier", out MetaAttributeToBeRead? attribute ) )
                {
                    doc.PartialId = metaAttribute;
                    metaAttribute.IsPartialIdentifier = bool.Parse( attribute.Value );
                }
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "is_data_copy_identifier", out MetaAttributeToBeRead? attribute ) )
                {
                    doc.DataCopyId = metaAttribute;
                    metaAttribute.IsDataCopyIdentifier = bool.Parse( attribute.Value );
                }
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "causes_all_fields_to_be_optional_except_central_identifier", out MetaAttributeToBeRead? attribute ) )
                    metaAttribute.CausesAllFieldsToBeOptionalExceptCentralIdentifier = bool.Parse( attribute.Value );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "is_description", out MetaAttributeToBeRead? attribute ) )
                {
                    doc.Description = metaAttribute;
                    metaAttribute.IsDescription = bool.Parse( attribute.Value );
                }
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "is_localized", out MetaAttributeToBeRead? attribute ) )
                    metaAttribute.IsLocalized = bool.Parse( attribute.Value );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "content_width_px", out MetaAttributeToBeRead? attribute ) )
                    metaAttribute.ContentWidthPx = int.Parse( attribute.Value );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "linebreak_before", out MetaAttributeToBeRead? attribute ) )
                    metaAttribute.LinebreakBefore = (LineBreakType)Enum.Parse( typeof( LineBreakType ), attribute.Value, true );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "linebreak_after", out MetaAttributeToBeRead? attribute ) )
                    metaAttribute.LinebreakAfter = (LineBreakType)Enum.Parse( typeof( LineBreakType ), attribute.Value, true );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "only_exists_if_conditional_passes", out MetaAttributeToBeRead? attribute ) )
                    metaAttribute.OnlyExistsIfConditionalPasses = attribute.Value;
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "is_user_facing_name", out MetaAttributeToBeRead? attribute ) )
                {
                    doc.UserFacingName = metaAttribute;
                    metaAttribute.IsUserFacingName = bool.Parse( attribute.Value );
                }
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "tooltip", out MetaAttributeToBeRead? attribute ) )
                    metaAttribute.Tooltip = attribute.Value;
                else { } //do nothing, because this field is optional
            }
        }

        private class MetaAttributeToBeRead
        {
            public string Name = string.Empty;
            private string value = string.Empty;
            public string Value
            {
                get
                {
                    this.HasReadValue = true;
                    return value;
                }
                set => this.value = value;
            }
            public bool HasReadValue { get; private set; }
        }
    }
}
