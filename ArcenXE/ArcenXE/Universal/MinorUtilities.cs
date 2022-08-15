using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArcenXE.Universal
{
    public static class MinorUtilities
    {
        public static int CalculateNumberOfDigits( int n )
        {
            if ( n == 0 )
                return 1;
            else
                return (n > 0 ? 1 : 2) + (int)Math.Log10( Math.Abs( (double)n ) );
        }
    }
}
