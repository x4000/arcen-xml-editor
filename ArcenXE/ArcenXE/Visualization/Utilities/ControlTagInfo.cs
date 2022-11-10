using ArcenXE.Universal;
using ArcenXE.Utilities;
using ArcenXE.Visualization;

namespace ArcenXE.Visualization.Utilities
{
    public class ControlTagInfo : IControlTagInfo
    {
        public IUnionElement? RelatedUnionElement { get; set; } = null;
        public Control RelatedControl { get; protected init; }
        public ErrorProvider RelatedErrorProvider { get; private init; } = new ErrorProvider();

        public ControlTagInfo( Control relatedControl )
        {
            this.RelatedControl = relatedControl;
        }

        public void ClearErrorProvider( Control control )
        {
            this.RelatedErrorProvider.SetError( control, string.Empty );
            this.RelatedErrorProvider.Clear();
        }
    }

    public class PooledControlTagInfo : ControlTagInfo, IControlTagInfo
    {
        /// <summary>
        /// Only used by numerical updown controls
        /// </summary>
        public Coordinate ControlsCoordinate = Coordinate.None;
        /// <summary>
        /// Only used by the buttons that show the attribute list of a node, when clicked
        /// </summary>
        public bool IsOpen = false;
        private readonly ReturnControlToPool ferrymanToPool;

        public PooledControlTagInfo( Control relatedControl, ReturnControlToPool ferrymanToPool ) : base( relatedControl )
        {
            RelatedControl = relatedControl;
            this.ferrymanToPool = ferrymanToPool;
        }

        public void ReturnToPool()
        {
            ClearBeforeReturningToPool();
            this.ferrymanToPool?.Invoke();
        }

        private void ClearBeforeReturningToPool()
        {
            this.RelatedUnionElement = null;
        }

        public delegate void ReturnControlToPool();
    }

    public interface IControlTagInfo
    {
        Control RelatedControl { get; }
        IUnionElement? RelatedUnionElement { get; }
        ErrorProvider RelatedErrorProvider { get; }
    }

    public class CheckedListBoxTagData
    {
        public readonly UnionNode UNode;
        public readonly Dictionary<string, MetaAttribute_Base> MetaAttributes;
        public readonly EditedXmlNode Node; // Updated each time the plus button is pressed to match the node from which the CLB was called
        public readonly XmlVisualizer Vis;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public CheckedListBoxTagData(UnionNode uNode, Dictionary<string, MetaAttribute_Base> metaAttributes, EditedXmlNode node, XmlVisualizer vis )
#pragma warning restore CS8618
        {
            this.UNode = uNode;            
            this.MetaAttributes = metaAttributes;
            this.Node = node;
            this.Vis = vis;
            if ( UNode == null )
                ArcenDebugging.LogSingleLine( "UNode in CheckedListBoxTagData constructor is NULL", Verbosity.ShowAsError );
            if ( MetaAttributes == null )
                ArcenDebugging.LogSingleLine( "MetaAttributes in CheckedListBoxTagData constructor is NULL", Verbosity.ShowAsError );
            if ( Node == null )
                ArcenDebugging.LogSingleLine( "Node in CheckedListBoxTagData constructor is NULL", Verbosity.ShowAsError );
            if ( Vis == null )
                ArcenDebugging.LogSingleLine( "Vis in CheckedListBoxTagData constructor is NULL", Verbosity.ShowAsError );
        }
    }

    public enum Coordinate
    {
        None = -1,
        x = 0,
        y = 1,
        z = 2,
    }
}
