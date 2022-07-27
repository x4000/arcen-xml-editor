namespace ArcenXE.Utilities
{
    public abstract class AttributeData_Base
    {
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
        public string Tooltip { get; set; } = string.Empty;
        public string OnlyExistsIfConditionalPasses { get; set; } = string.Empty;
        public bool IsUserFacingName { get; set; } = false;
        
    }

    public class AttributeData_Bool : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.Bool;
        public bool Default { get; set; } = false;
    }

    public class AttributeData_BoolInt : AttributeData_Base
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
                    ArcenDebugging.LogSingleLine( "Default Value in AttributeData_BoolInt - Value must be 0 or 1", Verbosity.DoNotShow );
            }
        }
    }

    public class AttributeData_String : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.String;
        public string Default { get; set; } = string.Empty;
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 1000;

    }

    public class AttributeData_StringMultiline : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.StringMultiLine;
        public string Default { get; set; } = string.Empty;
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 10000;
        public int ShowLines { get; set; } = 3;
    }

    public class AttributeData_ArbitraryString : AttributeData_Base // aka string-dropdown
    {
        public override AttributeType Type => AttributeType.ArbitraryString;
        public string Default { get; set; } = string.Empty;
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 1000;
        public List<string> Strings { get; set; } = new List<string>();
    }

    public class AttributeData_Int : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.Int;
        //convert to int form string?
        public int Default { get; set; } = 0;
        public int Min { get; set; } = int.MinValue;
        public int Max { get; set; } = int.MaxValue;
        public int MinimumDigits { get; set; } = 1;
    }

    public class AttributeData_Float : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.Float;
        //convert to float form string?
        public float Default { get; set; } = 0f;
        public float Min { get; set; } = float.MinValue;
        public float Max { get; set; } = float.MaxValue;
        public int Precision { get; set; } = 3;
        public int MinimumDigits { get; set; } = 1;
    }

    public class AttributeData_ArbitraryNode : AttributeData_Base // aka node-dropdown
    {
        public override AttributeType Type => AttributeType.ArbitraryNode;
        public ReferenceXmlNode Default { get; set; } = new ReferenceXmlNode();
        public string NodeSource { get; set; } = string.Empty;
        public List<ReferenceXmlNode> Nodes { get; set; } = new List<ReferenceXmlNode>();
    }

    public class ReferenceXmlNode
    {
        public string Name { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Descriptions { get; set; } = string.Empty;
    }

    public class AttributeData_NodeList : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.NodeList;
        public ReferenceXmlNode Default { get; set; } = new ReferenceXmlNode();
        public string NodeSource { get; set; } = string.Empty;
        public List<ReferenceXmlNode> Nodes { get; set; } = new List<ReferenceXmlNode>();
    }

    public class AttributeData_FolderList : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.FolderList;
        public string FoldeSource { get; set; } = string.Empty;
        //public string PathToMetadata { get; set; } = Path.GetFullPath( Application.ExecutablePath ); //temporary
    }

    public class AttributeData_Point : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.Point;
        public AttributeData_Int x = new AttributeData_Int();
        public AttributeData_Int y = new AttributeData_Int();
    }

    public class AttributeData_Vector2 : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.Vector2;
        public AttributeData_Float x = new AttributeData_Float();
        public AttributeData_Float y = new AttributeData_Float();
    }

    public class AttributeData_Vector3 : AttributeData_Base
    {
        public override AttributeType Type => AttributeType.Vector3;
        public AttributeData_Float x = new AttributeData_Float();
        public AttributeData_Float y = new AttributeData_Float();
        public AttributeData_Float z = new AttributeData_Float();
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
