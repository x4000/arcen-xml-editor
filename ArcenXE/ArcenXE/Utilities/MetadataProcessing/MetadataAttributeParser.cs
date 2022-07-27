using System.Xml;
using ArcenXE.Universal;

namespace ArcenXE.Utilities.MetadataProcessing
{
    public static class MetadataAttributeParser
    {
        public static void ProcessMetadataAttributes( XmlElement element, MetadataDocument doc, out AttributeData_Base? result )
        {
            XmlAttributeCollection originalAttributes = element.Attributes;

            if ( originalAttributes.Count > 0 )
            {
                Dictionary<string, XmlAttributeToRead> attributes = new Dictionary<string, XmlAttributeToRead>();
                //dump2.Add( " DUMP2 HEADER: " + element.GetAttribute( "key" ) );
                foreach ( XmlAttribute att in originalAttributes )
                {
                    XmlAttributeToRead xmlAttributeToRead = new XmlAttributeToRead
                    {
                        Name = att.Name.ToLowerInvariant(),
                        Value = att.Value.ToLowerInvariant()
                    };
                    attributes[xmlAttributeToRead.Name] = xmlAttributeToRead;
                    //dump2.Add( att.Name + "  " + attributes.Count );
                }

                if ( !attributes.TryGetValue( "type", out XmlAttributeToRead? mainAttributeType ) )
                {
                    ArcenDebugging.LogSingleLine( "ERROR: Required attribute \"type\" in file " + doc.Name + " is missing. You must provide one.", Verbosity.DoNotShow );
                    result = null;
                    return;
                }

                if ( mainAttributeType != null )
                    switch ( mainAttributeType.Value )
                    {
                        #region Bool
                        case "bool":
                            {
                                AttributeData_Bool attributeData_Bool = new AttributeData_Bool();
                                SpiffyBaseAttributesReader( attributeData_Bool, attributes, doc );
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                    attributeData_Bool.Default = bool.Parse( attribute.Value );
                                result = attributeData_Bool;

                            }
                            break;
                        #endregion
                        #region BoolInt
                        case "int-bool":
                            {
                                AttributeData_BoolInt attributeData_BoolInt = new AttributeData_BoolInt();
                                SpiffyBaseAttributesReader( attributeData_BoolInt, attributes, doc );
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                    attributeData_BoolInt.Default = int.Parse( attribute.Value );
                                result = attributeData_BoolInt;
                            }

                            break;
                        #endregion
                        #region String
                        case "string":
                            AttributeData_String attributeData_String = new AttributeData_String();
                            SpiffyBaseAttributesReader( attributeData_String, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                    attributeData_String.Default = attribute.Value;
                            }
                            {
                                if ( attributes.TryGetValue( "minlength", out XmlAttributeToRead? attribute ) )
                                    attributeData_String.MinLength = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "maxlength", out XmlAttributeToRead? attribute ) )
                                    attributeData_String.MaxLength = int.Parse( attribute.Value );
                            }
                            result = attributeData_String;

                            break;
                        #endregion
                        #region StringMultiLine
                        case "string-multiline":
                            AttributeData_StringMultiline attributeData_StringMultiline = new AttributeData_StringMultiline();
                            SpiffyBaseAttributesReader( attributeData_StringMultiline, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                    attributeData_StringMultiline.Default = attribute.Value;
                            }
                            {
                                if ( attributes.TryGetValue( "minlength", out XmlAttributeToRead? attribute ) )
                                    attributeData_StringMultiline.MinLength = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "maxlength", out XmlAttributeToRead? attribute ) )
                                    attributeData_StringMultiline.MaxLength = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "show_lines", out XmlAttributeToRead? attribute ) )
                                    attributeData_StringMultiline.ShowLines = int.Parse( attribute.Value );
                            }
                            //throw new Exception( "Unknown attribute " + attribute.Name + " in type " + type );
                            result = attributeData_StringMultiline;
                            break;
                        #endregion
                        #region ArbitraryString
                        case "string-dropdown":
                            AttributeData_ArbitraryString attributeData_ArbitraryString = new AttributeData_ArbitraryString();
                            SpiffyBaseAttributesReader( attributeData_ArbitraryString, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                    attributeData_ArbitraryString.Default = attribute.Value;
                            }
                            XmlNodeList subNodes = element.ChildNodes; //"option" only
                            if ( subNodes.Count > 0 )
                                foreach ( XmlNode subNode in subNodes )
                                    if ( subNode.NodeType == XmlNodeType.Element )
                                        if ( subNode.Name.ToLowerInvariant() == "option" )
                                            attributeData_ArbitraryString.Strings.Add( subNode.InnerText );
                                        else
                                            ArcenDebugging.LogSingleLine( "Why do we have a " + subNode.Name + " as subNode?", Verbosity.DoNotShow );
                                    else if ( subNode.NodeType != XmlNodeType.Comment ) // ignore comments
                                        ArcenDebugging.LogSingleLine( "Why do we have a " + subNode.NodeType + " directly under the element node?", Verbosity.DoNotShow );
                            result = attributeData_ArbitraryString;
                            break;
                        #endregion
                        #region Int
                        case "int-textbox":
                            AttributeData_Int attributeData_Int = new AttributeData_Int();
                            SpiffyBaseAttributesReader( attributeData_Int, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                    attributeData_Int.Default = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "min", out XmlAttributeToRead? attribute ) )
                                    attributeData_Int.Min = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "max", out XmlAttributeToRead? attribute ) )
                                    attributeData_Int.Max = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "minimum_digits", out XmlAttributeToRead? attribute ) )
                                    attributeData_Int.MinimumDigits = int.Parse( attribute.Value );
                            }
                            result = attributeData_Int;
                            break;
                        #endregion
                        #region Float
                        case "float-textbox":
                            AttributeData_Float attributeData_Float = new AttributeData_Float();
                            SpiffyBaseAttributesReader( attributeData_Float, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                    if ( FloatExtensions.TryParsePrecise( attribute.Value, out float temp ) )
                                        attributeData_Float.Default = temp;
                            }
                            {
                                if ( attributes.TryGetValue( "min", out XmlAttributeToRead? attribute ) )
                                    if ( FloatExtensions.TryParsePrecise( attribute.Value, out float temp ) )
                                        attributeData_Float.Min = temp;
                            }
                            {
                                if ( attributes.TryGetValue( "max", out XmlAttributeToRead? attribute ) )
                                    if ( FloatExtensions.TryParsePrecise( attribute.Value, out float temp ) )
                                        attributeData_Float.Max = temp;
                            }
                            {
                                if ( attributes.TryGetValue( "precision", out XmlAttributeToRead? attribute ) )
                                    attributeData_Float.Precision = int.Parse( attribute.Value );
                            }
                            {
                                if ( attributes.TryGetValue( "minimum_digits", out XmlAttributeToRead? attribute ) )
                                    attributeData_Float.MinimumDigits = int.Parse( attribute.Value );
                            }
                            result = attributeData_Float;
                            break;
                        #endregion
                        #region ArbitraryNode
                        case "node-dropdown":
                            AttributeData_ArbitraryNode attributeData_ArbitraryNode = new AttributeData_ArbitraryNode();
                            SpiffyBaseAttributesReader( attributeData_ArbitraryNode, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                    attributeData_ArbitraryNode.Default.Name = attribute.Value;
                            }
                            {
                                if ( attributes.TryGetValue( "node_source", out XmlAttributeToRead? attribute ) )
                                    attributeData_ArbitraryNode.NodeSource = attribute.Value;
                            }
                            result = attributeData_ArbitraryNode;
                            break;
                        #endregion
                        #region NodeList
                        case "node-list":
                            AttributeData_NodeList attributeData_NodeList = new AttributeData_NodeList();
                            SpiffyBaseAttributesReader( attributeData_NodeList, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                    attributeData_NodeList.Default.Name = attribute.Value;
                            }
                            {
                                if ( attributes.TryGetValue( "node_source", out XmlAttributeToRead? attribute ) )
                                    attributeData_NodeList.NodeSource = attribute.Value;
                            }
                            //todo
                            result = attributeData_NodeList;
                            break;
                        #endregion
                        #region FolderList
                        case "folder-list":
                            AttributeData_FolderList attributeData_FolderList = new AttributeData_FolderList();
                            SpiffyBaseAttributesReader( attributeData_FolderList, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "folder_source", out XmlAttributeToRead? attribute ) )
                                    attributeData_FolderList.FoldeSource = attribute.Value;
                            }
                            result = attributeData_FolderList;
                            break;
                        #endregion
                        #region Point
                        case "point-textbox":
                            AttributeData_Point attributeData_Point = new AttributeData_Point();
                            SpiffyBaseAttributesReader( attributeData_Point, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                {
                                    string[] defaults = attribute.Value.Split( ',' );
                                    attributeData_Point.x.Default = int.Parse( defaults[0] );
                                    attributeData_Point.y.Default = int.Parse( defaults[1] );
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "min", out XmlAttributeToRead? attribute ) )
                                {
                                    string[] minimums = attribute.Value.Split( ',' );
                                    attributeData_Point.x.Min = int.Parse( minimums[0] );
                                    attributeData_Point.y.Min = int.Parse( minimums[1] );
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "max", out XmlAttributeToRead? attribute ) )
                                {
                                    string[] maximums = attribute.Value.Split( ',' );
                                    attributeData_Point.x.Max = int.Parse( maximums[0] );
                                    attributeData_Point.y.Max = int.Parse( maximums[1] );
                                }
                            }
                            result = attributeData_Point;
                            break;
                        #endregion
                        #region Vector2
                        case "vector2-textbox":
                            AttributeData_Vector2 attributeData_Vector2 = new AttributeData_Vector2();
                            SpiffyBaseAttributesReader( attributeData_Vector2, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                {
                                    string[] defaults = attribute.Value.Split( ',' );
                                    float[] values = new float[2];
                                    if ( FloatExtensions.TryParsePrecise( defaults[0], out values[0] ) )
                                        attributeData_Vector2.x.Default = values[0];
                                    if ( FloatExtensions.TryParsePrecise( defaults[1], out values[1] ) )
                                        attributeData_Vector2.y.Default = values[1];
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "min", out XmlAttributeToRead? attribute ) )
                                {
                                    string[] minimums = attribute.Value.Split( ',' );
                                    float[] values = new float[2];
                                    if ( FloatExtensions.TryParsePrecise( minimums[0], out values[0] ) )
                                        attributeData_Vector2.x.Min = values[0];
                                    if ( FloatExtensions.TryParsePrecise( minimums[1], out values[1] ) )
                                        attributeData_Vector2.y.Min = values[1];
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "max", out XmlAttributeToRead? attribute ) )
                                {
                                    string[] maximums = attribute.Value.Split( ',' );
                                    float[] values = new float[2];
                                    if ( FloatExtensions.TryParsePrecise( maximums[0], out values[0] ) )
                                        attributeData_Vector2.x.Max = values[0];
                                    if ( FloatExtensions.TryParsePrecise( maximums[1], out values[1] ) )
                                        attributeData_Vector2.y.Max = values[1];
                                }
                            }
                            result = attributeData_Vector2;
                            break;
                        #endregion
                        #region Vector3
                        case "vector3-textbox":
                            AttributeData_Vector3 attributeData_Vector3 = new AttributeData_Vector3();
                            SpiffyBaseAttributesReader( attributeData_Vector3, attributes, doc );
                            {
                                if ( attributes.TryGetValue( "default", out XmlAttributeToRead? attribute ) )
                                {
                                    string[] defaults = attribute.Value.Split( ',' );
                                    float[] values = new float[3];
                                    if ( FloatExtensions.TryParsePrecise( defaults[0], out values[0] ) )
                                        attributeData_Vector3.x.Default = values[0];
                                    if ( FloatExtensions.TryParsePrecise( defaults[1], out values[1] ) )
                                        attributeData_Vector3.y.Default = values[1];
                                    if ( FloatExtensions.TryParsePrecise( defaults[2], out values[2] ) )
                                        attributeData_Vector3.z.Default = values[2];
                                }
                            }
                            {
                                if ( attributes.TryGetValue( "min", out XmlAttributeToRead? attribute ) )
                                {
                                    string[] minimums = attribute.Value.Split( ',' );
                                    float[] values = new float[3];
                                    if ( FloatExtensions.TryParsePrecise( minimums[0], out values[0] ) )
                                        attributeData_Vector3.x.Min = values[0];
                                    if ( FloatExtensions.TryParsePrecise( minimums[1], out values[1] ) )
                                        attributeData_Vector3.y.Min = values[1];
                                    if ( FloatExtensions.TryParsePrecise( minimums[2], out values[2] ) )
                                        attributeData_Vector3.z.Min = values[2];
                                }
                            }

                            {
                                if ( attributes.TryGetValue( "max", out XmlAttributeToRead? attribute ) )
                                {
                                    string[] maximums = attribute.Value.Split( ',' );
                                    float[] values = new float[3];
                                    if ( FloatExtensions.TryParsePrecise( maximums[0], out values[0] ) )
                                        attributeData_Vector3.x.Max = values[0];
                                    if ( FloatExtensions.TryParsePrecise( maximums[1], out values[1] ) )
                                        attributeData_Vector3.y.Max = values[1];
                                    if ( FloatExtensions.TryParsePrecise( maximums[2], out values[2] ) )
                                        attributeData_Vector3.z.Max = values[2];
                                }
                            }
                            result = attributeData_Vector3;
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
                    ArcenDebugging.LogSingleLine( "No Main Attribute Type found at all. File name:" + doc.Name + "\nElement: " + element.OuterXml, Verbosity.DoNotShow );
                }
                List<string> missing = new List<string>();
                foreach ( XmlAttributeToRead attribute in attributes.Values )
                    if ( !attribute.HasReadValue )
                        missing.Add( attribute.Name );
                if ( missing.Count > 0 )
                {
                    string missingToDisplay = string.Empty;
                    foreach ( string attributeName in missing )
                        missingToDisplay += "    " + attributeName + "\n";
                    ArcenDebugging.LogSingleLine( "Attributes:\n" + missingToDisplay + "haven't been read! File name:" + doc.Name + "\nElement: " + element.OuterXml, Verbosity.DoNotShow );
                }
            }
            else
            {
                result = null;
                ArcenDebugging.LogSingleLine( "No attributes found at all. File name:" + doc.Name + "\nElement: " + element.OuterXml, Verbosity.DoNotShow );
            }
        }

        private static void SpiffyBaseAttributesReader( AttributeData_Base attributeData, Dictionary<string, XmlAttributeToRead> attributes, MetadataDocument doc )
        {
            {
                if ( attributes.TryGetValue( "key", out XmlAttributeToRead? attribute ) )
                    attributeData.Key = attribute.Value;
                else
                    ArcenDebugging.LogSingleLine( "WARNING: Required attribute \"key\" in file " + doc.Name + " is missing.", Verbosity.DoNotShow );
            }
            {
                if ( attributes.TryGetValue( "is_required", out XmlAttributeToRead? attribute ) )
                    attributeData.IsRequired = bool.Parse( attribute.Value );
                else { }
            }
            {
                if ( attributes.TryGetValue( "is_central_identifier", out XmlAttributeToRead? attribute ) )
                {
                    if ( attribute.Value.ToLowerInvariant() == "true" )
                    {
                        if ( !doc.IsDataCopyIdentifierAlreadyRead )
                        {
                            attributeData.IsCentralIdentifier = bool.Parse( attribute.Value );
                            doc.IsDataCopyIdentifierAlreadyRead = true;
                        }
                        else
                            ArcenDebugging.LogSingleLine( "There is more than 1 IsCentralIdentifier inside of metadata " + doc.Name, Verbosity.DoNotShow );
                    }
                }
                else { } //do nothing, because this field is optional (there has to be 1 per file)
            }
            {
                if ( attributes.TryGetValue( "is_partial_identifier", out XmlAttributeToRead? attribute ) )
                    attributeData.IsPartialIdentifier = bool.Parse( attribute.Value );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "is_data_copy_identifier", out XmlAttributeToRead? attribute ) )
                    attributeData.IsDataCopyIdentifier = bool.Parse( attribute.Value );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "causes_all_fields_to_be_optional_except_central_identifier", out XmlAttributeToRead? attribute ) )
                    attributeData.CausesAllFieldsToBeOptionalExceptCentralIdentifier = bool.Parse( attribute.Value );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "is_description", out XmlAttributeToRead? attribute ) )
                    attributeData.IsDescription = bool.Parse( attribute.Value );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "is_localized", out XmlAttributeToRead? attribute ) )
                    attributeData.IsLocalized = bool.Parse( attribute.Value );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "content_width_px", out XmlAttributeToRead? attribute ) )
                    attributeData.ContentWidthPx = int.Parse( attribute.Value );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "linebreak_before", out XmlAttributeToRead? attribute ) )
                    attributeData.LinebreakBefore = (LineBreakType)Enum.Parse( typeof( LineBreakType ), attribute.Value, true );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "linebreak_after", out XmlAttributeToRead? attribute ) )
                    attributeData.LinebreakAfter = (LineBreakType)Enum.Parse( typeof( LineBreakType ), attribute.Value, true );
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "only_exists_if_conditional_passes", out XmlAttributeToRead? attribute ) )
                    attributeData.OnlyExistsIfConditionalPasses = attribute.Value;
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "tooltip", out XmlAttributeToRead? attribute ) )
                    attributeData.Tooltip = attribute.Value;
                else { } //do nothing, because this field is optional
            }
            {
                if ( attributes.TryGetValue( "is_user_facing_name", out XmlAttributeToRead? attribute ) )
                    attributeData.Tooltip = attribute.Value;
                else { } //do nothing, because this field is optional
            }
        }

        private class XmlAttributeToRead
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
