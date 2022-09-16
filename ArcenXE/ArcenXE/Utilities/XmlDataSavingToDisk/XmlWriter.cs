using System.Numerics;
using System.Text;
using ArcenXE.Universal;

namespace ArcenXE.Utilities.XmlDataSavingToDisk
{
    public class XmlWriter
    {
        private readonly StringBuilder output = new StringBuilder();

        private readonly Stack<string> whiteSpaceOffset = new Stack<string>();
        private string currentLeadingWhitespace = string.Empty;
        public const ushort MaxPixelsPerLineBeforeLineBreak = 600;
        private ushort pixelsOnCurrentLine = 0;

        /// <summary>
        /// This is the windows style of newline, aka CR LF (carriage return = \r, line feed = \n)
        /// </summary>
        private const string _NEWLINE = "\r\n";
        private string GetNewLineAndResetCharsOnCurrentLine()
        {
            pixelsOnCurrentLine = 0;
            return _NEWLINE;
        }
        public ushort GetPixelsOnCurrentLine() => pixelsOnCurrentLine;

        public XmlWriter()
        {
            output.Append( "<?xml version=\"1.0\" encoding=\"utf-8\"?>" );
            output.Append( GetNewLineAndResetCharsOnCurrentLine() );
        }

        #region CalculateIfNewLineIsRequired
        public bool CalculateIfNewLineIsRequired( ushort pixelsToBeAdded )
        {
            //ArcenDebugging.LogSingleLine( $"pixelsOnCurrentLine = {pixelsOnCurrentLine}\t\tpixelsToBeAdded = {pixelsToBeAdded}", Verbosity.DoNotShow );
            if ( pixelsOnCurrentLine + pixelsToBeAdded > MaxPixelsPerLineBeforeLineBreak ) //check
            {
                pixelsOnCurrentLine = pixelsToBeAdded;
                return true;
            }
            else
            {
                pixelsOnCurrentLine += pixelsToBeAdded;
                return false;
            }
        }

        public void IncrementPixelsOnCurrentLine( ushort pixelsToBeAdded )
        {
            pixelsOnCurrentLine += pixelsToBeAdded;
        }
        #endregion

        #region IncrementWhitespace
        public XmlWriter IncrementWhitespace()
        {
            if ( whiteSpaceOffset.Count > 0 )
            {
                string newWhiteSpace = whiteSpaceOffset.Peek();
                newWhiteSpace += "	"; //this is a tab, it's equal to the one below
                whiteSpaceOffset.Push( newWhiteSpace );
                this.currentLeadingWhitespace = newWhiteSpace;
            }
            else
            {
                string newWhiteSpace = "	"; //this is a tab, it's equal to the one above
                whiteSpaceOffset.Push( newWhiteSpace );
                this.currentLeadingWhitespace = newWhiteSpace;
            }
            return this;
        }
        #endregion

        #region DecrementWhitespace
        public XmlWriter DecrementWhitespace()
        {
            if ( whiteSpaceOffset.Count > 0 )
            {
                whiteSpaceOffset.Pop(); //toss away the current
                if ( whiteSpaceOffset.Count > 0 )
                    this.currentLeadingWhitespace = whiteSpaceOffset.Peek();
                else
                    this.currentLeadingWhitespace = string.Empty;
            }
            else
                ArcenDebugging.LogWithStack( "Tried to DecrementWhitespace in ArcenXMLWriter when there was nothing to decrement!", Verbosity.ShowAsError );
            return this;
        }
        #endregion

        #region NewLine
        public XmlWriter NewLine( XmlLeadingWhitespace WhitespaceUse )
        {
            output.Append( GetNewLineAndResetCharsOnCurrentLine() );
            if ( WhitespaceUse == XmlLeadingWhitespace.IncludeAfter )
                output.Append( currentLeadingWhitespace );
            return this;
        }
        #endregion

        #region StartOpenNode
        public XmlWriter StartOpenNode( string StartNodeTag, bool FinishOpenNodeNow )
        {
            output.Append( currentLeadingWhitespace ).Append( "<" ).Append( StartNodeTag );
            if ( FinishOpenNodeNow )
                output.Append( ">" );
            IncrementWhitespace();
            return this;
        }
        #endregion

        #region FinishOpenNode
        public XmlWriter FinishOpenNode( bool IncludeNewline )
        {
            output.Append( ">" );
            if ( IncludeNewline )
                output.Append( GetNewLineAndResetCharsOnCurrentLine() );
            return this;
        }
        #endregion

        #region FinishOpenNodeAndSelfClose
        public XmlWriter FinishOpenNodeAndSelfClose()
        {
            output.Append( "/>" );
            DecrementWhitespace();
            NewLine( XmlLeadingWhitespace.None );
            return this;
        }
        #endregion

        #region CloseNode
        /// <summary>
        /// Rather than having a stack where we pop and end the prior Node, it's much easier for us to read the code
        /// if the code has to explicitly state what the end Node is again.  Otherwise that was going in comments, anyhow.
        /// </summary>
        public XmlWriter CloseNode( string CloseNodeTag, bool IncludeLeadingWhitespace )
        {
            DecrementWhitespace();

            if ( IncludeLeadingWhitespace )
                output.Append( currentLeadingWhitespace );
            output.Append( "</" ).Append( CloseNodeTag ).Append( ">" );
            NewLine( XmlLeadingWhitespace.None );
            return this;
        }
        #endregion

        #region HandleAttributeLeadInOut
        public XmlWriter HandleAttributeLeadInOut( XmlAttLeadInOut Lead )
        {
            switch ( Lead )
            {
                case XmlAttLeadInOut.Space:
                    output.Append( " " );
                    break;
                case XmlAttLeadInOut.Linebreak:
                    output.Append( GetNewLineAndResetCharsOnCurrentLine() );
                    output.Append( currentLeadingWhitespace );
                    break;
            }
            return this;
        }
        #endregion

        #region String
        public XmlWriter StringAttributeIfNot( XmlAttLeadInOut LeadIn, string AttributeTag, string Value, string ValueToIgnore )
        {
            if ( Value == ValueToIgnore )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" ).Append( Value ).Append( "\"" );
            return this;
        }

        public XmlWriter StringAttribute( XmlAttLeadInOut LeadIn, string AttributeTag, string Value )
        {
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" ).Append( Value ).Append( "\"" );
            return this;
        }
        #endregion

        #region Float
        public XmlWriter FloatAttributeIfNot( XmlAttLeadInOut LeadIn, string AttributeTag, float Value, float ValueToIgnore, int Digits )
        {
            if ( Value == ValueToIgnore )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" ).Append( GetFloatStringForXml( Value, Digits ) ).Append( "\"" );
            return this;
        }

        public XmlWriter FloatAttribute( XmlAttLeadInOut LeadIn, string AttributeTag, float Value, int Digits )
        {
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" ).Append( GetFloatStringForXml( Value, Digits ) ).Append( "\"" );
            return this;
        }

        public XmlWriter FloatAttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, IList<float> List, int Digits )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( "," );
                float val = List[i];

                output.Append( GetFloatStringForXml( val, Digits ) );
            }
            output.Append( "\"" );
            return this;
        }

        public XmlWriter FloatAttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, List<float> List, int Digits )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( "," );
                float val = List[i];

                output.Append( GetFloatStringForXml( val, Digits ) );
            }
            output.Append( "\"" );
            return this;
        }
        #endregion

        #region Vector2
        public XmlWriter Vector2AttributeIfNot( XmlAttLeadInOut LeadIn, string AttributeTag, Vector2 Value, Vector2 ValueToIgnore, int Digits )
        {
            if ( Value == ValueToIgnore )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" )
               .Append( GetFloatStringForXml( Value.X, Digits ) )
               .Append( "," )
               .Append( GetFloatStringForXml( Value.Y, Digits ) ).Append( "\"" );
            return this;
        }

        public XmlWriter Vector2Attribute( XmlAttLeadInOut LeadIn, string AttributeTag, Vector2 Value, int Digits )
        {
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" )
               .Append( GetFloatStringForXml( Value.X, Digits ) )
               .Append( "," )
               .Append( GetFloatStringForXml( Value.Y, Digits ) ).Append( "\"" );
            return this;
        }

        public XmlWriter Vector2AttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, IList<Vector2> List, int Digits )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( ";" );
                Vector2 val = List[i];

                output.Append( GetFloatStringForXml( val.X, Digits ) )
                    .Append( "," )
                    .Append( GetFloatStringForXml( val.Y, Digits ) );
            }
            output.Append( "\"" );
            return this;
        }

        public XmlWriter Vector2AttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, List<Vector2> List, int Digits )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( ";" );
                Vector2 val = List[i];

                output.Append( GetFloatStringForXml( val.X, Digits ) )
                    .Append( "," )
                    .Append( GetFloatStringForXml( val.Y, Digits ) );
            }
            output.Append( "\"" );
            return this;
        }
        #endregion

        #region Vector3
        public XmlWriter Vector3AttributeIfNot( XmlAttLeadInOut LeadIn, string AttributeTag, Vector3 Value, Vector3 ValueToIgnore, int Digits )
        {
            if ( Value == ValueToIgnore )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" )
               .Append( GetFloatStringForXml( Value.X, Digits ) )
               .Append( "," )
               .Append( GetFloatStringForXml( Value.Y, Digits ) )
               .Append( "," )
               .Append( GetFloatStringForXml( Value.Z, Digits ) ).Append( "\"" );
            return this;
        }

        public XmlWriter Vector3Attribute( XmlAttLeadInOut LeadIn, string AttributeTag, Vector3 Value, int Digits )
        {
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" )
                .Append( GetFloatStringForXml( Value.X, Digits ) )
                .Append( "," )
                .Append( GetFloatStringForXml( Value.Y, Digits ) )
                .Append( "," )
                .Append( GetFloatStringForXml( Value.Z, Digits ) ).Append( "\"" );
            return this;
        }

        public XmlWriter Vector3AttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, IList<Vector3> List, int Digits )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( ";" );
                Vector3 val = List[i];

                output.Append( GetFloatStringForXml( val.X, Digits ) )
                    .Append( "," )
                    .Append( GetFloatStringForXml( val.Y, Digits ) )
                    .Append( "," )
                    .Append( GetFloatStringForXml( val.Z, Digits ) );
            }
            output.Append( "\"" );
            return this;
        }

        public XmlWriter Vector3AttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, List<Vector3> List, int Digits )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( ";" );
                Vector3 val = List[i];

                output.Append( GetFloatStringForXml( val.X, Digits ) )
                    .Append( "," )
                    .Append( GetFloatStringForXml( val.Y, Digits ) )
                    .Append( "," )
                    .Append( GetFloatStringForXml( val.Z, Digits ) );
            }
            output.Append( "\"" );
            return this;
        }
        #endregion

        #region Point
        public XmlWriter PointAttributeIfNot( XmlAttLeadInOut LeadIn, string AttributeTag, ArcenPoint Value, ArcenPoint ValueToIgnore )
        {
            if ( Value == ValueToIgnore )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" )
               .Append( Value.X )
               .Append( "," )
               .Append( Value.Y ).Append( "\"" );
            return this;
        }

        public XmlWriter PointAttribute( XmlAttLeadInOut LeadIn, string AttributeTag, ArcenPoint Value )
        {
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" )
               .Append( Value.X )
               .Append( "," )
               .Append( Value.Y ).Append( "\"" );
            return this;
        }

        public XmlWriter PointAttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, IList<ArcenPoint> List )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( ";" );
                ArcenPoint val = List[i];

                output.Append( val.X )
                    .Append( "," )
                    .Append( val.Y );
            }
            output.Append( "\"" );
            return this;
        }

        public XmlWriter PointAttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, List<ArcenPoint> List )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( ";" );
                ArcenPoint val = List[i];

                output.Append( val.X )
                    .Append( "," )
                    .Append( val.Y );
            }
            output.Append( "\"" );
            return this;
        }
        #endregion

        #region Int
        public XmlWriter IntAttributeIfNot( XmlAttLeadInOut LeadIn, string AttributeTag, int Value, int ValueToIgnore )
        {
            if ( Value == ValueToIgnore )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" ).Append( Value ).Append( "\"" );
            return this;
        }

        public XmlWriter IntAttribute( XmlAttLeadInOut LeadIn, string AttributeTag, int Value )
        {
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" ).Append( Value ).Append( "\"" );
            return this;
        }

        public XmlWriter IntAttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, IList<int> List )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( "," );
                int val = List[i];
                output.Append( val );
            }
            output.Append( "\"" );
            return this;
        }

        public XmlWriter IntAttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, List<int> List )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( "," );
                int val = List[i];
                output.Append( val );
            }
            output.Append( "\"" );
            return this;
        }
        #endregion

        #region Bool
        public XmlWriter BoolAttributeIfNot( XmlAttLeadInOut LeadIn, string AttributeTag, bool Value, bool ValueToIgnore )
        {
            if ( Value == ValueToIgnore )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" ).Append( Value ? "true" : "false" ).Append( "\"" );
            return this;
        }

        public XmlWriter BoolAttribute( XmlAttLeadInOut LeadIn, string AttributeTag, bool Value )
        {
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" ).Append( Value ? "true" : "false" ).Append( "\"" );
            return this;
        }

        public XmlWriter BoolAttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, IList<bool> List )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( "," );
                bool val = List[i];
                output.Append( val ? "true" : "false" );
            }
            output.Append( "\"" );
            return this;
        }

        public XmlWriter BoolAttributeList( XmlAttLeadInOut LeadIn, string AttributeTag, List<bool> List )
        {
            if ( List == null || List.Count == 0 )
                return this;
            HandleAttributeLeadInOut( LeadIn );
            output.Append( AttributeTag ).Append( "=\"" );

            for ( int i = 0; i < List.Count; i++ )
            {
                if ( i > 0 )
                    output.Append( "," );
                bool val = List[i];
                output.Append( val ? "true" : "false" );
            }
            output.Append( "\"" );
            return this;
        }
        #endregion

        #region GetFloatStringForXml
        public string GetFloatStringForXml( float Value, int Digits )
        {
            Value = (float)Math.Round( Value, Digits );
            return Value.ToString( "0.###", System.Globalization.CultureInfo.InvariantCulture );
        }
        #endregion

        #region GetFinishedXmlDocument
        public void AddCompleteNode( string node )
        {
            output.Append( node );
            this.NewLine( XmlLeadingWhitespace.None );
        }
        #endregion

        #region GetFinishedXmlDocument
        public string GetFinishedXmlDocument()
        {
            return this.output.ToString();
        }
        #endregion
    }

    public enum XmlLeadingWhitespace
    {
        None,
        IncludeAfter,
    }

    public enum XmlAttLeadInOut
    {
        None,
        Space,
        Linebreak,
    }
}
