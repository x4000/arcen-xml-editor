using System;
using System.Globalization;

namespace ArcenXE.Universal
{
    public static class FloatExtensions
    {
        public static bool TryParsePrecise( string str, out float val )
        {
            double dval;
            if ( !double.TryParse( str, NumberStyles.Float | NumberStyles.AllowThousands | NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite,
                NumberFormatInfo.InvariantInfo, out dval ) )
            {
                val = float.NaN;
                return false;
            }
            val = (float)dval;
            return true;
        }
    }
}
