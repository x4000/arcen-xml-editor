namespace ArcenXE.Universal
{
    public struct ArcenPoint
    {
        public int X;
        public int Y;

        public static ArcenPoint Create( int X, int Y )
        {
            ArcenPoint p;
            p.X = X;
            p.Y = Y;
            return p;
        }

        public override bool Equals( object? obj )
        {
            if ( obj == null || obj is not ArcenPoint )
                return false;

            ArcenPoint other = (ArcenPoint)obj;
            return this.X == other.X && this.Y == other.Y;
        }

        public override int GetHashCode()
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = (hash * 23) + this.X.GetHashCode();
                hash = (hash * 23) + this.Y.GetHashCode();
                return hash;
            }
        }

        #region ==
        public static bool operator ==( ArcenPoint one, ArcenPoint other )
        {
            return one.X == other.X && one.Y == other.Y;
        }
        #endregion

        #region !=
        public static bool operator !=( ArcenPoint one, ArcenPoint other )
        {
            return one.X != other.X || one.Y != other.Y;
        }
        #endregion
    }
}
