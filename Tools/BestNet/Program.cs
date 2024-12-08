﻿using CommandLine;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

#pragma warning disable CA1416

namespace BestNet
{
    internal class Program
    {
        public const string Best32DllName = "Best32.dll";
        public const string Best64DllName = "Best64.dll";
        public const string StdLibName = "B2Runtim.lib";
        public const int MaxPasswords = 10;
        public const int MaxPasswordLen = 10;
        public const int MinBestVersion = 0x00070600;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Best1ProgressDelegate(int value);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Best1ErrorTextDelegate([MarshalAs(UnmanagedType.LPStr)] string text);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Best1ErrorValueDelegate(uint value, [MarshalAs(UnmanagedType.LPStr)] string text);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Best2ProgressDelegate(int value1, int value2, int value3);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Best2ErrorTextDelegate([MarshalAs(UnmanagedType.LPStr)] string text);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int Best2ErrorValueDelegate(uint value, [MarshalAs(UnmanagedType.LPStr)] string text);

        [DllImport(Best32DllName, EntryPoint = "__best32Startup")]
        public static extern int __best32Startup32(int version, IntPtr infoText, int printInfo, IntPtr verBuffer, int verSize);

        [DllImport(Best64DllName, EntryPoint = "__best32Startup")]
        public static extern int __best32Startup64(int version, IntPtr infoText, int printInfo, IntPtr verBuffer, int verSize);

        [DllImport(Best32DllName, EntryPoint = "__best32Shutdown")]
        public static extern int __best32Shutdown32();

        [DllImport(Best64DllName, EntryPoint = "__best32Shutdown")]
        public static extern int __best32Shutdown64();

        [DllImport(Best32DllName, EntryPoint = "__best1AsmVersion")]
        public static extern IntPtr __best1AsmVersion32();

        [DllImport(Best64DllName, EntryPoint = "__best1AsmVersion")]
        public static extern IntPtr __best1AsmVersion64();

        [DllImport(Best32DllName, EntryPoint = "__best1Init")]
        public static extern int __best1Init32(IntPtr inputFile, IntPtr outputFile, int revision,
            IntPtr userName, int generateMapfile, int fileType, IntPtr dateString, IntPtr passwordLabel, IntPtr passwordBuffer);

        [DllImport(Best64DllName, EntryPoint = "__best1Init")]
        public static extern int __best1Init64(IntPtr inputFile, IntPtr outputFile, int revision,
            IntPtr userName, int generateMapfile, int fileType, IntPtr dateString, IntPtr passwordLabel, IntPtr passwordBuffer);

        [DllImport(Best32DllName, EntryPoint = "__best1Config")]
        public static extern Best1ErrorValueDelegate __best1Config32(Best1ProgressDelegate progressCallback, Best1ErrorTextDelegate errorTextCallback, Best1ErrorValueDelegate errorValueCallback);

        [DllImport(Best64DllName, EntryPoint = "__best1Config")]
        public static extern Best1ErrorValueDelegate __best1Config64(Best1ProgressDelegate progressCallback, Best1ErrorTextDelegate errorTextCallback, Best1ErrorValueDelegate errorValueCallback);

        [DllImport(Best32DllName, EntryPoint = "__best1Options")]
        public static extern int __best1Options32(int mapOptions);

        [DllImport(Best64DllName, EntryPoint = "__best1Options")]
        public static extern int __best1Options64(int mapOptions);

        [DllImport(Best32DllName, EntryPoint = "__best1Asm")]
        public static extern int __best1Asm32(IntPtr mapFile, IntPtr infoFile);

        [DllImport(Best64DllName, EntryPoint = "__best1Asm")]
        public static extern int __best1Asm64(IntPtr mapFile, IntPtr infoFile);

        [DllImport(Best32DllName, EntryPoint = "__best2Init")]
        public static extern int __best2Init32();

        [DllImport(Best64DllName, EntryPoint = "__best2Init")]
        public static extern int __best2Init64();

        [DllImport(Best32DllName, EntryPoint = "__best2Config")]
        public static extern int __best2Config32(Best2ProgressDelegate progressCallback, Best2ErrorTextDelegate errorTextCallback, Best2ErrorValueDelegate errorValueCallback);

        [DllImport(Best64DllName, EntryPoint = "__best2Config")]
        public static extern int __best2Config64(Best2ProgressDelegate progressCallback, Best2ErrorTextDelegate errorTextCallback, Best2ErrorValueDelegate errorValueCallback);

        [DllImport(Best32DllName, EntryPoint = "__best2Options")]
        public static extern void __best2Options32(int option1, int option2, int option3);

        [DllImport(Best64DllName, EntryPoint = "__best2Options")]
        public static extern void __best2Options64(int option1, int option2, int option3);

        [DllImport(Best32DllName, EntryPoint = "__best2Cc")]
        public static extern int __best2Cc32(IntPtr inputFile, IntPtr outAsmFile, IntPtr[] libFiles, IntPtr infoFile, IntPtr incDirs);

        [DllImport(Best64DllName, EntryPoint = "__best2Cc")]
        public static extern int __best2Cc64(IntPtr inputFile, IntPtr outAsmFile, IntPtr[] libFiles, IntPtr infoFile, IntPtr incDirs);

        [DllImport(Best32DllName, EntryPoint = "__best2CcTotal")]
        public static extern int __best2CcTotal32();

        [DllImport(Best64DllName, EntryPoint = "__best2CcTotal")]
        public static extern int __best2CcTotal64();

        [DllImport(Best32DllName, EntryPoint = "__best2AsmTotal")]
        public static extern int __best2AsmTotal32();

        [DllImport(Best64DllName, EntryPoint = "__best2AsmTotal")]
        public static extern int __best2AsmTotal64();

        [DllImport(Best32DllName, EntryPoint = "__best2Rev")]
        public static extern int __best2Rev32(IntPtr value, IntPtr buffer);

        [DllImport(Best64DllName, EntryPoint = "__best2Rev")]
        public static extern int __best2Rev64(IntPtr value, IntPtr buffer);

        public class Options
        {
            [Option('i', "inputfile", Required = true, HelpText = "Input file to compile.")]
            public string InputFile { get; set; }

            [Option('o', "outputfile", Required = false, HelpText = "Optional output file name.")]
            public string OutputFile { get; set; }

            [Option('r', "revision", Required = false, HelpText = "Specify revision <X.Y>.")]
            public string RevisionString { get; set; }

            [Option('u', "userName", Required = false, HelpText = "Specify user name.")]
            public string UserName { get; set; }

            [Option('p', "password", Required = false, HelpText = "Specify password label or @<password label file>")]
            public string PasswordLabel { get; set; }

            [Option('m', "mapfile", Required = false, HelpText = "Set to create map file.")]
            public bool CreateMapFile { get; set; }

            [Option('k', "keepFiles", Required = false, HelpText = "Set to keep intermediate files.")]
            public bool KeepFiles { get; set; }

            [Option('l', "libfile", Required = false, HelpText = "Specify lib file name.")]
            public IEnumerable<string> LibFiles { get; set; }

            [Option('d', "incDirs", Required = false, HelpText = "Include directories, separated by ;")]
            public string IncDirs { get; set; }

            [Option('e', "ediabasDir", Required = false, HelpText = "Specify EDIABAS bin directory for bestXX.dll selection.")]
            public string EdiabasDir { get; set; }
        }

        static int _lastBest1OutLine = -1;
        static int _lastBest2OutLine = -1;
        static readonly List<IntPtr> _freeIntPtrList = new List<IntPtr>();

        static int Main(string[] args)
        {
            bool is64Bit = Environment.Is64BitProcess;
            IntPtr libHandle = IntPtr.Zero;
            try
            {
                string inputFile = null;
                string outputFile = null;
                string revisionString = null;
                string userName = null;
                string passwordLabel = null;
                int generateMapFile = 0;
                bool keepFiles = false;
                List<string> libFiles = null;
                string incDirs = null;
                string ediabasDir = null;
                bool hasErrors = false;
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed<Options>(o =>
                    {
                        inputFile = o.InputFile;
                        outputFile = o.OutputFile;
                        revisionString = o.RevisionString;
                        userName = o.UserName;
                        passwordLabel = o.PasswordLabel;
                        generateMapFile = o.CreateMapFile ? 1: 0;
                        keepFiles = o.KeepFiles;
                        libFiles = o.LibFiles?.ToList();
                        incDirs = o.IncDirs;
                        ediabasDir = o.EdiabasDir;
                    })
                    .WithNotParsed(e =>
                    {
                        hasErrors = true;
                    });

                if (hasErrors)
                {
                    return 1;
                }

                if (string.IsNullOrEmpty(inputFile))
                {
                    WriteNewConsoleLine("Input file not specified");
                    return 1;
                }

                inputFile = Path.GetFullPath(inputFile);
                if (!File.Exists(inputFile))
                {
                    WriteNewConsoleLine("Input file not found");
                    return 1;
                }

                string bestDllName = is64Bit ? Best64DllName : Best32DllName;
                string ediabasBinPath = ediabasDir;
                if (string.IsNullOrEmpty(ediabasBinPath))
                {
                    ediabasBinPath = DetectBestDllFolder(bestDllName);
                }

                if (!Directory.Exists(ediabasBinPath))
                {
                    WriteNewConsoleLine("EDIABAS bin path not found");
                    return 1;
                }

                string bestDllPath = Path.Combine(ediabasBinPath, bestDllName);
                if (!File.Exists(bestDllPath))
                {
                    WriteNewConsoleLine("{0} not found", bestDllName);
                    return 1;
                }

                long bestDllVerNum = GetBestDllVersion(bestDllPath);
                if (bestDllVerNum < MinBestVersion)
                {
                    WriteNewConsoleLine("Minimum best version required: {0:X06}", MinBestVersion);
                    return 1;
                }

                if (!NativeLibrary.TryLoad(bestDllPath, out libHandle))
                {
                    WriteNewConsoleLine("{0} not loaded", bestDllName);
                    return 1;
                }

                string fileExt = Path.GetExtension(inputFile);
                bool best2Api;

                if (fileExt.StartsWith(".b1", StringComparison.OrdinalIgnoreCase))
                {
                    best2Api = false;
                }
                else if (fileExt.StartsWith(".b2", StringComparison.OrdinalIgnoreCase))
                {
                    best2Api = true;
                }
                else
                {
                    WriteNewConsoleLine("Invalid input file extension");
                    return 1;
                }

                int fileType = 1;
                string outExt = ".prg";
                string asmExt = ".b1v";
                string mapExt = ".m1v";
                string infoExt = ".biv";
                string preproExt = ".p2v";
                if (string.Compare(fileExt, ".b1g", StringComparison.OrdinalIgnoreCase) == 0 ||
                    string.Compare(fileExt, ".b2g", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    fileType = 0;
                    outExt = ".grp";
                    asmExt = ".b1g";
                    mapExt = ".m1g";
                    infoExt = ".big";
                    preproExt = ".p2g";
                }

                if (!best2Api)
                {
                    asmExt = null;
                }

                if (string.IsNullOrEmpty(outputFile))
                {
                    outputFile = Path.ChangeExtension(inputFile, outExt);
                }
                outputFile = Path.GetFullPath(outputFile);

                string mapFile = Path.ChangeExtension(outputFile, mapExt);
                string asmOutFile = null;
                string infoFile = null;

                WriteNewConsoleLine("Output file: {0}", outputFile);
                if (generateMapFile != 0)
                {
                    WriteNewConsoleLine("Map file: {0}", mapFile);
                }

                int? revision = null;
                if (!string.IsNullOrEmpty(revisionString))
                {
                    string[] revParts = revisionString.Split('.');
                    if (revParts.Length == 2)
                    {
                        if (int.TryParse(revParts[0], out int major) && int.TryParse(revParts[1], out int minor))
                        {
                            revision = ((major & 0xFFFF) << 16) | (minor & 0xFFFF);
                        }
                    }
                }

                int asmRevValue = 0;
                string asmUserName = null;

                //Console.ReadKey();
                bool best32Started = false;
                int bestVerSize = 16;
                IntPtr bestVerPtr = StoreIntPtr(Marshal.AllocHGlobal(bestVerSize));
                IntPtr bestRevPtr = IntPtr.Zero;
                IntPtr bestRevValuePtr = IntPtr.Zero;

                IntPtr inputFilePtr = StoreIntPtr(Marshal.StringToHGlobalAnsi(inputFile));
                IntPtr outputFilePtr = StoreIntPtr(Marshal.StringToHGlobalAnsi(outputFile));
                IntPtr mapFilePtr = StoreIntPtr(Marshal.StringToHGlobalAnsi(mapFile));
                IntPtr asmOutFilePtr = IntPtr.Zero;
                IntPtr infoFilePtr = IntPtr.Zero;
                IntPtr incDirsFilePtr = IntPtr.Zero;
                IntPtr[] libFilesPtr = new IntPtr[] { IntPtr.Zero };
                IntPtr userNamePtr = IntPtr.Zero;

                DateTime inputFileDate = File.GetLastWriteTime(inputFile);
                string dateStr = inputFileDate.ToString("ddd MMM dd HH:mm:ss yyyy", CultureInfo.InvariantCulture);
                IntPtr datePtr = StoreIntPtr(Marshal.StringToHGlobalAnsi(dateStr));
                string password = passwordLabel ?? string.Empty;
                IntPtr passwordLabelPtr = StoreIntPtr(Marshal.StringToHGlobalAnsi(password));

                int passwordBufferSize = MaxPasswords * MaxPasswordLen;
                IntPtr passwordBufferPtr = StoreIntPtr(Marshal.AllocHGlobal(passwordBufferSize));
                // erase password buffer
                for (int i = 0; i < passwordBufferSize; i++)
                {
                    Marshal.WriteByte(passwordBufferPtr, i, 0);
                }

                try
                {
                    int startResult = is64Bit ? __best32Startup64(0x20000, IntPtr.Zero, 0, bestVerPtr, bestVerSize) :
                        __best32Startup32(0x20000, IntPtr.Zero, 0, bestVerPtr, bestVerSize);
                    //WriteNewConsoleLine("Best32 start result: {0}", startResult);
                    if (startResult != 1)
                    {
                        WriteNewConsoleLine("Best32 startup failed");
                        return 1;
                    }

                    best32Started = true;
                    string bestVer = Marshal.PtrToStringAnsi(bestVerPtr);
                    WriteNewConsoleLine("Best version: {0}", bestVer);

                    // BEST2 init
                    int configResult = is64Bit ? __best2Config64(Best2ProgressEvent, Best2ErrorTextEvent, Best2ErrorValueEvent) :
                        __best2Config32(Best2ProgressEvent, Best2ErrorTextEvent, Best2ErrorValueEvent);
                    if (configResult != 0)
                    {
                        WriteNewConsoleLine("Best2 config failed");
                        return 1;
                    }

                    if (is64Bit)
                    {
                        __best2Options64(0, 0, 0);
                    }
                    else
                    {
                        __best2Options32(0, 0, 0);
                    }

                    int initResult = is64Bit ? __best2Init64() : __best2Init32();
                    WriteNewConsoleLine("Best2 init result: {0}", initResult);
                    if (initResult != 0)
                    {
                        WriteNewConsoleLine("Best2 init failed");
                        return 1;
                    }

                    if (best2Api)
                    {
                        if (string.IsNullOrEmpty(asmExt))
                        {
                            WriteNewConsoleLine("Best2 asm extension missing");
                            return 1;
                        }

                        asmOutFile = Path.ChangeExtension(outputFile, asmExt);
                        asmOutFilePtr = StoreIntPtr(Marshal.StringToHGlobalAnsi(asmOutFile));

                        if (keepFiles)
                        {
                            WriteNewConsoleLine("Asm output file: {0}", asmOutFile);
                        }

                        infoFile = Path.ChangeExtension(outputFile, infoExt);
                        infoFilePtr = StoreIntPtr(Marshal.StringToHGlobalAnsi(infoFile));

                        if (keepFiles)
                        {
                            WriteNewConsoleLine("Info file: {0}", infoFile);
                        }

                        if (libFiles == null || libFiles.Count == 0)
                        {
                            string stdLibFile = Path.Combine(ediabasBinPath, StdLibName);
                            if (!File.Exists(stdLibFile))
                            {
                                WriteNewConsoleLine("Standard lib not found: {0}", stdLibFile);
                                return 1;
                            }

                            libFiles = new List<string> { stdLibFile };
                            WriteNewConsoleLine("Using standard lib: {0}", stdLibFile);
                        }

                        libFilesPtr = new IntPtr[libFiles.Count + 1];
                        for (int i = 0; i < libFiles.Count; i++)
                        {
                            string libFile = Path.GetFullPath(libFiles[i]);
                            libFilesPtr[i] = StoreIntPtr(Marshal.StringToHGlobalAnsi(libFile));
                        }
                        libFilesPtr[libFiles.Count] = IntPtr.Zero;

                        string incDir = incDirs;
                        if (string.IsNullOrEmpty(incDirs))
                        {
                            incDir = Path.GetDirectoryName(inputFile);
                        }

                        incDir ??= string.Empty;
                        incDirsFilePtr = StoreIntPtr(Marshal.StringToHGlobalAnsi(incDir));

                        //Console.ReadKey();
                        int ccResult = is64Bit ? __best2Cc64(inputFilePtr, asmOutFilePtr, libFilesPtr, infoFilePtr, incDirsFilePtr) :
                            __best2Cc32(inputFilePtr, asmOutFilePtr, libFilesPtr, infoFilePtr, incDirsFilePtr);
                        //WriteNewConsoleLine("Best2 CC result: {0}", ccResult);
                        if (ccResult != 0)
                        {
                            WriteNewConsoleLine("Best2 CC failed");
                            return 1;
                        }

                        int ccTotal = is64Bit ? __best2CcTotal64() : __best2CcTotal32();
                        WriteNewConsoleLine("Best2 CC total: {0}", ccTotal);

                        int asmTotal = is64Bit ? __best2AsmTotal64() : __best2AsmTotal32();
                        WriteNewConsoleLine("Best2 ASM total: {0}", asmTotal);

                        int bestRevSize = 0x40;
                        bestRevPtr = StoreIntPtr(Marshal.AllocHGlobal(bestRevSize));
                        bestRevValuePtr = StoreIntPtr(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(uint))));

                        int revValue = is64Bit ? __best2Rev64(bestRevValuePtr, bestRevPtr) : __best2Rev32(bestRevValuePtr, bestRevPtr);
                        Int32 revValueBuf = Marshal.ReadInt32(bestRevValuePtr);
                        if (revValue != revValueBuf)
                        {
                            WriteNewConsoleLine("Best2 revision mismatch: {0:X08}, {1:X08}", revValue, revValueBuf);
                        }

                        string revString = Marshal.PtrToStringAnsi(bestRevPtr);
                        WriteNewConsoleLine("Best2 revision: {0}.{1}, '{2}'", (revValue >> 16) & 0xFFFF, revValue & 0xFFFF, revString);
                        asmRevValue = revValueBuf;
                        asmUserName = revString;

                        if (!File.Exists(asmOutFile))
                        {
                            WriteNewConsoleLine("Best2 generated asm output file not found: {0}", asmOutFile);
                            return 1;
                        }
                    }

                    // BEST1 init
                    Best1ErrorValueDelegate config1Result = is64Bit ? __best1Config64(Best1ProgressEvent, Best1ErrorTextEvent, Best1ErrorValueEvent) :
                        __best1Config32(Best1ProgressEvent, Best1ErrorTextEvent, Best1ErrorValueEvent);
                    if (config1Result == null)
                    {
                        WriteNewConsoleLine("Best1 config failed");
                        return 1;
                    }

                    int optionsResult = is64Bit ? __best1Options64(0) : __best1Options32(0);
                    // the option result is the specified value

                    if (revision.HasValue)
                    {
                        asmRevValue = revision.Value;
                    }

                    if (!string.IsNullOrEmpty(userName))
                    {
                        asmUserName = userName;
                    }

                    if (!string.IsNullOrEmpty(asmUserName))
                    {
                        userNamePtr = StoreIntPtr(Marshal.StringToHGlobalAnsi(asmUserName));
                    }

                    WriteNewConsoleLine("Asm revision: {0}.{1}", (asmRevValue >> 16) & 0xFFFF, asmRevValue & 0xFFFF);
                    if (!string.IsNullOrEmpty(asmUserName))
                    {
                        WriteNewConsoleLine("Asm user name: {0}", asmUserName);
                    }

                    IntPtr best1InputFilePtr = asmOutFilePtr != IntPtr.Zero ? asmOutFilePtr : inputFilePtr;
                    int init1Result = is64Bit ? __best1Init64(best1InputFilePtr, outputFilePtr, asmRevValue, userNamePtr, generateMapFile,
                            fileType, datePtr, passwordLabelPtr, passwordBufferPtr) :
                        __best1Init32(best1InputFilePtr, outputFilePtr, asmRevValue, userNamePtr, generateMapFile,
                            fileType, datePtr, passwordLabelPtr, passwordBufferPtr);
                    //WriteNewConsoleLine("Best1 init result: {0}", initResult);

                    if (init1Result != 0)
                    {
                        WriteNewConsoleLine("Best1 init failed");
                        return 1;
                    }

                    int asmResult = is64Bit ? __best1Asm64(mapFilePtr, infoFilePtr) :
                        __best1Asm32(mapFilePtr, infoFilePtr);
                    //WriteNewConsoleLine("Best1 asm result: {0}", asmResult);
                    if (asmResult != 0)
                    {
                        WriteNewConsoleLine("Best1 asm failed");
                        return 1;
                    }

                    IntPtr bestVersionPtr = is64Bit ? __best1AsmVersion64() : __best1AsmVersion32();
                    if (IntPtr.Zero != bestVersionPtr)
                    {
                        Int32 asmVer = Marshal.ReadInt32(bestVersionPtr);
                        WriteNewConsoleLine("BIP version: {0}.{1}.{2}", (asmVer >> 16) & 0xFF, (asmVer >> 8) & 0xFF, asmVer & 0xFF);
                    }

                    IntPtr pwdPtr = passwordBufferPtr;
                    for (int i = 0; i < MaxPasswords; i++)
                    {
                        string resultPassword = Marshal.PtrToStringAnsi(pwdPtr, MaxPasswordLen);
                        if (resultPassword != null)
                        {
                            resultPassword = resultPassword.Trim('\0');
                            if (!string.IsNullOrEmpty(resultPassword))
                            {
                                WriteNewConsoleLine("Password {0}: '{1}'", i + 1, resultPassword);
                            }
                        }

                        pwdPtr = IntPtr.Add(pwdPtr, MaxPasswordLen);
                    }

                    if (best2Api && !keepFiles)
                    {
                        if (!string.IsNullOrEmpty(asmOutFile))
                        {
                            if (File.Exists(asmOutFile))
                            {
                                File.Delete(asmOutFile);
                            }
                        }

                        if (!string.IsNullOrEmpty(infoFile))
                        {
                            if (File.Exists(infoFile))
                            {
                                File.Delete(infoFile);
                            }
                        }

                        string preproFile = Path.ChangeExtension(outputFile, preproExt);
                        if (File.Exists(preproFile))
                        {
                            File.Delete(preproFile);
                        }
                    }
                }
                finally
                {
                    if (best32Started)
                    {
                        if (is64Bit)
                        {
                            __best32Shutdown64();
                        }
                        else
                        {
                            __best32Shutdown32();
                        }
                    }

                    foreach (IntPtr intPtr in _freeIntPtrList)
                    {
                        if (intPtr != IntPtr.Zero)
                        {
                            Marshal.FreeHGlobal(intPtr);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WriteNewConsoleLine("Exception: {0}", e.Message);
                return 1;
            }
            finally
            {
                if (libHandle != IntPtr.Zero)
                {
                    NativeLibrary.Free(libHandle);
                }
            }

            return 0;
        }

        private static int Best1ProgressEvent(int value)
        {
            if (value >= 0)
            {
                if (_lastBest1OutLine != value && value % 100 == 0)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write("Line: {0}                             ", value);
                }
            }
            else
            {
                WriteNewConsoleLine("Done");
            }

            _lastBest1OutLine = value;
            return 0;
        }

        private static int Best1ErrorTextEvent(string text)
        {
            WriteNewConsoleLine("Error: {0}", text);
            return 0;
        }

        private static int Best1ErrorValueEvent(uint value, string text)
        {
            WriteNewConsoleLine("Error value: {0}: {1}", value, text);
            return 0;
        }

        private static int Best2ProgressEvent(int value1, int value2, int value3)
        {
            if (value1 >= 0)
            {
                if (value1 != _lastBest2OutLine && value1 % 10 == 0)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write("Line: {0}, {1}, {2}                             ", value1, value2, value3);
                }
            }
            else
            {
                WriteNewConsoleLine("Done");
            }

            _lastBest2OutLine = value1;
            return 0;
        }

        private static int Best2ErrorTextEvent(string text)
        {
            WriteNewConsoleLine("Error: {0}", text);
            return 0;
        }

        private static int Best2ErrorValueEvent(uint value, string text)
        {
            WriteNewConsoleLine("Error value: {0}: {1}", value, text);
            return 0;
        }

        private static void AdaptConsoleCursor()
        {
            if (Console.GetCursorPosition().Left > 0)
            {
                Console.WriteLine();
            }
        }

        private static void WriteNewConsoleLine(string format, object arg0 = null, object arg1 = null, object arg2 = null)
        {
            AdaptConsoleCursor();
            Console.WriteLine(format, arg0, arg1, arg2);
        }

        private static string DetectBestDllFolder(string bestDllName)
        {
            List<string> ediabasPathList = new List<string>();

            try
            {
                using (RegistryKey localMachine32 = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
                {
                    using (RegistryKey key = localMachine32.OpenSubKey(@"SOFTWARE\BMWGroup\ISPI\ISTA"))
                    {
                        string path = key?.GetValue("InstallLocation", null) as string;
                        if (!string.IsNullOrEmpty(path))
                        {
                            string ediabasBinPath = Path.Combine(path, @"Ediabas", @"bin");
                            if (Directory.Exists(ediabasBinPath))
                            {
                                ediabasPathList.Add(ediabasBinPath);
                            }
                        }
                    }
                }

                string ediabasPath = Environment.GetEnvironmentVariable("EDIABAS_PATH");
                if (!string.IsNullOrEmpty(ediabasPath))
                {
                    string ediabasBinPath = Path.Combine(ediabasPath, "bin");
                    if (Directory.Exists(ediabasBinPath))
                    {
                        ediabasPathList.Add(ediabasBinPath);
                    }
                }

                foreach (string ediabasBinPath in ediabasPathList)
                {
                    string bestDllPath = Path.Combine(ediabasBinPath, bestDllName);
                    if (File.Exists(bestDllPath))
                    {
                        long bestDllVerNum = GetBestDllVersion(bestDllPath);
                        if (bestDllVerNum >= MinBestVersion)
                        {
                            return ediabasBinPath;
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
            return null;
        }

        private static long GetBestDllVersion(string bestDllPath)
        {
            FileVersionInfo bestDllVersionInfo = FileVersionInfo.GetVersionInfo(bestDllPath);
            string bestDllVersion = bestDllVersionInfo.FileVersion;
            if (string.IsNullOrEmpty(bestDllVersion))
            {
                return 0;
            }

            string[] versionParts = bestDllVersion.Split('.');
            if (versionParts.Length != 3)
            {
                return 0;
            }

            long bestDllVerNum = (long.Parse(versionParts[0]) << 16) + (long.Parse(versionParts[1]) << 8) + long.Parse(versionParts[2]);
            return bestDllVerNum;
        }

        private static IntPtr StoreIntPtr(IntPtr intPtr)
        {
            if (intPtr != IntPtr.Zero && !_freeIntPtrList.Contains(intPtr))
            {
                _freeIntPtrList.Add(intPtr);
            }
            return intPtr;
        }
    }
}
