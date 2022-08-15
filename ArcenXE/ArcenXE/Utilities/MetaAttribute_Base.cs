using ArcenXE.Universal;
using ArcenXE.Utilities.MetadataProcessing;
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
        public int ContentWidthPx { get; set; } = 20;
        public virtual LineBreakType LinebreakBefore { get; set; } = LineBreakType.PreferNot;
        public virtual LineBreakType LinebreakAfter { get; set; } = LineBreakType.PreferNot;
        public string OnlyExistsIfConditionalPasses { get; set; } = string.Empty;
        public bool IsUserFacingName { get; set; } = false;
        public string Tooltip { get; set; } = string.Empty;

        public abstract string DoValidate( EditedXmlAttribute att );

    }

    public class MetaAttribute_Bool : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Bool;
        public bool Default { get; set; } = false;

        public override string DoValidate( EditedXmlAttribute att )
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

        public override string DoValidate( EditedXmlAttribute att )
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

        public override string DoValidate( EditedXmlAttribute att )
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

        public override string DoValidate( EditedXmlAttribute att )
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

        public override string DoValidate( EditedXmlAttribute att )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null && val.Length > 0 && !this.Options.Contains( val ) )
                errorList += $"Value {val} is not in the list of option.\n";
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

        public override string DoValidate( EditedXmlAttribute att )
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

        public override string DoValidate( EditedXmlAttribute att )
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

        public override string DoValidate( EditedXmlAttribute att )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            bool found = false;
            if ( val != null && val.Length > 0 )
            {
                MetadataDocument? metaDoc = MetadataStorage.GetMetadataDocumentByName( NodeSource );
                if ( metaDoc == null )
                    return string.Empty;

                List<TopNodesCaching.TopNode>? topNodes = TopNodesCaching.GetAllNodesForDataTable( metaDoc );
                if ( topNodes != null )
                    foreach ( TopNodesCaching.TopNode node in topNodes )
                        if ( node.UserFacingName == val )
                            found = true;
            }
            if ( !found )
                errorList += $"Value {val} is not in a valid node.\n";
            return errorList;
        }
    }

    public class MetaAttribute_NodeList : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.NodeList;
        public List<string> Defaults { get; set; } = new List<string>();
        public string NodeSource { get; set; } = string.Empty;

        public override string DoValidate( EditedXmlAttribute att )
        {
            string[]? listedNodes = att.GetEffectiveValue()?.Split( ',' );
            string errorList = string.Empty;
            bool found;
            if ( listedNodes != null && listedNodes.Length > 0 )
            {
                MetadataDocument? metaDoc = MetadataStorage.GetMetadataDocumentByName( NodeSource );
                if ( metaDoc == null )
                    return string.Empty;

                List<TopNodesCaching.TopNode>? topNodes = TopNodesCaching.GetAllNodesForDataTable( metaDoc );
                if ( topNodes == null )
                    return string.Empty;

                foreach ( string listedNode in listedNodes )
                {
                    found = false;
                    foreach ( TopNodesCaching.TopNode topNode in topNodes )
                    {
                        if ( topNode.UserFacingName == listedNode )
                            found = true;
                    }
                    if ( !found )
                        errorList += $"Value {listedNode} is not in a valid node.\n";
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

        public override string DoValidate( EditedXmlAttribute att )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            bool found = false;
            if ( val != null && val.Length > 0 )
                foreach ( string folderPath in this.FolderPaths )
                    if ( folderPath.Contains( val ) )
                        found = true;
            if ( !found )
                errorList += $"Value {val} is not in a valid folder.\n";
            return errorList;
        }
    }

    public class MetaAttribute_Point : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Point;
        public MetaAttribute_Int x = new MetaAttribute_Int();
        public MetaAttribute_Int y = new MetaAttribute_Int();

        public override string DoValidate( EditedXmlAttribute att )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null )
            {
                string[] values = val.Split( ',' );
                int[] ints = new int[values.Length];
                foreach ( string value in values )
                {
                    if ( int.TryParse( values[0], out ints[0] ) )
                    {
                        if ( ints[0] < x.Min )
                            errorList += $"Int is {ints[0]}. It needs to be at least {this.x.Min}.\n";
                        if ( ints[0] > x.Max )
                            errorList += $"Int is {ints[0]}. It needs to be at maximum {this.x.Max}.\n";
                        if ( MinorUtilities.CalculateNumberOfDigits( ints[0] ) < x.MinimumDigits )
                            errorList += $"Int is {ints[0]}. It needs to have at least {this.x.MinimumDigits} digits.\n";
                    }
                    else
                        errorList += "Invalid Int x";
                    if ( int.TryParse( values[1], out ints[1] ) )
                    {
                        if ( ints[1] < y.Min )
                            errorList += $"Int is {ints[1]}. It needs to be at least {this.y.Min}.\n";
                        if ( ints[1] > y.Max )
                            errorList += $"Int is {ints[1]}. It needs to be at maximum {this.y.Max}.\n";
                        if ( MinorUtilities.CalculateNumberOfDigits( ints[1] ) < y.MinimumDigits )
                            errorList += $"Int is {ints[1]}. It needs to have at least {this.y.MinimumDigits} digits.\n";
                    }
                    else
                        errorList += "Invalid Int y";
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

        public override string DoValidate( EditedXmlAttribute att )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null )
            {
                string[] values = val.Split( ',' );
                float[] floats = new float[values.Length];
                foreach ( string value in values )
                {
                    if ( float.TryParse( values[0], out floats[0] ) )
                    {
                        if ( floats[0] < x.Min )
                            errorList += $"Float is {floats[0]}. It needs to be at least {this.x.Min}.\n";
                        if ( floats[0] > x.Max )
                            errorList += $"Float is {floats[0]}. It needs to be at maximum {this.x.Max}.\n";
                        if ( MinorUtilities.CalculateNumberOfDigits( (int)floats[0] ) < x.MinimumDigits )
                            errorList += $"Float's whole number is {floats[0]}. It needs to have at least {this.x.MinimumDigits} digits.\n";
                    }
                    else
                        errorList += "Invalid Float x";
                    if ( float.TryParse( values[1], out floats[1] ) )
                    {
                        if ( floats[1] < y.Min )
                            errorList += $"Float is {floats[1]}. It needs to be at least {this.y.Min}.\n";
                        if ( floats[1] > y.Max )
                            errorList += $"Float is {floats[1]}. It needs to be at maximum {this.y.Max}.\n";
                        if ( MinorUtilities.CalculateNumberOfDigits( (int)floats[1] ) < y.MinimumDigits )
                            errorList += $"Float's whole number is {floats[1]}. It needs to have at least {this.y.MinimumDigits} digits.\n";
                    }
                    else
                        errorList += "Invalid Float y";
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

        public override string DoValidate( EditedXmlAttribute att )
        {
            string? val = att.GetEffectiveValue();
            string errorList = string.Empty;
            if ( val != null )
            {
                string[] values = val.Split( ',' );
                float[] floats = new float[values.Length];
                foreach ( string value in values )
                {
                    if ( float.TryParse( values[0], out floats[0] ) )
                    {
                        if ( floats[0] < x.Min )
                            errorList += $"Float is {floats[0]}. It needs to be at least {this.x.Min}.\n";
                        if ( floats[0] > x.Max )
                            errorList += $"Float is {floats[0]}. It needs to be at maximum {this.x.Max}.\n";
                        if ( MinorUtilities.CalculateNumberOfDigits( (int)floats[0] ) < x.MinimumDigits )
                            errorList += $"Float's whole number is {floats[0]}. It needs to have at least {this.x.MinimumDigits} digits.\n";
                    }
                    else
                        errorList += "Invalid Float x";
                    if ( float.TryParse( values[1], out floats[1] ) )
                    {
                        if ( floats[1] < y.Min )
                            errorList += $"Float is {floats[1]}. It needs to be at least {this.y.Min}.\n";
                        if ( floats[1] > y.Max )
                            errorList += $"Float is {floats[1]}. It needs to be at maximum {this.y.Max}.\n";
                        if ( MinorUtilities.CalculateNumberOfDigits( (int)floats[1] ) < y.MinimumDigits )
                            errorList += $"Float's whole number is {floats[1]}. It needs to have at least {this.y.MinimumDigits} digits.\n";
                    }
                    else
                        errorList += "Invalid Float y";
                    if ( float.TryParse( values[2], out floats[2] ) )
                    {
                        if ( floats[2] < z.Min )
                            errorList += $"Float is {floats[2]}. It needs to be at least {this.z.Min}.\n";
                        if ( floats[2] > z.Max )
                            errorList += $"Float is {floats[2]}. It needs to be at maximum {this.z.Max}.\n";
                        if ( MinorUtilities.CalculateNumberOfDigits( (int)floats[2] ) < z.MinimumDigits )
                            errorList += $"Float's whole number is {floats[2]}. It needs to have at least {this.z.MinimumDigits} digits.\n";
                    }
                    else
                        errorList += "Invalid Float z";
                }
            }
            else
                errorList += "Invalid Vector2";
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
