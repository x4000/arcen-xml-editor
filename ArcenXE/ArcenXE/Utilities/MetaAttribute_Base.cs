namespace ArcenXE.Utilities
{
    public abstract class MetaAttribute_Base
    { // make those all fields and not properties if no extra valdiation is added here
        public string Key { get; set; } = string.Empty;
        public virtual AttributeType Type { get; private init; } = AttributeType.Unknown;
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

    }

    public class MetaAttribute_Bool : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Bool;
        public bool Default { get; set; } = false;
    }

    public class MetaAttribute_BoolInt : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.BoolInt;
        private int def = 0;
        public int Default
        {
            get => def;
            set
            {
                if ( value == 0 || value == 1 )
                    def = value;
                else
                    ArcenDebugging.LogSingleLine( "ERROR: Default Value in MetaAttribute_BoolInt - Value must be 0 or 1", Verbosity.DoNotShow );
            }
        }
    }

    public class MetaAttribute_String : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.String;
        public string Default { get; set; } = string.Empty;
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 1000;

    }

    public class MetaAttribute_StringMultiline : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.StringMultiLine;
        public string Default { get; set; } = string.Empty;
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 10000;
        public int ShowLines { get; set; } = 3;
    }

    public class MetaAttribute_ArbitraryString : MetaAttribute_Base // aka string-dropdown
    {
        public override AttributeType Type => AttributeType.ArbitraryString;
        public string Default { get; set; } = string.Empty;
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 1000;
        public List<string> Options { get; set; } = new List<string>();
    }

    public class MetaAttribute_Int : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Int;
        //convert to int form string?
        public int Default { get; set; } = 0;
        public int Min { get; set; } = int.MinValue;
        public int Max { get; set; } = int.MaxValue;
        public int MinimumDigits { get; set; } = 1;
    }

    public class MetaAttribute_Float : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Float;
        //convert to float form string?
        public float Default { get; set; } = 0f;
        public float Min { get; set; } = float.MinValue;
        public float Max { get; set; } = float.MaxValue;
        public int Precision { get; set; } = 3;
        public int MinimumDigits { get; set; } = 1;
    }

    public class MetaAttribute_ArbitraryNode : MetaAttribute_Base // aka node-dropdown
    {
        public override AttributeType Type => AttributeType.ArbitraryNode;
        public ReferenceXmlNode Default { get; set; } = new ReferenceXmlNode();
        public string NodeSource { get; set; } = string.Empty;
        public List<ReferenceXmlNode> Nodes { get; set; } = new List<ReferenceXmlNode>(); //todo
    }

    public class ReferenceXmlNode
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class MetaAttribute_NodeList : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.NodeList;
        public List<ReferenceXmlNode> Defaults { get; set; } = new List<ReferenceXmlNode>();
        public string NodeSource { get; set; } = string.Empty;
        public List<ReferenceXmlNode> Nodes { get; set; } = new List<ReferenceXmlNode>(); // todo: fill using NodeSource
    }

    public class MetaAttribute_FolderList : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.FolderList;
        public List<string> Defaults { get; set; } = new List<string>();
        public string FolderSource { get; set; } = string.Empty;
        public List<string> FoldersPath { get; set; } = new List<string>(); //todo
    }

    public class MetaAttribute_Point : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Point;
        public MetaAttribute_Int x = new MetaAttribute_Int();
        public MetaAttribute_Int y = new MetaAttribute_Int();
    }

    public class MetaAttribute_Vector2 : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Vector2;
        public MetaAttribute_Float x = new MetaAttribute_Float();
        public MetaAttribute_Float y = new MetaAttribute_Float();
    }

    public class MetaAttribute_Vector3 : MetaAttribute_Base
    {
        public override AttributeType Type => AttributeType.Vector3;
        public MetaAttribute_Float x = new MetaAttribute_Float();
        public MetaAttribute_Float y = new MetaAttribute_Float();
        public MetaAttribute_Float z = new MetaAttribute_Float();
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
