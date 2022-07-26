using System.Collections.Concurrent;

namespace ArcenXE
{
    public static class ArcenDebugging
    {
        public static WhatToDoWhenAssertionFailsMode AssertionFailureMode = WhatToDoWhenAssertionFailsMode.LogToDebugLogAndLocalMessages;

        public static void ProcessAssertionFailure( string Message )
        {
            switch ( AssertionFailureMode )
            {
                case WhatToDoWhenAssertionFailsMode.Nothing:
                    break;
                case WhatToDoWhenAssertionFailsMode.LogToDebugLog:
                    LogWithStack( Message, Verbosity.DoNotShow );
                    break;
                case WhatToDoWhenAssertionFailsMode.LogToDebugLogAndLocalMessages:
                    LogWithStack( Message, Verbosity.DoNotShow );
                    break;
                case WhatToDoWhenAssertionFailsMode.ThrowException:
                    throw new Exception( Message );
            }
        }

        #region Stopwatch
        private static System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();
        private static int swClickOns = 0;

        public static void StartStopwatch()
        {
            sw.Start();
            swClickOns++;
        }

        public static void StopStopwatch()
        {
            sw.Stop();
        }

        public static void ResetStopwatch()
        {
            sw.Reset();
            swClickOns = 0;
        }

        public static float GetStopwatchMilliseconds()
        {
            return sw.ElapsedMilliseconds;
        }

        public static void WriteStopwatchTime( bool SingleLine )
        {
            if ( SingleLine )
                LogSingleLine( sw.ElapsedMilliseconds + "ms   " + swClickOns + " click-ons", Verbosity.ShowAsWarning );
            else
                LogWithStack( sw.ElapsedMilliseconds + "ms   " + swClickOns + " click-ons", Verbosity.ShowAsWarning );
        }
        #endregion

        public enum WhatToDoWhenAssertionFailsMode
        {
            ThrowException,
            LogToDebugLogAndLocalMessages,
            LogToDebugLog,
            Nothing
        }

        public static Int64 ErrorSinceStart = 0;

        #region Thread Error States
        private static ConcurrentDictionary<int, bool> threadErrorStates = new ConcurrentDictionary<int, bool>( 4, 1000 );

        public static void SetCurrentThreadHasHadError( bool HasHadError )
        {
            int hashCode = Thread.CurrentThread.GetHashCode();
            threadErrorStates[hashCode] = HasHadError;
        }

        public static void SetThreadHasHadError( Thread T, bool HasHadError )
        {
            int hashCode = T.GetHashCode();
            threadErrorStates[hashCode] = HasHadError;
        }
        public static bool GetThreadHasHadError( Thread T )
        {
            int hashCode = T.GetHashCode();
            bool hasHadError = false;
            if ( threadErrorStates.TryGetValue( hashCode, out hasHadError ) )
                return hasHadError;
            return false;
        }
        public static bool GetCurrentThreadHasHadError()
        {
            int hashCode = Thread.CurrentThread.GetHashCode();
            bool hasHadError = false;
            if ( threadErrorStates.TryGetValue( hashCode, out hasHadError ) )
                return hasHadError;
            return false;
        }
        #endregion

        private static ConcurrentQueue<DelayedMessagePayLoad> delayedLines = new ConcurrentQueue<DelayedMessagePayLoad>();

        #region LogSingleLineDelayed
        private static int delayedIndexIDCreator = 0;
        private static void LogSingleLineDelayed( string Message, Verbosity Verbosity )
        {
            if ( Verbosity == Verbosity.ShowAsError )
            {
                if ( ArcenDebugging.ShouldSkipErrorMessage( Message ) )
                    return; //don't bother logging these, either
            }

            DelayedMessagePayLoad message;
            message.Timestamp = DateTime.Now;
            message.Message = Message;
            message.Verbosity = Verbosity;
            message.DelayedIndex = System.Threading.Interlocked.Add( ref delayedIndexIDCreator, 1 );
            message.DelayedThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;

            delayedLines.Enqueue( message );
        }
        #endregion

        #region LogSingleLineError
        private static void LogSingleLineError( string Message, Verbosity Verbosity )
        {
            if ( ArcenDebugging.ShouldSkipErrorMessage( Message ) )
                return; //don't bother logging these, either

            DelayedMessagePayLoad message;
            message.Timestamp = DateTime.Now;
            message.Message = Message;
            message.Verbosity = Verbosity;
            message.DelayedIndex = System.Threading.Interlocked.Add( ref delayedIndexIDCreator, 1 );
            message.DelayedThreadID = System.Threading.Thread.CurrentThread.ManagedThreadId;

            delayedLines.Enqueue( message );
        }
        #endregion

        private static List<DelayedMessagePayLoad> workingDelayedLinesToWriteMainThreadOnly = new List<DelayedMessagePayLoad>( 400 );

        public static void DumpAllPriorDelayedSingleLines()
        {
            if ( delayedLines.IsEmpty )
                return;
            if ( !Program.CalculateIsCurrentThreadMainThead() )
                return; //if we're not on the main thread, don't do a dump!  This was called in error

            workingDelayedLinesToWriteMainThreadOnly.Clear();

            while ( true )
            {
                if ( !delayedLines.TryDequeue( out DelayedMessagePayLoad message ) )
                    break;
                workingDelayedLinesToWriteMainThreadOnly.Add( message );
            }

            if ( workingDelayedLinesToWriteMainThreadOnly.Count > 0 )
            {
                workingDelayedLinesToWriteMainThreadOnly.Sort( delegate ( DelayedMessagePayLoad L, DelayedMessagePayLoad R )
                {
                    return L.DelayedIndex.CompareTo( R.DelayedIndex );
                } );
                foreach ( DelayedMessagePayLoad message in workingDelayedLinesToWriteMainThreadOnly )
                {
                    ArcenLog_InnerOnMainThreadOnly( "DELAYED" + message.DelayedIndex + " TID" + message.DelayedThreadID + " " +
                        message.Message, DebugLogDestination.XEDebugLog, false, message.Verbosity, message.Timestamp );
                }
                workingDelayedLinesToWriteMainThreadOnly.Clear();
            }
        }

        public static void LogErrorWithStack( Exception Exception )
        {
            string message = Exception.ToString() + Environment.NewLine + Environment.NewLine + Exception.StackTrace;
            if ( !Program.CalculateIsCurrentThreadMainThead() )
            {
                LogSingleLineDelayed( message, Verbosity.ShowAsError );
                return; //if we're not on the main thread, then we need to do a delayed write!  Regular writes will sometimes get lost
            }

            ArcenLog_InnerOnMainThreadOnly( message, DebugLogDestination.XEDebugLog, false, Verbosity.ShowAsError, DateTime.Now );
        }

        public static void LogSilentWithStack( string Message )
        {
            if ( !Program.CalculateIsCurrentThreadMainThead() )
            {
                Message += Environment.NewLine + Environment.StackTrace + Environment.NewLine;
                LogSingleLineDelayed( Message, Verbosity.DoNotShow );
                return; //if we're not on the main thread, then we need to do a delayed write and add the stack trace here!  Regular writes will sometimes get lost
            }
            ArcenLog_InnerOnMainThreadOnly( Message, DebugLogDestination.XEDebugLog, true, Verbosity.DoNotShow, DateTime.Now );
        }

        public static void LogDebugStageWithStack( string Header, int DebugStage, Exception e, Verbosity Verbosity )
        {
            LogDebugStageWithStack( Header, DebugStage, null, e, Verbosity );
        }

        public static void LogDebugStageWithStack( string Header, int DebugStage, string? AddedMessage, Exception e, Verbosity Verbosity )
        {
            string fullMessage = " '" + Header + "' error at DebugStage " + DebugStage + "\r\n" +
                (AddedMessage == null || AddedMessage.Length <= 0 ? string.Empty : AddedMessage + "\r\n") +
                e.ToString();

            LogSingleLine( fullMessage, Verbosity ); //don't double-include the stack, since the error already has that in there
        }

        public static void LogDebugStageWithStack( string Header, int DebugStage, string AddedMessage, string ExceptionText, Verbosity Verbosity )
        {
            string fullMessage = " '" + Header + "' error at DebugStage " + DebugStage + "\r\n" +
                (AddedMessage == null || AddedMessage.Length <= 0 ? string.Empty : AddedMessage + "\r\n") +
                ExceptionText;

            LogWithStack( fullMessage, Verbosity ); //do add the stack since we don't know if the exception text has it or not
        }

        public static void LogWithStack( string Message, Verbosity Verbosity )
        {
            if ( Verbosity == Verbosity.ShowAsError )
            {
                if ( ArcenDebugging.ShouldSkipErrorMessage( Message ) )
                    return; //don't bother logging these, either
            }
            if ( !Program.CalculateIsCurrentThreadMainThead() )
            {
                Message += Environment.NewLine + Environment.StackTrace + Environment.NewLine;
                LogSingleLineDelayed( Message, Verbosity );
                return; //if we're not on the main thread, then we need to do a delayed write and add the stack trace here!  Regular writes will sometimes get lost
            }
            ArcenLog_InnerOnMainThreadOnly( Message, DebugLogDestination.XEDebugLog, true, Verbosity, DateTime.Now );
        }

        public static void LogSingleLine( string Message, Verbosity Verbosity )
        {
            if ( Message == null || Message.Length == 0 )
                return;
            if ( Verbosity == Verbosity.ShowAsError )
            {
                if ( ArcenDebugging.ShouldSkipErrorMessage( Message ) )
                    return; //don't bother logging these, either
            }
            if ( !Program.CalculateIsCurrentThreadMainThead() )
            {
                LogSingleLineDelayed( Message, Verbosity );
                return; //if we're not on the main thread, then we need to do a delayed write!  Regular writes will sometimes get lost
            }
            ArcenLog_InnerOnMainThreadOnly( Message, DebugLogDestination.XEDebugLog, false, Verbosity, DateTime.Now );
        }

        private static void LogToConsole( string Message )
        {
            Console.WriteLine( Message );
        }

        private static void ArcenLog_InnerOnMainThreadOnly( string Message, DebugLogDestination Destination, bool IncludeStackTrace, Verbosity Verbosity, DateTime Timestamp )
        {
            bool isMainThread = Program.CalculateIsCurrentThreadMainThead();
            bool isEditor = false;
            if ( isMainThread || isEditor )
            {
                if ( Verbosity != Verbosity.DoNotShowAndDoNotLogToConsole )
                {
                    if ( !isMainThread )
                        LogToConsole( Message + "\n\n" + Environment.StackTrace );
                    else
                    {
                        if ( isEditor )
                            LogToConsole( Message );
                        else
                        {
                            switch ( Verbosity )
                            {
                                case Verbosity.ShowAsError:
                                case Verbosity.DoNotShowButLogToConsole:
                                    LogToConsole( Message );
                                    break;
                            }

                        }
                    }
                }
            }
            if ( Verbosity == Verbosity.ShowAsError )
            {
                //yes we've had an error
                SetThreadHasHadError( Thread.CurrentThread, true );
                ErrorSinceStart++;
            }
            try
            {
                if ( ArcenStrings.IsEmpty( Program.CurrentLogDirectory ) )
                {
                    if ( ShouldSkipErrorMessage( Message ) )
                        return;

                    LogToConsole( Message ); //before CurrentLogDirectory is initialized, log to debug log instead
                }
                else
                {
                    string fullMessageNoStack = Timestamp.ToString();
                    string fullMessageMaybeStack = fullMessageNoStack;

                    fullMessageNoStack += "\t" + Message;
                    fullMessageMaybeStack += "\t" + Message;

                    if ( IncludeStackTrace )
                        fullMessageMaybeStack += Environment.NewLine + Environment.StackTrace + Environment.NewLine;
                    fullMessageMaybeStack += Environment.NewLine;
                    fullMessageNoStack += Environment.NewLine;

                    if ( ShouldSkipErrorMessage( fullMessageNoStack ) )
                        return;

                    Program.AppendTextToFile( Program.CurrentLogDirectory + Destination.ToString() + ".txt", fullMessageMaybeStack, Program.MAX_ERROR_FILE_SIZE );
                    MainWindow.Instance.ErrorsWrittenToLog++;
                    switch ( Verbosity )
                    {
                        //todo
                        case Verbosity.ShowAsError:
                            MessageBox.Show( Message, fullMessageNoStack, MessageBoxButtons.OK, MessageBoxIcon.Error );
                            break;
                        case Verbosity.ShowAsInfo:
                            MessageBox.Show( Message, fullMessageNoStack, MessageBoxButtons.OK, MessageBoxIcon.Information );
                            break;
                        case Verbosity.ShowAsWarning:
                            MessageBox.Show( Message, fullMessageNoStack, MessageBoxButtons.OK, MessageBoxIcon.Warning );
                            break;
                    }
                }
            }
            catch
            {
                // don't crash; this can be thrown if the user has the file open in another program that doesn't share well
            }
        }

        internal static bool ShouldSkipErrorMessage( string Message )
        {
            if ( Message == null || Message.Length == 0 )
                return true;
            if ( Message.Contains( "data archive" ) )
                return true;
            return false;
        }

        public static void LogNoDateOrAnything( string Message, DebugLogDestination Destination, Verbosity Verbosity )
        {
            if ( ShouldSkipErrorMessage( Message ) )
                return;

            bool isMainThread = Program.CalculateIsCurrentThreadMainThead();
            bool isEditor = false;
            if ( isMainThread || isEditor )
            {
                if ( Verbosity != Verbosity.DoNotShowAndDoNotLogToConsole )
                {
                    if ( !isMainThread )
                        LogToConsole( Message + "\n\n" + Environment.StackTrace );
                    else
                    {
                        if ( isEditor )
                            LogToConsole( Message );
                        else
                        {
                            switch ( Verbosity )
                            {
                                case Verbosity.ShowAsError:
                                case Verbosity.DoNotShowButLogToConsole:
                                    LogToConsole( Message );
                                    break;
                            }

                        }
                    }
                }
            }
            if ( Verbosity == Verbosity.ShowAsError )
            {
                //yes we've had an error
                SetThreadHasHadError( Thread.CurrentThread, true );
                ErrorSinceStart++;
            }
            try
            {
                if ( ArcenStrings.IsEmpty( Program.CurrentLogDirectory ) )
                {
                    LogToConsole( Message ); //before CurrentLogDirectory is initialized, log to debug log instead
                }
                else
                {
                    Program.AppendTextToFile( Program.CurrentLogDirectory + Destination.ToString() + ".txt",
                        Environment.NewLine + Message, Program.MAX_BIG_ERROR_FILE_SIZE );
                    MainWindow.Instance.ErrorsWrittenToLog++;
                }
                switch ( Verbosity )
                {
                    //todo
                    case Verbosity.ShowAsError:
                        MessageBox.Show( Message, "test", MessageBoxButtons.OK, MessageBoxIcon.Error );
                        break;
                    case Verbosity.ShowAsInfo:
                        MessageBox.Show( Message, "test", MessageBoxButtons.OK, MessageBoxIcon.Information );
                        break;
                    case Verbosity.ShowAsWarning:
                        MessageBox.Show( Message, "test", MessageBoxButtons.OK, MessageBoxIcon.Warning );
                        break;
                }
            }
            catch
            {
                // don't crash; this can be thrown if the user has the file open in another program that doesn't share well
            }
        }
    }

    public class DebugLogToDisk
    {
        private static string DebugLogFolder = Program.CurrentLogDirectory;

        public static void Initialize()
        {
            DebugLogFolder = Program.CurrentLogDirectory;

            DebugLog_GeneralMessages = new DebugLogToDisk( "DebugLog_GeneralMessages.txt",
                new string[]
                {
                "message"
                } );

            DebugLog_GeneralErrors = new DebugLogToDisk( "DebugLog_GeneralErrors.txt",
                new string[]
                {
                "message"
                } );
        }

        public static DebugLogToDisk? DebugLog_GeneralMessages = null;
        public static DebugLogToDisk? DebugLog_GeneralErrors = null;

        private string DebugLogFile = string.Empty;

        public DebugLogToDisk( string DebugLogFile, string[] columnHeaders )
        {
            this.DebugLogFile = DebugLogFile;

            LogRecord( columnHeaders );
        }

        public void LogRecord( string[] values )
        {
            if ( true ) //~*~GameSettings.Current.DeveloperDebugLogging )
            {
                try
                {
                    Program.AppendTextToFile( DebugLogFolder + DebugLogFile, DateTime.Now + "\t" + string.Join( "\t", values ) + "\n", Program.MAX_ERROR_FILE_SIZE );
                }
                catch
                {
                    // don't crash; this can be thrown if the user has the file open in another program that doesn't share well
                }
            }
        }

        public void LogRecord( List<string> values )
        {
            if ( true ) //~*~GameSettings.Current.DeveloperDebugLogging )
            {
                try
                {
                    Program.AppendTextToFile( DebugLogFolder + DebugLogFile, DateTime.Now + "\t" + string.Join( "\t", values ) + "\n", Program.MAX_ERROR_FILE_SIZE );
                }
                catch
                {
                    // don't crash; this can be thrown if the user has the file open in another program that doesn't share well
                }
            }
        }

        public void LogRecord( string PreDelimitedString )
        {
            LogRecord( new string[] { PreDelimitedString } );
        }

        private List<string> tempRecord = new List<string>( 400 );

        public void StartRecord( string[] values )
        {
            tempRecord.Clear();
            tempRecord.AddRange( values );
        }

        public void AddToRecord( string[] values )
        {
            tempRecord.AddRange( values );
        }

        public void AddToRecord( string value )
        {
            tempRecord.Add( value );
        }

        public void CommitRecord()
        {
            LogRecord( tempRecord );
        }
    }

    public enum DebugLogDestination
    {
        XEDebugLog,
        ChunkGenTimes,
        MarketItemSubTypes
    }

    public enum Verbosity
    {
        ShowAsError, //todo
        ShowAsInfo, //todo
        ShowAsWarning,
        DoNotShow,
        DoNotShowButLogToConsole,
        DoNotShowAndDoNotLogToConsole
    }

    public struct DelayedMessagePayLoad
    {
        public DateTime Timestamp;
        public string Message;
        public Verbosity Verbosity;
        public int DelayedIndex;
        public int DelayedThreadID;
    }
}