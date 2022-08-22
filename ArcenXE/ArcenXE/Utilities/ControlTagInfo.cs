using ArcenXE.Utilities.XmlDataProcessing;
using ArcenXE.Universal;

namespace ArcenXE.Utilities
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
        public Coordinate ControlsCoordinate = Coordinate.None; // only used by numerical updown controls
        private readonly ReturnControlToPool ferrymanToPool;        
        
        public PooledControlTagInfo( Control relatedControl, ReturnControlToPool ferrymanToPool ) : base( relatedControl )
        {
            base.RelatedControl = relatedControl;
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

    public enum Coordinate
    {
        None = -1,
        x = 0,
        y = 1,
        z = 2,
    }
}
