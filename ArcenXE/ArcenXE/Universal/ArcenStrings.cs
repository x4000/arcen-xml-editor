using System;

using System.Text;
using System.IO;
using System.IO.Compression;

using System.Linq.Expressions;
using System.Text.RegularExpressions;

/// <summary>
/// Based on https://docs.unity3d.com/560/Documentation/Manual/BestPracticeUnderstandingPerformanceInUnity5.html
/// </summary>
namespace ArcenXE
{
    public static class ArcenStrings
    {
        public static bool EndsWith( string a, string b )
        {
            if ( a == null || b == null )
                return false;

            int ap = a.Length - 1;
            int bp = b.Length - 1;
            if ( bp > ap )
                return false;

            while ( ap >= 0 && bp >= 0 && a[ap] == b[bp] )
            {
                ap--;
                bp--;
            }
            return (bp < 0 && a.Length >= b.Length) ||

                    (ap < 0 && b.Length >= a.Length);
        }

        public static bool StartsWith( string a, string b )
        {
            if ( a == null || b == null )
                return false;

            int aLen = a.Length;
            int bLen = b.Length;
            if ( bLen > aLen )
                return false;

            int ap = 0; int bp = 0;

            while ( ap < aLen && bp < bLen && a[ap] == b[bp] )
            {
                ap++;
                bp++;
            }

            return (bp == bLen && aLen >= bLen) ||

                    (ap == aLen && bLen >= aLen);
        }

        public static bool Equals( string a, string b )
        {
            if ( a == null || b == null )
                return (a == null && b == null);

            int aLen = a.Length;
            int bLen = b.Length;
            if ( aLen != bLen )
                return false;

            int ap = 0;

            while ( ap < aLen )
            {
                if ( a[ap] != b[ap] )
                    return false;
                ap++;
            }

            return true;
        }

        public static bool EqualsCaseInvariant( string source, string toCheck )
        {
            if ( source == null || toCheck == null )
                return false;
            return source.Equals( toCheck, StringComparison.InvariantCultureIgnoreCase );
        }

        public static bool IsEmpty( string a )
        {
            if ( a == null || a.Length == 0 )
                return true;
            return false;
        }

        public static bool ListContains( List<string> list, string itemToCheckFor )
        {
            for ( int i = 0; i < list.Count; i++ )
            {
                if ( Equals( list[i], itemToCheckFor ) )
                    return true;
            }
            return false;
        }

        public static bool IsNullOrWhiteSpaceSlow( string value )
        {
            return value == null || value.Trim().Length == 0;
        }

        public static bool GetIsHexadecimalDigit( char addedChar )
        {
            bool isHexDigit = false;
            if ( Char.IsDigit( addedChar ) )
                isHexDigit = true;
            if ( Char.IsLetter( addedChar ) )
            {
                switch ( addedChar )
                {
                    case 'a':
                    case 'A':
                    case 'b':
                    case 'B':
                    case 'c':
                    case 'C':
                    case 'd':
                    case 'D':
                    case 'e':
                    case 'E':
                    case 'f':
                    case 'F':
                        isHexDigit = true;
                        break;
                }
            }

            return isHexDigit;
        }

        #region ToRomanNumeral_Inefficient
        /// <summary>
        /// This should be called with great care.  It's based on this simple implementation: https://stackoverflow.com/questions/7040289/converting-integers-to-roman-numerals/23303475
        /// It's fine for the few small cases we typically have.
        /// </summary>
        public static string ToRomanNumeral_Inefficient( int number )
        {
            if ( (number < 0) || (number > 3999) )
                throw new ArgumentOutOfRangeException( "insert value betwheen 1 and 3999" );
            if ( number < 1 ) return string.Empty;
            if ( number >= 1000 ) return "M" + ToRomanNumeral_Inefficient( number - 1000 );
            if ( number >= 900 ) return "CM" + ToRomanNumeral_Inefficient( number - 900 );
            if ( number >= 500 ) return "D" + ToRomanNumeral_Inefficient( number - 500 );
            if ( number >= 400 ) return "CD" + ToRomanNumeral_Inefficient( number - 400 );
            if ( number >= 100 ) return "C" + ToRomanNumeral_Inefficient( number - 100 );
            if ( number >= 90 ) return "XC" + ToRomanNumeral_Inefficient( number - 90 );
            if ( number >= 50 ) return "L" + ToRomanNumeral_Inefficient( number - 50 );
            if ( number >= 40 ) return "XL" + ToRomanNumeral_Inefficient( number - 40 );
            if ( number >= 10 ) return "X" + ToRomanNumeral_Inefficient( number - 10 );
            if ( number >= 9 ) return "IX" + ToRomanNumeral_Inefficient( number - 9 );
            if ( number >= 5 ) return "V" + ToRomanNumeral_Inefficient( number - 5 );
            if ( number >= 4 ) return "IV" + ToRomanNumeral_Inefficient( number - 4 );
            if ( number >= 1 ) return "I" + ToRomanNumeral_Inefficient( number - 1 );
            throw new ArgumentOutOfRangeException( "something bad happened" );
        }
        #endregion

        private static List<string> invalidPathChars;
        public static string[] invalidFilenames = new string[] { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        public static List<string> GetInvalidPathChars()
        {
            if ( invalidPathChars != null )
                return invalidPathChars;

            char[] chars = Path.GetInvalidPathChars();
            invalidPathChars = new List<string>( chars.Length + 10 );

            for ( int i = 0; i < chars.Length; i++ )
                invalidPathChars.Add( chars[i].ToString() );

            chars = Path.GetInvalidFileNameChars();
            string s;
            for ( int i = 0; i < chars.Length; i++ )
            {
                s = chars[i].ToString();
                if ( !invalidPathChars.Contains( s ) )
                    invalidPathChars.Add( s );
            }

            if ( !invalidPathChars.Contains( "/" ) )
                invalidPathChars.Add( "/" );
            if ( !invalidPathChars.Contains( @"\" ) )
                invalidPathChars.Add( @"\" );
            if ( !invalidPathChars.Contains( "\t" ) )
                invalidPathChars.Add( "\t" );
            if ( !invalidPathChars.Contains( "." ) )
                invalidPathChars.Add( "." );
            if ( !invalidPathChars.Contains( "," ) )
                invalidPathChars.Add( "," );
            if ( !invalidPathChars.Contains( "'" ) )
                invalidPathChars.Add( "'" );
            if ( !invalidPathChars.Contains( "\"" ) )
                invalidPathChars.Add( "\"" );

            return invalidPathChars;
        }

        public static bool ValidateFilename( string Filename )
        {
            List<string> invalidPathChars = GetInvalidPathChars();
            for ( int i = 0; i < invalidPathChars.Count; i++ )
            {
                if ( Filename.Contains( invalidPathChars[i] ) )
                {
                    ArcenDebugging.LogWithStack(
                        String.Format( "Unfortunately, '{0}' is not a valid filename, because the character '{1}' cannot be used in filenames.", Filename, invalidPathChars[i] ), Verbosity.ShowAsError );
                    return false;
                }
            }

            string upperFilename = Filename.ToUpper();

            for ( int i = 0; i < invalidFilenames.Length; i++ )
            {
                if ( upperFilename == invalidFilenames[i] )
                {
                    ArcenDebugging.LogWithStack(
                        String.Format( "Unfortunately, '{0}' is not a valid filename, because the filename is a restricted windows filename.", upperFilename ), Verbosity.ShowAsError );
                    return false;
                }
            }
            return true;
        }

        public static string MakeValidFilename( string Filename, bool CareAboutInvalidFilenameMinusExtensionList )
        {
            List<string> invalidPathChars = GetInvalidPathChars();
            for ( int i = 0; i < invalidPathChars.Count; i++ )
                Filename = Filename.Replace( invalidPathChars[i], "_" );

            if ( CareAboutInvalidFilenameMinusExtensionList )
            {
                string upperFilename = Filename.ToUpper();

                for ( int i = 0; i < invalidFilenames.Length; i++ )
                {
                    if ( upperFilename == invalidFilenames[i] )
                    {
                        Filename += "__";
                        break;
                    }
                }
            }
            return Filename;
        }

        #region GetStringAsEnum
        public static T GetStringAsEnum<T>( string strValue, T DefaultValue, string DebugName )
        {
            try
            {
                return (T)Enum.Parse( typeof( T ), strValue, true );
            }
            catch
            {
                ArcenDebugging.LogSingleLine( "Trying to Get Enum: Unknown enum '" + typeof( T ) + "' '" + DebugName + "'" + "' (" + strValue + ")", Verbosity.ShowAsError );
            }

            return DefaultValue;
        }
        #endregion

        public static bool DoesStringStartWithVowel( string str )
        {
            char firstChar = str[0];
            bool isVowel = "aeiouAEIOU".IndexOf( firstChar ) >= 0;
            return isVowel;
        }

        #region GetAbbreviationFromThreadName
        public static string GetAbbreviationFromThreadName( string LongerThreadName )
        {
            StringBuilder briefBuilder = new StringBuilder();
            bool priorWasLowercaseOrSpace = true;
            foreach ( char c in LongerThreadName )
            {
                if ( c == ' ' )
                {
                    priorWasLowercaseOrSpace = true;
                    continue;
                }

                bool isUpper = char.IsUpper( c );
                if ( priorWasLowercaseOrSpace )
                {
                    if ( isUpper )
                    {
                        briefBuilder.Append( c );
                        priorWasLowercaseOrSpace = false;
                        continue;
                    }
                }

                priorWasLowercaseOrSpace = !isUpper;
            }
            return briefBuilder.ToString();
        }
        #endregion
    }
}