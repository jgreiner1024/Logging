using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace MediaSuite.Core
{
	public class LogServer
	{
		//log path and filename
		private string m_logFileName;
		private StreamWriter m_logFile = null;
		private NamedPipesServer m_pipeServer;
        private string m_PreviousMessage = null;
		//destructor
		~LogServer()
		{
			Close();
		}
		//constructor
		public LogServer(string path, string filename, string servername)
		{
			if (OpenLogFile(path, filename) == false)
				return;
			StartServer(servername);
		}


		private bool OpenLogFile(string path, string filename)
		{
			if (path == null)
			{
				path = ".\\log\\";
			}
			if (path == "")
			{
				path = ".\\log\\";
			}
			if (!path.EndsWith(@"\"))
				path += @"\";

			//create the log folder if it does not exist
			if (Directory.Exists(path) == false)
			{
				try
				{
					Directory.CreateDirectory(path);
				}
				catch
				{
					//failed to create the log folder
					return false;
				}
			}

			//check for old log files
			string oFile;
			string nFile;


			try
			{
				oFile = path + filename + "009.log";
				File.Delete(oFile);
				for (int i = 8; i > 0; i--)
				{
					int j = i + 1;
					oFile = path + filename + "00" + i.ToString() + ".log";
					nFile = path + filename + "00" + j.ToString() + ".log";
					if (File.Exists(oFile))
					{
						File.Move(oFile, nFile);
					}
				}
				//shift log
				oFile = path + filename + ".log";
				nFile = path + filename + "001.log";
				if (File.Exists(oFile))
				{
					File.Move(oFile, nFile);
				}
			}
			catch
			{
				//something went wrong so we'll just try to create the new log file next
			}

			try
			{
				m_logFileName = Path.Combine(path, filename) + ".log";
				//m_logFile = //File.CreateText(m_logFileName);
                m_logFile = new StreamWriter(new FileStream(m_logFileName, FileMode.CreateNew, FileAccess.Write, FileShare.ReadWrite));
                
			}
			catch
			{
				return false;
			}
			//success
			return true;
		}

		public void Close()
		{
            if (m_pipeServer != null)
            {
                m_pipeServer.Close();
            }
		}

		private bool StartServer(string servername)
		{
			try
			{
				m_pipeServer = new NamedPipesServer();

				m_pipeServer.StringMessageReceived +=
				    new NamedPipesServer.StringMessageReceivedHandler(pipeServer_StringMessageReceived);
				//start the pipe server if it's not already running
				if (!m_pipeServer.Running)
				{
					if (servername.Contains(@"\\.\pipe\"))
						m_pipeServer.PipeName = servername;
					else
						m_pipeServer.PipeName = @"\\.\pipe\" + servername;
					m_pipeServer.Start();
				}
			}
			catch(Exception e)
			{
				//failed to create
				m_logFile.Write("Failed to create the Log Server: " + m_pipeServer.PipeName + "\r\n");
				m_logFile.Write(e.ToString() + "\r\n");
				m_logFile.Flush();
				return false;
			}

			//success
			return true;
		}

		//log the message
		void pipeServer_StringMessageReceived(NamedPipesServer.Client client, string message)
		{

			try
			{
                if (message != m_PreviousMessage)
                {
                    /*
                     * getting rid of the timestamp it makes things bad in the log file
                    string time = string.Format("[{0:hh}:{0:mm}:{0:ss}] ", DateTime.Now);
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        System.Diagnostics.Debug.Write(time);
                    }
                    m_logFile.Write(time);
                    */
                    System.Diagnostics.Debug.Write(message);
                    m_logFile.Write(message);
                    m_logFile.Flush();

                    m_PreviousMessage = message;
                }
			}
			catch
			{
			}
		}


	}
}
