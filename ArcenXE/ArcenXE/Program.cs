using ArcenXE.Utilities;

namespace ArcenXE
{
    internal static class Program
    {
        public static int MainThreadID;

        public const int MAX_ERROR_FILE_SIZE = 1024 * 1024 * 4; //4 MB
        public const int MAX_BIG_ERROR_FILE_SIZE = 1024 * 1024 * 40; //40 MB

        public static string CurrentLogDirectory = string.Empty;

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            MainThreadID = Thread.CurrentThread.ManagedThreadId;

            CurrentLogDirectory = Environment.CurrentDirectory;
            ProgramPermanentSettings.ApplicationPath.Path = CurrentLogDirectory.Replace( @"bin\Debug\net6.0-windows", "" );
            CurrentLogDirectory = CurrentLogDirectory.Replace( @"bin\Debug\net6.0-windows", @"logs\" );

            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            string pathToDebugFolder = Environment.CurrentDirectory.Replace( @"bin\Debug\net6.0-windows", @"logs\" );
            if ( !Directory.Exists( pathToDebugFolder ) )
                Directory.CreateDirectory( pathToDebugFolder );

            string pathToDebugLog = pathToDebugFolder + "XEDebugLog.txt";
            string textToAppend = "\n\n" + DateTime.Now.ToString() + "\t\tPROGRAM START\n";
            AppendTextToFile( pathToDebugLog, textToAppend, MAX_ERROR_FILE_SIZE );

            ApplicationConfiguration.Initialize();
            Application.Run( new MainWindow() );
        }

        public static bool CalculateIsCurrentThreadMainThead()
        {
            return Thread.CurrentThread.ManagedThreadId == MainThreadID;
        }

        public static void AppendTextToFile( string Filename, string Text, int MaxFileSize )
        {
            if ( MaxFileSize > 0 )
            {
                try
                {
                    FileInfo info = new FileInfo( Filename );
                    if ( info.Exists && info.Length > MaxFileSize )
                        info.Delete();
                }
                catch { }
            }

            using ( FileStream stream = new FileStream( Filename, FileMode.Append, FileAccess.Write, FileShare.ReadWrite ) )
            {
                using ( StreamWriter writer = new StreamWriter( stream ) )
                {
                    writer.Write( Text );
                    writer.Flush();
                    writer.Close();
                }
                stream.Close();
            }
        }
    }
}