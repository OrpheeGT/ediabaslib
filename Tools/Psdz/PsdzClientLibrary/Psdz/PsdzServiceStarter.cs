﻿using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Threading;
using BMW.Rheingold.Psdz;
using log4net;
using log4net.Repository.Hierarchy;
using PsdzClient;

namespace BMW.Rheingold.Psdz.Client
{
    public class PsdzServiceStarter
    {
        public enum PsdzServiceStartResult
        {
            PsdzStillRunning,
            PsdzStartFailed,
            PsdzStartFailedMemError,
            PsdzStartOk
        }

        private const string PsdzServiceStarterMutex = "Global\\PsdzServiceStarterMutex";

        private const string HostReadyEventName = "Global\\PsdzServiceHostReadyEvent";

        private const string HostFailedEventName = "Global\\PsdzServiceHostFailedEvent";

        private const string HostFailedEventMemErrorName = "Global\\PsdzServiceHostFailedMemError";

        private const string istaPIDfileName = "PsdzInstances.txt";

        private static string istaPIDfilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ISTA", "PsdzInstances.txt");

        private static readonly ILog Logger = LogManager.GetLogger(typeof(PsdzServiceStarter));

        private const string PsdzServiceHostProcessName = "PsdzServiceHost";

        private readonly string psdzHostDir;

        private readonly PsdzServiceArgs psdzServiceArgs;

        private readonly string psdzServiceHostLogDir;


        public PsdzServiceStarter(string psdzHostDir, string psdzServiceHostLogDir, PsdzServiceArgs psdzServiceArgs)
        {
            if (psdzHostDir == null)
            {
                throw new ArgumentNullException("psdzHostDir");
            }
            if (psdzServiceHostLogDir == null)
            {
                throw new ArgumentNullException("psdzServiceHostLogDir");
            }
            if (psdzServiceArgs == null)
            {
                throw new ArgumentNullException("psdzServiceArgs");
            }
            this.psdzHostDir = psdzHostDir;
            this.psdzServiceHostLogDir = psdzServiceHostLogDir;
            this.psdzServiceArgs = psdzServiceArgs;
        }

        // [UH] added
        public static bool IsThisServerInstanceRunning()
        {
            if (ClientContext.EnablePsdzMultiSession())
            {
                return IsServerInstanceRunning(Process.GetCurrentProcess().Id);
            }
            return IsServerInstanceRunning();
        }

        public static bool IsServerInstanceRunning(int istaProcessId = 0)
        {
            using (Mutex mutex = new Mutex(false, PsdzServiceStarterMutex))
            {
                try
                {
                    mutex.WaitOne();
                    return IsPsdzServiceHostRunning(istaProcessId);
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        private static bool IsPsdzServiceHostRunning(int istaProcessId)
        {
            if (File.Exists(istaPIDfilePath))
            {
                checkForPsdzInstancesLogFile();
                Logger.Info($"Checking for already running PsdzServiceHost instances for ISTA Process ID {istaProcessId} ...");
                using (FileStream stream = new FileStream(istaPIDfilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader streamReader = new StreamReader(stream))
                    {
                        string text;
                        while ((text = streamReader.ReadLine()) != null)
                        {
                            Logger.Info("Found instance of PsdzServiceHost with ISTA Process ID " + text + ".");
                            if (int.Parse(text) == istaProcessId)
                            {
                                Logger.Info($"Another instance of PsdzServiceHost is already running for the ISTA Process ID {istaProcessId}.");
                                Logger.Info("Start of a second instance is cancelled.");
                                return true;
                            }
                        }
                    }
                }
                Logger.Info($"No other instance of PsdzServiceHost is running for ISTA Process ID {istaProcessId}.");
                return false;
            }

            Process[] processesByName = Process.GetProcessesByName(PsdzServiceHostProcessName);
            if (istaProcessId == 0)
            {
                return processesByName.Any();
            }
            foreach (ManagementObject item in new ManagementObjectSearcher(string.Format("select CommandLine from Win32_Process where Name='{0}.exe'", PsdzServiceHostProcessName)).Get())
            {
                string[] array = item["CommandLine"].ToString().Split(' ');
                if (array.Length == 3)
                {
                    int num2 = int.Parse(array[2]);
                    if (istaProcessId == num2)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public PsdzServiceStartResult StartIfNotRunning(int istaProcessId = 0)
        {
            PsdzServiceStartResult result;
            using (Mutex mutex = new Mutex(false, PsdzServiceStarterMutex))
            {
                try
                {
                    mutex.WaitOne();
                    if (PsdzServiceStarter.IsPsdzServiceHostRunning(istaProcessId))
                    {
                        result = PsdzServiceStartResult.PsdzStillRunning;
                    }
                    else
                    {
                        result = this.StartServerInstance(istaProcessId);
                    }
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
            return result;
        }

        private PsdzServiceStartResult StartServerInstance(int istaProcessId)
        {
            Logger.Info("Starting new PsdzServiceHost instance...");
            string tempFileName = Path.GetTempFileName();
            PsdzServiceArgs.Serialize(tempFileName, psdzServiceArgs);
            ProcessStartInfo processStartInfo = new ProcessStartInfo();
            processStartInfo.FileName = Path.Combine(psdzHostDir, string.Format(CultureInfo.InvariantCulture, "{0}.exe", PsdzServiceHostProcessName));
            processStartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            processStartInfo.UseShellExecute = false;
            processStartInfo.RedirectStandardInput = false;
            processStartInfo.RedirectStandardOutput = false;
            processStartInfo.RedirectStandardError = false;
            processStartInfo.CreateNoWindow = true;
            processStartInfo.Environment["PSDZSERVICEHOST_LOGDIR"] = psdzServiceHostLogDir;

            bool pidFileSupport = false;
            FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(processStartInfo.FileName);
            if (fileVersionInfo.FileVersion != null)
            {
                if (fileVersionInfo.FileMajorPart >= 24)
                {
                    pidFileSupport = true;
                }
            }

            if (pidFileSupport)
            {
                if (istaProcessId == 0)
                {
                    processStartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\" \"{1}\"", tempFileName, istaPIDfilePath);
                }
                else
                {
                    processStartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1} \"{2}\"", tempFileName, istaPIDfilePath, istaProcessId);
                }
            }
            else
            {
                if (istaProcessId == 0)
                {
                    processStartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", tempFileName);
                }
                else
                {
                    processStartInfo.Arguments = string.Format(CultureInfo.InvariantCulture, "\"{0}\" {1}", tempFileName, istaProcessId);
                }
            }
            EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset, HostReadyEventName);
            EventWaitHandle eventWaitHandle2 = new EventWaitHandle(false, EventResetMode.AutoReset, HostFailedEventName);
            EventWaitHandle eventWaitHandle3 = new EventWaitHandle(false, EventResetMode.AutoReset, HostFailedEventMemErrorName);
            Process.Start(processStartInfo);
            int num = WaitHandle.WaitAny(new WaitHandle[3] { eventWaitHandle, eventWaitHandle2, eventWaitHandle3 }, new TimeSpan(0, 0, 5, 0));
            File.Delete(tempFileName);
            switch (num)
            {
                case 0:
                {
                    if (pidFileSupport)
                    {
                        checkForPsdzInstancesLogFile();
                        using (StreamWriter streamWriter = new StreamWriter(istaPIDfilePath, append: true))
                        {
                            streamWriter.WriteLine(istaProcessId);
                        }
                    }
                    Logger.Info("Start of new PsdzServiceHost instance successful!");
                    return PsdzServiceStartResult.PsdzStartOk;
                }
                case 1:
                    Logger.Info("Start of new PsdzServiceHost instance failed!");
                    return PsdzServiceStartResult.PsdzStartFailed;
                case 2:
                    Logger.Info($"Start of new PsdzServiceHost instance failed! Result was {num} (Memory Error).");
                    return PsdzServiceStartResult.PsdzStartFailedMemError;
                case 258:
                    Logger.Info($"Start of new PsdzServiceHost instance failed! Result was {num} (Timeout).");
                    return PsdzServiceStartResult.PsdzStartFailed;
                default:
                    Logger.Error($"Start of new PsdzServiceHost instance failed! Unexpected result: {num}.");
                    return PsdzServiceStartResult.PsdzStartFailed;
            }
        }

        private static void checkForPsdzInstancesLogFile()
        {
            if (!Directory.Exists(Path.GetDirectoryName(istaPIDfilePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(istaPIDfilePath));
            }
            if (!File.Exists(istaPIDfilePath))
            {
                File.Create(istaPIDfilePath);
            }
        }
	}
}
