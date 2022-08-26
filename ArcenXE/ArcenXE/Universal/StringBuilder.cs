using System.Globalization;

namespace Arcen.Universal
{
    public class StringBuilder
    {
        private char[] data;
        private int size;

        public int Length { get { return size; } }
        public int Capacity { get { return data.Length; } }

        public char this[int index]
        {
            get { if ( index < 0 || index >= size ) throw new IndexOutOfRangeException(); return data[index]; }
            set { if ( index < 0 || index >= size ) throw new IndexOutOfRangeException(); data[index] = value; }
        }

        public StringBuilder()
        {
            data = new char[16];
            size = 0;
        }

        public StringBuilder( int InitialCapacity )
        {
            data = new char[InitialCapacity];
            size = 0;
        }

        public StringBuilder Clear()
        {
            size = 0;
            return this;
        }

        public StringBuilder Append( string value )
        {
            //handle null
            value ??= "null";

            EnsureCapacity( size + value.Length );
            for ( int i = 0; i < value.Length; i++ )
                data[size++] = value[i];

            return this;
        }

        public StringBuilder Append( char value )
        {
            EnsureCapacity( size + 1 );
            data[size++] = value;
            return this;
        }

        public StringBuilder Append( StringBuilder value )
        {
            if ( value == null )
                return Append( "null" );

            int totalLength = size + value.size;
            EnsureCapacity( totalLength );

            Array.Copy( value.data, 0, data, size, value.size );
            size += value.size;

            return this;
        }

        public StringBuilder Append( bool value )
        {
            return Append( value.ToString() );
        }

        public StringBuilder Append( sbyte value )
        {
            return Append( value.ToString( CultureInfo.CurrentCulture ) );
        }

        public StringBuilder Append( short value )
        {
            return Append( value.ToString( CultureInfo.CurrentCulture ) );
        }

        public StringBuilder Append( byte value )
        {
            return Append( value.ToString( CultureInfo.CurrentCulture ) );
        }

        public StringBuilder Append( int value )
        {
            return Append( value.ToString( CultureInfo.CurrentCulture ) );
        }

        public StringBuilder Append( long value )
        {
            return Append( value.ToString( CultureInfo.CurrentCulture ) );
        }

        public StringBuilder Append( float value )
        {
            return Append( value.ToString( CultureInfo.CurrentCulture ) );
        }

        public StringBuilder Append( double value )
        {
            return Append( value.ToString( CultureInfo.CurrentCulture ) );
        }

        public StringBuilder Append( System.Object value )
        {

            if ( null == value )
            {
                Append( "[null]" );
                return this;
            }
            return Append( value.ToString() );
        }

        public StringBuilder AppendLine()
        {
            return Append( Environment.NewLine );
        }

        public StringBuilder AppendLine( string value )
        {
            return Append( value ).AppendLine();
        }

        public StringBuilder Insert( int index, string value )
        {
            if ( index < 0 || index > size )
                throw new IndexOutOfRangeException();

            //handle null
            value ??= "null";

            int totalLength = size + value.Length;
            EnsureCapacity( totalLength );
            int shiftAmount = size - index;

            //shift down from index to index+value.Length
            Array.Copy( data, index, data, index + value.Length, shiftAmount );

            //copy value
            for ( int i = 0; i < value.Length; i++ )
                data[index + i] = value[i];

            size += value.Length;

            return this;
        }

        public StringBuilder Insert( int index, char value )
        {
            if ( index < 0 || index > size )
                throw new IndexOutOfRangeException();

            int totalLength = size + 1;
            EnsureCapacity( totalLength );
            int shiftAmount = size - index;

            //shift down from index to index+value.Length
            Array.Copy( data, index, data, index + 1, shiftAmount );

            //copy value
            data[index] = value;

            size += 1;

            return this;
        }

        private void EnsureCapacity( int capacity )
        {
            if ( capacity > data.Length )
            {
                //grow by double to amortize growth to O(1)
                char[] newData = new char[Math.Max( capacity, 2 * data.Length )];

                //copy data
                Array.Copy( data, 0, newData, 0, size );
                data = newData;
            }
        }

        public override string ToString()
        {
            return new string( data, 0, size );
        }

        public bool Equals( StringBuilder other )
        {
            if ( other == null )
                return false;

            if ( other == this )
                return true;

            if ( other.Length != Length )
                return false;

            for ( int i = 0; i < other.Length; i++ )
            {
                if ( other[i] != data[i] )
                    return false;
            }
            return true;
        }
    }
}