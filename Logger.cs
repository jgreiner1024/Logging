using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace MediaSuite.Core
{
    public enum LogLevel
    {
        None = 0,
        Low = 1,
        Medium = 2,
        Full = 3,
        Debug = 10
    }

	public static class Logger
	{
        private static NamedPipesClient m_pipeClient = null;
        private static LogLevel g_Level = LogLevel.Medium;
        private static Dictionary<string, int> m_WriteCounts = new Dictionary<string, int>();
		static Logger()
		{
			m_pipeClient = new NamedPipesClient();
			m_pipeClient.PipeServerName = "MediaSuiteLogServer";
			m_pipeClient.Connect();
            Logger.Level = LogLevel.Medium;
		}

		public static string ServerName
		{
			set 
			{	
				m_pipeClient.PipeServerName = value;
				m_pipeClient.Connect();
			}
		}

        public static LogLevel Level
        {
            get { return g_Level; }
            set 
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    g_Level = LogLevel.Debug;
                }
                else
                {
                    g_Level = value;
                }
            }
        }

		//log a simple text string
        public static void Write(string text)
        {
            Write(LogLevel.Medium, text);
        }

        public static void Write(LogLevel level, string text)
		{
            if (level <= g_Level)
            {
                m_pipeClient.SendMessage(text);
            }
		}
        
        public static void WriteLine(string text)
        {
            WriteLine(LogLevel.Medium, text);
        }
		
        public static void WriteLine(LogLevel level, string text)
		{
			text += "\r\n";
            Write(level, text);
		}

		//format and log a parameter list
        public static void Write(string text, params object[] list)
        {
            Write(LogLevel.Medium, text, list);
        }

		public static void Write(LogLevel level, string text, params object[] list)
		{
			string formattedText = String.Format(text, list);
            Write(level, formattedText);
		}

		//log
        public static void WriteLine(string text, params object[] list)
        {
            WriteLine(LogLevel.Medium, text, list);
        }

		public static void WriteLine(LogLevel level, string text, params object[] list)
		{
			string formattedText = String.Format(text, list) + "\r\n";
            Write(level, formattedText);
		}



		//an exception
        public static void Write(Exception e)
        {
            Write(LogLevel.Medium, e);
        }

		public static void Write(LogLevel level, Exception e)
		{
			WriteHeader(level, "Exception Information");
            WriteLine(level, e.ToString());
			WriteFooter(level, "Exception Information");
		}

        public static void WriteLine(Exception e)
        {
            WriteLine(LogLevel.Medium, e);
        }

		public static void WriteLine(LogLevel level, Exception e)
		{
			WriteHeader(level, "Exception Information");
            WriteLine(level, e.ToString());
			WriteFooter(level, "Exception Information");
		}

        //write a specific number of times
        public static void WriteLineXTimes(string id, int count, string text, params object[] list)
        {
            WriteXTimes(LogLevel.Medium, id, count, text + "\r\n", list);
        }

        public static void WriteXTimes(string id, int count, string text, params object[] list)
        {
            WriteXTimes(LogLevel.Medium, id, count, text, list);
        }

        public static void WriteLineXTimes(LogLevel level, string id, int count, string text, params object[] list)
        {
            WriteXTimes(level, id, count, text + "\r\n", list);
        }

        public static void WriteXTimes(LogLevel level, string id, int count, string text, params object[] list)
        {
            if (m_WriteCounts.ContainsKey(id))
            {
                int writeCount = m_WriteCounts[id];

                //check to see if we want to wrie the data
                if (writeCount >= count)
                    return;

                m_WriteCounts[id] = writeCount + 1;
            }
            else
            {
                m_WriteCounts.Add(id, 1);
            }

            Write(level, text, list);
        }

        public static void WriteOnce(string id, string text, params object[] list)
        {
            WriteOnce(LogLevel.Medium, id, text, list);
        }

        public static void WriteLineOnce(string id, string text, params object[] list)
        {
            WriteOnce(LogLevel.Medium, id, text + "\r\n", list);
        }

        public static void WriteOnce(LogLevel level, string id, string text, params object[] list)
        {
            WriteXTimes(level, id, 1, text, list);
        }

        public static void WriteLineOnce(LogLevel level, string id, string text, params object[] list)
        {
            WriteOnce(level, id, text + "\r\n", list);
        }


        public static void WriteFolder(string path)
        {
            WriteFolder(LogLevel.Medium, path);
        }

		public static void WriteFolder(LogLevel level, string path)
		{
			//make sure path is valid
			if (Directory.Exists(path) == false)
				return;

			long size = 0;
			WriteLine(level, "Contents of: {0}", path);
			WriteHeader(level, "Folder Contents");
            size = WriteSubFolder(level, path);
			WriteFooter(level, "Folder Contents");
			WriteLine(level, "Total Size: {0}", FileUtil.UpConvertBytes((double)size, false));

		}

		private static long WriteSubFolder(LogLevel level, string path)
		{
			long size = 0;
			try
			{
				WriteLine(path);

				DirectoryInfo di = new DirectoryInfo(path);
				//list the folders
				foreach (DirectoryInfo g in di.GetDirectories())
				{
                    size += WriteSubFolder(level, g.FullName);
				}

				//list the files
				FileInfo[] fi = di.GetFiles();
				foreach (FileInfo f in fi)
				{
					WriteLine(level, "{0} - {1}", f.FullName, f.Length);
					size += f.Length;
				}

			}
			catch //(System.Exception e)
			{
				//WriteLine("Could not log path {0}\r\n{1}", path, e.ToString());
			}

			return size;
		}

		//close the client
		public static void Close()
		{
			if (m_pipeClient != null)
				m_pipeClient.Close();
		}

        //log a header
        public static void WriteHeader(string text, params object[] list)
        {
            WriteHeader(LogLevel.Medium, text, list);
        }

        public static void WriteHeader(LogLevel level, string text, params object[] list)
        {
            WriteLine(level, "---[ Begin " + String.Format(text, list) + " ]---");
        }

        //log a footer
        public static void WriteFooter(string text, params object[] list)
        {
            WriteFooter(LogLevel.Medium, text, list);
        }

        public static void WriteFooter(LogLevel level, string text, params object[] list)
        {
            WriteLine(level, "---[ End " + String.Format(text, list) + " ]---\n");
        }


        public static void AppendLog(string path)
        {
            AppendLog(LogLevel.Medium, path);
        }

        public static void AppendLog(LogLevel level, string path)
        {
            if(!File.Exists(path))
                return;
			WriteLine(level, "Appending log file: {0}", path);
			WriteHeader(level, "Appending Log");

			try
			{
				StreamReader reader = File.OpenText(path);
				string appendstring = reader.ReadToEnd();
                WriteLine(level, appendstring);
				
			}
			catch (Exception exp)
			{
				Write(exp);
			}
			WriteFooter(level, "Appending Log");


        }




	}
}

