using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace FindDependenies
{
    internal static class Program
    {
        private static readonly string ThisDir = AppDomain.CurrentDomain.BaseDirectory;
        private static readonly string CurDir = Environment.CurrentDirectory;
        private static readonly string DependsPath = Path.Combine(ThisDir, "depends.exe");
        private static string _resultDir;
        private static string _pathsDwpPath;
        private static string _resultCsvPath;
        private static string _resultScriptPath;
        private static string[] _paths;
        private static string[] _modules;

        private static void Main(string[] args)
        {
            if (args.Length <= 1 || args.First() == "/?")
            {
                const string help =
                    "Finds dependencies for given executable. First argument specifies path to executable, second - file with paths, separated with new lines. " +
                    "Output is a batch script that xcopies all dependency dlls to a given folder. Third argument could be result folder name.";
                Console.WriteLine(help);
                return;
            }

            try
            {
                PrepareResultDir(args.Length == 3 ? args[2] : Path.GetFileNameWithoutExtension(args[0]));
                CreatePathsDwpFile(args[1]);
                Execute(args[0]);
                CreateBatchScript();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: {0}", e.Message);
            }
        }

        private static void PrepareResultDir(string resultDir)
        {
            _resultDir = Path.IsPathRooted(resultDir) ? resultDir : Path.Combine(CurDir, resultDir);
            _pathsDwpPath = Path.Combine(_resultDir, "paths.dwp");
            _resultCsvPath = Path.Combine(_resultDir, "result.csv");
            _resultScriptPath = Path.Combine(_resultDir, "copydlls.bat");

            if (Directory.Exists(_resultDir))
            {
                Directory.Delete(_resultDir, true);
                Thread.Sleep(100);
            }
            Directory.CreateDirectory(_resultDir);
        }

        private static void CreateBatchScript()
        {
            Console.WriteLine("Creating batch script...");
            const string startText = @"
@echo off
set _dest=bin\
";
            var scriptText = startText +
                             string.Join(Environment.NewLine,
                                 _modules.Select(module => $"xcopy \"{module}\" \"%_dest%\" /y"));
            File.WriteAllText(_resultScriptPath, scriptText);
            Console.WriteLine("Script ready.");
        }

        private static void Execute(string exePath)
        {
            if (!File.Exists(exePath))
            {
                throw new ApplicationException("Input exe file does not exist.");
            }
            Console.WriteLine("Executing depends.exe...");
            File.Delete(_resultCsvPath);
            Process.Start(DependsPath, $"/c /f:1 /d:\"{_pathsDwpPath}\" /oc:\"{_resultCsvPath}\" \"{exePath}\"");
            WaitUntilResultWritten();
            ParseResult();
        }

        private static void ParseResult()
        {
            Console.WriteLine("Parsing result...");
            _modules = File.ReadAllLines(_resultCsvPath).Select(row => row.Split(','))
                .Select(cells => cells.ElementAtOrDefault(1))
                .Where(m => m != null)
                .Select(m => m.ToLower().Trim(' ', '"', '\''))
                .Where(m => _paths.Any(m.StartsWith))
                .ToArray();
        }

        private static void WaitUntilResultWritten()
        {
            const int maxWaitTimeSec = 20;
            for (var i = 0; i <= maxWaitTimeSec * 10; i++)
            {
                if (File.Exists(_resultCsvPath))
                {
                    return;
                }
                Thread.Sleep(100);
            }
            throw new ApplicationException("Result file timeout.");
        }

        private static void CreatePathsDwpFile(string pathsFilePath)
        {
            _paths =
                File.ReadAllLines(pathsFilePath)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(Environment.ExpandEnvironmentVariables)
                    .Select(p => p.ToLower().Trim(' ', '"', '\''))
                    .ToArray();
            const string predefinedPaths =
                "SxS\r\nKnownDLLs\r\nAppDir\r\n32BitSysDir\r\n16BitSysDir\r\nOSDir\r\nAppPath\r\nSysPath\r\n";
            var pathsDwpText = predefinedPaths + string.Join(Environment.NewLine, _paths.Select(p => "UserDir " + p));
            File.WriteAllText(_pathsDwpPath, pathsDwpText);
        }
    }
}