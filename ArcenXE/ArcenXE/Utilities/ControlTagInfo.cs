using ArcenXE.Utilities.XmlDataProcessing;
using ArcenXE.Universal;

namespace ArcenXE.Utilities
{
    public class PooledControlTagInfo : IControlTagInfo
    {
        public IEditedXmlElement? RelatedTo { get; set; } = null;
        private readonly ReturnControlToPool ferrymanToPool;
        private readonly Control relatedControl;
        public Control RelatedControl { get => relatedControl; }

        public PooledControlTagInfo( Control relatedControl, ReturnControlToPool ferrymanToPool )
        {
            this.relatedControl = relatedControl;
            this.ferrymanToPool = ferrymanToPool;
        }

        public void ReturnToPool()
        {
            if ( ferrymanToPool != null )
                this.ferrymanToPool();
        }

        public delegate void ReturnControlToPool();
    }

    public interface IControlTagInfo
    {
        IEditedXmlElement? RelatedTo { get; set; }
        Control RelatedControl { get; }
    }
}
