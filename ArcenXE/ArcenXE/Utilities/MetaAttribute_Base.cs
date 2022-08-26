using ArcenXE.Universal;
using ArcenXE.Utilities.MetadataProcessing;
using ArcenXE.Visualization.Utilities;
using System;

namespace ArcenXE.Utilities
{
    public abstract class MetaAttribute_Base
    { // make those all fields and not properties if no extra valdiation is added here
        public string Key { get; set; } = string.Empty;
        public abstract AttributeType Type { get; }
        public bool IsRequired { get; set; } = false;
        public bool IsCentralIdentifier { get; set; } = false;
        public bool IsPartialIdentifier { get; set; } = false;
        public bool IsDataCopyIdentifier { get; set; } = false;
        public bool CausesAllFieldsToBeOptionalExceptCentralIdentifier { get; set; } = false;
        public bool IsDescription { get; set; } = false;
        public bool IsLocalized { get; set; } = false;
        public int ContentWidthPx { get; set; } = 70;
        public virtual LineBreakType LinebreakBefore { get; set; } = LineBreakType.PreferNot;
        public virtual LineBreakType LinebreakAfter { get; set; } = LineBreakType.PreferNot;
        public string OnlyExistsIfConditionalPasses { get; set; } = string.Empty;
        public bool IsUserFacingName { get; set; } = false;
        public string Tooltip { get; set; } = string.Empty;
        public UnionAttribute? RelatedUnionAttribute { get; set; } = null;

        public abstract string DoValidate( EditedXmlAttribute att, Coordinate coordinate );

    }

    public class MetaAttribute_Bool : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Bool;
        public bool Default { get; set; } = false;

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            if ( val == null )
                return "Invalid Bool";
            return bool.TryParse( val, out _ ) ? string.Empty : "Invalid Bool";
        }
    }

    public class MetaAttribute_BoolInt : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.BoolInt;
        public int Default { get; set; } = 0;

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            if ( val == null )
                return "Invalid BoolInt";
            return int.TryParse( val, out _ ) ? string.Empty : "Invalid BoolInt";
        }
    }

    public class MetaAttribute_String : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.String;
        public string Default { get; set; } = string.Empty;
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 1000;

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null )
            {
                if ( val.Length < this.MinLength )
                    errorList += $"String has {val.Length} chars. It needs at least {this.MinLength}.\n";
                if ( val.Length > this.MaxLength )
                    errorList += $"String has {val.Length} chars. It needs at maximum {this.MaxLength}.\n";
            }
            return errorList;
        }
    }

    public class MetaAttribute_StringMultiline : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.StringMultiLine;
        public string Default { get; set; } = string.Empty;
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 10000;
        public int ShowLines { get; set; } = 3;

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null )
            {
                if ( val.Length < this.MinLength )
                    errorList += $"String has {val.Length} chars. It needs at least {this.MinLength}.\n";
                if ( val.Length > this.MaxLength )
                    errorList += $"String has {val.Length} chars. It needs at maximum {this.MaxLength}.\n";
            }
            return errorList;
        }
    }

    public class MetaAttribute_ArbitraryString : MetaAttribute_Base // aka string-dropdown
    {
        public override AttributeType Type => AttributeType.ArbitraryString;
        public string Default { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new List<string>();

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null && val.Length > 0 && !this.Options.Contains( val ) )
                errorList += $"String '{val}' is not in the list of option.\n";
            return errorList;
        }
    }

    public class MetaAttribute_Int : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Int;
        public int Default { get; set; } = 0;
        public int Min { get; set; } = int.MinValue;
        public int Max { get; set; } = int.MaxValue;
        public int MinimumDigits { get; set; } = 1;

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null && int.TryParse( val, out int result ) )
            {
                if ( result < this.Min )
                    errorList += $"Int is {result}. It needs to be at least {this.Min}.\n";
                if ( result > this.Max )
                    errorList += $"Int is {result}. It needs to be at maximum {this.Max}.\n";
                if ( MinorUtilities.CalculateNumberOfDigits( result ) < MinimumDigits )
                    errorList += $"Int is {result}. It needs to have at least {this.MinimumDigits} digits.\n";
            }
            else
                errorList += "Invalid Int";
            return errorList;
        }
    }

    public class MetaAttribute_Float : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Float;
        public float Default { get; set; } = 0f;
        public float Min { get; set; } = float.MinValue;
        public float Max { get; set; } = float.MaxValue;
        public int Precision { get; set; } = 3;
        public int MinimumDigits { get; set; } = 1;

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null && float.TryParse( val, out float result ) )
            {
                if ( result < this.Min )
                    errorList += $"Float is {result}. It needs to be at least {this.Min}.\n";
                if ( result > this.Max )
                    errorList += $"Float is {result}. It needs to be at maximum {this.Max}.\n";
                if ( MinorUtilities.CalculateNumberOfDigits( (int)result ) < MinimumDigits )
                    errorList += $"Float's whole number is {result}. It needs to have at least {this.MinimumDigits} digits.\n";
            }
            else
                errorList += "Invalid Float";
            return errorList;
        }
    }

    public class MetaAttribute_ArbitraryNode : MetaAttribute_Base // aka node-dropdown
    {
        public override AttributeType Type => AttributeType.ArbitraryNode;
        public string Default { get; set; } = string.Empty;
        public string NodeSource { get; set; } = string.Empty;

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            bool found = false;
            if ( val != null && val.Length > 0 )
            {
                MetadataDocument? metaDoc = MetadataStorage.GetMetadataDocumentByName( NodeSource );
                if ( metaDoc == null )
                    return string.Empty;// complain?

                List<TopNodesCaching.TopNode>? topNodes = TopNodesCaching.GetAllNodesForDataTable( metaDoc );
                if ( topNodes != null )// complain on else?
                {
                    foreach ( TopNodesCaching.TopNode node in topNodes )
                        if ( node.CentralID == val )
                            found = true;
                }
                else
                    ArcenDebugging.LogSingleLine( "topnodes null in DoVal!", Verbosity.DoNotShow );
            }
            if ( !found )
                errorList += $"Value '{val}' is not in valid node.\n";
            return errorList;
        }
    }

    public class MetaAttribute_NodeList : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.NodeList;
        public List<string> Defaults { get; set; } = new List<string>();
        public string NodeSource { get; set; } = string.Empty;

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string[]? listedNodes = att.GetEffectiveValue()?.Split( ',' );
            string errorList = string.Empty;
            bool found;
            if ( listedNodes != null && listedNodes.Length > 0 )
            {
                MetadataDocument? metaDoc = MetadataStorage.GetMetadataDocumentByName( NodeSource );
                if ( metaDoc == null )
                    return string.Empty; // complain?

                List<TopNodesCaching.TopNode>? topNodes = TopNodesCaching.GetAllNodesForDataTable( metaDoc );
                if ( topNodes == null )
                    return string.Empty; // complain?

                foreach ( string listedNode in listedNodes )
                {
                    found = false;
                    foreach ( TopNodesCaching.TopNode topNode in topNodes )
                    {
                        if ( topNode.CentralID == listedNode )
                            found = true;
                    }
                    if ( !found )
                        errorList += $"Value '{listedNode}' is not a valid node.\n";
                }
            }
            return errorList;
        }
    }

    public class MetaAttribute_FolderList : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.FolderList;
        public List<string> Defaults { get; set; } = new List<string>();
        public string FolderSource { get; set; } = string.Empty;
        private List<string> folderPaths = new List<string>();
        public List<string> FolderPaths
        {
            get => folderPaths;
            set
            {
                if ( this.folderPaths.Count > 0 )
                    folderPaths.Clear();
                folderPaths = value;
            }
        }

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            bool found = false;
            if ( val != null && val.Length > 0 )
                foreach ( string folderPath in this.FolderPaths )
                    if ( folderPath.Contains( val ) )
                        found = true;
            if ( !found )
                errorList += $"Value {val} is not a valid folder.\n";
            return errorList;
        }
    }

    public class MetaAttribute_Point : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Point;
        public MetaAttribute_Int x = new MetaAttribute_Int();
        public MetaAttribute_Int y = new MetaAttribute_Int();

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null )
            {
                int intVal;
                switch ( coordinate )
                {
                    case Coordinate.x:                       
                    if ( int.TryParse( val, out intVal ) )
                    {
                        if ( intVal < x.Min )
                            errorList += $"Int is {intVal}. It needs to be at least {this.x.Min}.\n";
                        if ( intVal > x.Max )
                            errorList += $"Int is {intVal}. It needs to be at maximum {this.x.Max}.\n";
                        if ( MinorUtilities.CalculateNumberOfDigits( intVal ) < x.MinimumDigits )
                            errorList += $"Int is {intVal}. It needs to have at least {this.x.MinimumDigits} digits.\n";
                    }
                    else
                        errorList += "Invalid Int x";
                        break;
                    case Coordinate.y:
                        if ( int.TryParse( val, out intVal ) )
                    {
                        if ( intVal < y.Min )
                            errorList += $"Int is {intVal}. It needs to be at least {this.y.Min}.\n";
                        if ( intVal > y.Max )
                            errorList += $"Int is {intVal}. It needs to be at maximum {this.y.Max}.\n";
                        if ( MinorUtilities.CalculateNumberOfDigits( intVal ) < y.MinimumDigits )
                            errorList += $"Int is {intVal}. It needs to have at least {this.y.MinimumDigits} digits.\n";
                    }
                    else
                        errorList += "Invalid Int y";
                        break;
                }
            }
            else
                errorList += "Invalid Point";
            return errorList;
        }
    }

    public class MetaAttribute_Vector2 : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Vector2;
        public MetaAttribute_Float x = new MetaAttribute_Float();
        public MetaAttribute_Float y = new MetaAttribute_Float();

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null )
            {
                float floatVal;
                switch ( coordinate )
                {
                    case Coordinate.x:
                        if ( float.TryParse( val, out floatVal ) )
                        {
                            if ( floatVal < x.Min )
                                errorList += $"Float is {floatVal}. It needs to be at least {this.x.Min}.\n";
                            if ( floatVal > x.Max )
                                errorList += $"Float is {floatVal}. It needs to be at maximum {this.x.Max}.\n";
                            if ( MinorUtilities.CalculateNumberOfDigits( (int)floatVal ) < x.MinimumDigits )
                                errorList += $"Float's whole number is {floatVal}. It needs to have at least {this.x.MinimumDigits} digits.\n";
                        }
                        else
                            errorList += "Invalid Float x";
                        break;
                    case Coordinate.y:
                        if ( float.TryParse( val, out floatVal ) )
                        {
                            if ( floatVal < y.Min )
                                errorList += $"Float is {floatVal}. It needs to be at least {this.y.Min}.\n";
                            if ( floatVal > y.Max )
                                errorList += $"Float is {floatVal}. It needs to be at maximum {this.y.Max}.\n";
                            if ( MinorUtilities.CalculateNumberOfDigits( (int)floatVal ) < y.MinimumDigits )
                                errorList += $"Float's whole number is {floatVal}. It needs to have at least {this.y.MinimumDigits} digits.\n";
                        }
                        else
                            errorList += "Invalid Float y";
                        break;
                }
            }
            else
                errorList += "Invalid Vector2";
            return errorList;
        }
    }

    public class MetaAttribute_Vector3 : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Vector3;
        public MetaAttribute_Float x = new MetaAttribute_Float();
        public MetaAttribute_Float y = new MetaAttribute_Float();
        public MetaAttribute_Float z = new MetaAttribute_Float();

        public override string DoValidate( EditedXmlAttribute att, Coordinate coordinate )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null )
            {
                float floatVal;
                switch ( coordinate )
                {
                    case Coordinate.x:
                        if ( float.TryParse( val, out floatVal ) )
                        {
                            if ( floatVal < x.Min )
                                errorList += $"Float is {floatVal}. It needs to be at least {this.x.Min}.\n";
                            if ( floatVal > x.Max )
                                errorList += $"Float is {floatVal}. It needs to be at maximum {this.x.Max}.\n";
                            if ( MinorUtilities.CalculateNumberOfDigits( (int)floatVal ) < x.MinimumDigits )
                                errorList += $"Float's whole number is {floatVal}. It needs to have at least {this.x.MinimumDigits} digits.\n";
                        }
                        else
                            errorList += "Invalid Float x";
                        break;
                    case Coordinate.y:
                        if ( float.TryParse( val, out floatVal ) )
                        {
                            if ( floatVal < y.Min )
                                errorList += $"Float is {floatVal}. It needs to be at least {this.y.Min}.\n";
                            if ( floatVal > y.Max )
                                errorList += $"Float is {floatVal}. It needs to be at maximum {this.y.Max}.\n";
                            if ( MinorUtilities.CalculateNumberOfDigits( (int)floatVal ) < y.MinimumDigits )
                                errorList += $"Float's whole number is {floatVal}. It needs to have at least {this.y.MinimumDigits} digits.\n";
                        }
                        else
                            errorList += "Invalid Float y";
                        break;
                    case Coordinate.z:
                        if ( float.TryParse( val, out floatVal ) )
                        {
                            if ( floatVal < z.Min )
                                errorList += $"Float is {floatVal}. It needs to be at least {this.z.Min}.\n";
                            if ( floatVal > z.Max )
                                errorList += $"Float is {floatVal}. It needs to be at maximum {this.z.Max}.\n";
                            if ( MinorUtilities.CalculateNumberOfDigits( (int)floatVal ) < z.MinimumDigits )
                                errorList += $"Float's whole number is {floatVal}. It needs to have at least {this.z.MinimumDigits} digits.\n";
                        }
                        else
                            errorList += "Invalid Float z";
                        break;
                    case Coordinate.None:
                        ArcenDebugging.LogSingleLine( "The coordinate in Vector3 shouldn't be set to None!", Verbosity.DoNotShow );
                        break;
                }
            }
            else
                errorList += "Invalid Vector3";
            return errorList;
        }
    }

    public enum AttributeType
    {
        Unknown = 0,
        Bool,
        BoolInt,
        String,
        StringMultiLine,
        ArbitraryString,
        Int,
        Float,
        ArbitraryNode, // choose 1
        NodeList, // choose multiple
        FolderList,
        Point,
        Vector2,
        Vector3,
        Length
    }

    public enum LineBreakType
    {
        Unknown = 0,
        Always,
        PreferNot,
        Length
    }
}
