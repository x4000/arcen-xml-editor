using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcenXE.Utilities
{
    #region EditedXmlNode
    public class EditedXmlNode : IEditedXmlNodeOrComment, IEditedXmlElement
    {
        public EditedXmlAttribute? NodeCentralID = null; // if != null, then it's a top node
        public Dictionary<string, EditedXmlAttribute> Attributes = new Dictionary<string, EditedXmlAttribute>();
        public List<IEditedXmlNodeOrComment> ChildNodes = new List<IEditedXmlNodeOrComment>();
        public bool IsRootOnly = false;
        public bool IsComment => false;

        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Label? CurrentViewControl;
    }
    #endregion

    #region EditedXmlComment
    public class EditedXmlComment : IEditedXmlNodeOrComment, IEditedXmlElement
    {
        public string Data = string.Empty;

        public bool IsComment => true;

        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Control? CurrentViewControl;
    }
    #endregion

    #region EditedXmlAttribute
    public class EditedXmlAttribute : IEditedXmlElement
    {
        public string Name = string.Empty;
        public AttributeType Type = AttributeType.Unknown; //to be filled by metadata
        public string? ValueOnDisk = null;
        public string? TempValue = null;

        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Label? CurrentViewControl_Name;
        /// <summary>
        /// For use in XmlVisualizer only
        /// </summary>
        public Control? CurrentViewControl_Value;

        public bool GetHasChanges()
        {
            if ( this.TempValue == null )
                return false;
            return this.ValueOnDisk != this.TempValue;
        }

        public string? GetEffectiveValue()
        {
            if ( this.TempValue == null )
                return this.ValueOnDisk;
            else
                return this.TempValue;
        }
    }
    #endregion

    #region IEditedXml
    public interface IEditedXmlNodeOrComment
    {
        public bool IsComment { get; }
    }

    public interface IEditedXmlElement
    {

    }
    #endregion
}
