using ArcenXE.Utilities.XmlDataProcessing;
using ArcenXE.Universal;

namespace ArcenXE.Utilities
{
    public class ControlTagInfo : IControlTagInfo
    {
        public IUnionElement? RelatedUnionElement { get; set; } = null;
        private readonly Control relatedControl;
        public Control RelatedControl { get => relatedControl; protected init => relatedControl = value; }

        public ControlTagInfo( Control relatedControl )
        {
            this.relatedControl = relatedControl;
        }
    }

    public class PooledControlTagInfo : ControlTagInfo
    {
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
        IUnionElement? RelatedUnionElement { get; set; }
    }
}
