﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Timers;
using System.Xml.Linq;
using System.Xml.XPath;
using NLog;

namespace NzbDrone
{
    internal class IISController
    {
        private static readonly Logger IISLogger = LogManager.GetLogger("IISExpress");
        private static readonly Logger Logger = LogManager.GetLogger("IISController");
        private static readonly string IISFolder = Path.Combine(Config.ProjectRoot, @"IISExpress\");
        private static readonly string IISExe = Path.Combine(IISFolder, @"iisexpress.exe");
        private static readonly string IISConfigPath = Path.Combine(IISFolder, "AppServer", "applicationhost.config");

        private static Timer _pingTimer;
        private static int _pingFailCounter;

        public static Process IISProcess { get; private set; }


        internal static string AppUrl
        {
            get { return string.Format("http://localhost:{0}/", Config.Port); }
        }

        internal static Process StartServer()
        {
            Logger.Info("Preparing IISExpress Server...");
            IISProcess = new Process();

            IISProcess.StartInfo.FileName = IISExe;
            IISProcess.StartInfo.Arguments = String.Format("/config:{0} /trace:i", IISConfigPath);//"/config:"""" /trace:i";
            IISProcess.StartInfo.WorkingDirectory = Config.ProjectRoot;

            IISProcess.StartInfo.UseShellExecute = false;
            IISProcess.StartInfo.RedirectStandardOutput = true;
            IISProcess.StartInfo.RedirectStandardError = true;
            IISProcess.StartInfo.CreateNoWindow = true;


            IISProcess.OutputDataReceived += (OnDataReceived);

            //Set Variables for the config file.
            Environment.SetEnvironmentVariable("NZBDRONE_PATH", Config.ProjectRoot);

            try
            {
                UpdateIISConfig();
            }
            catch (Exception e)
            {
                Logger.ErrorException("An error has occurred while trying to update the config file.", e);
            }


            Logger.Info("Starting process. [{0}]", IISProcess.StartInfo.FileName);
            IISProcess.Start();

            IISProcess.BeginErrorReadLine();
            IISProcess.BeginOutputReadLine();

            //Start Ping
            _pingTimer = new Timer(10000) { AutoReset = true };
            _pingTimer.Elapsed += (Server);
            _pingTimer.Start();

            return IISProcess;
        }

        internal static void StopServer()
        {
            KillProcess(IISProcess);

            Logger.Info("Finding orphaned IIS Processes.");
            foreach (var process in Process.GetProcessesByName("IISExpress"))
            {
                string processPath = process.MainModule.FileName;
                Logger.Info("[{0}]IIS Process found. Path:{1}", process.Id, processPath);
                if (CleanPath(processPath) == CleanPath(IISExe))
                {
                    Logger.Info("[{0}]Process is considered orphaned.", process.Id);
                    KillProcess(process);
                }
                else
                {
                    Logger.Info("[{0}]Process has a different start-up path. skipping.", process.Id);
                }
            }
        }

        private static void RestartServer()
        {
            _pingTimer.Stop();
            Logger.Warn("Attempting to restart server.");
            StopServer();
            StartServer();
        }

        private static void Server(object sender, ElapsedEventArgs e)
        {
            try
            {
                new WebClient().DownloadString(AppUrl);
                if (_pingFailCounter > 0)
                {
                    Logger.Info("Application pool has been successfully recovered.");
                }
                _pingFailCounter = 0;
            }
            catch (Exception ex)
            {
                _pingFailCounter++;
                Logger.ErrorException("Application pool is not responding. Count " + _pingFailCounter, ex);
                if (_pingFailCounter > 2)
                {
                    RestartServer();
                }
            }
        }

        private static void OnDataReceived(object s, DataReceivedEventArgs e)
        {
            if (e == null || String.IsNullOrWhiteSpace(e.Data) || e.Data.StartsWith("Request started:") ||
                e.Data.StartsWith("Request ended:") || e.Data == ("IncrementMessages called"))
                return;

            IISLogger.Trace(e.Data);
        }

        private static void UpdateIISConfig()
        {
            string configPath = Path.Combine(IISFolder, @"AppServer\applicationhost.config");

            Logger.Info(@"Server configuration file: {0}", configPath);
            Logger.Info(@"Configuring server to: [http://localhost:{0}]", Config.Port);

            var configXml = XDocument.Load(configPath);

            var bindings =
                configXml.XPathSelectElement("configuration/system.applicationHost/sites").Elements("site").Where(
                    d => d.Attribute("name").Value.ToLowerInvariant() == "nzbdrone").First().Element("bindings");
            bindings.Descendants().Remove();
            bindings.Add(
                new XElement("binding",
                             new XAttribute("protocol", "http"),
                             new XAttribute("bindingInformation", String.Format("*:{0}:", Config.Port))
                    ));

            configXml.Save(configPath);
        }

        private static void KillProcess(Process process)
        {
            if (process != null && !process.HasExited)
            {
                Logger.Info("[{0}]Killing process", process.Id);
                process.Kill();
                Logger.Info("[{0}]Waiting for exit", process.Id);
                process.WaitForExit();
                Logger.Info("[{0}]Process terminated successfully", process.Id);
            }
        }

        private static string CleanPath(string path)
        {
            return path.ToLower().Replace("\\", "").Replace("//", "//");
        }


    }
}