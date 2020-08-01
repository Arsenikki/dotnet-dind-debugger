using EnvDTE;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text.RegularExpressions;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;

namespace DebuggerLauncher
{
    internal static class Program
    {
        private static string _projectRootPath = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
        private static bool _detachFlag;

        private static void Main()
        {
            // Get the correct Visual Studio process with the specified title
            var correctDevEnvProcess = FindCorrectVisualStudioProcess("put project title or part of it here");
            var dte = GetDTE(correctDevEnvProcess.Id, 10);

            // Create cmd process to run the container workload using docker-compose files
            var DockerRunner = new System.Diagnostics.ProcessStartInfo
            {
                WorkingDirectory = _projectRootPath,
                FileName = "cmd.exe",
                UseShellExecute = false
            };

            DockerRunner.Arguments = "/K docker-compose up --build --force-recreate <outer_container_name>";

            // Start the process
            Process.Start(DockerRunner);

            // Create cmd process to check if the inner container is running
            var containerAvailabilityCheckerProcessConfig = new System.Diagnostics.ProcessStartInfo
            {
                WorkingDirectory = _projectRootPath,
                FileName = "cmd.exe",
                Arguments = "/K docker-compose exec -T <outer_container_name> docker inspect -f '{{.State.Running}}' <inner_container_2_name>",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            while (!System.Diagnostics.Debugger.IsAttached)
            {
                Process containerAvailabilityCheckerProcessConfig = Process.Start(containerAvailabilityCheckerProcessConfig);
                string stdout = containerAvailabilityCheckerProcessConfig.StandardOutput.ReadLine();

                // Checks the container availability once every second
                Thread.Sleep(1000);
                if (stdout.Equals("'true'"))
                {
                    // Set path for debugger configuration
                    var pathToFolder = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.FullName;
                    var pathToConfigFile = Path.Combine(pathToDebuggerLauncher, "debuggerConfig.json");

                    // Attach the debugger to the specified container
                    AttachDebugger(pathToConfigFile, dte);
                    break;
                }
            }

            // Keep debugger connected until stopped
            while (!_detachFlag)
            {
                var keyInfo = Console.ReadKey();
                _detachFlag = keyInfo.Key == ConsoleKey.C && keyInfo.Modifiers == ConsoleModifiers.Control;
                dte.Debugger.DetachAll();
            }
        }

        private static Process FindCorrectVisualStudioProcess(string selectedTitle)
        {
            var devEnvProcesses = Process.GetProcessesByName("devenv");
            foreach (var process in devEnvProcesses)
            {
                var ProcessTitle = process.MainWindowTitle;
                if (ProcessTitle.Contains(selectedTitle))
                {
                    Console.WriteLine($"Visual Studio instance found with title: {ProcessTitle}");
                    return process;
                }
                Console.WriteLine(ProcessTitle);
            }
            Console.WriteLine($"No Visual Studio instance found with title including word" + selectedTitle);
            return null;
        }

        private static void AttachDebugger(string debugConfigurationPath, DTE dte)
        {
            dte.ExecuteCommand("DebugAdapterHost.Launch", $"/LaunchJson:{debugConfigurationPath}");
        }

        /// Gets the DTE object from any devenv process.
        private static EnvDTE.DTE GetDTE(int processId, int timeout)
        {
            EnvDTE.DTE res = null;
            DateTime startTime = DateTime.Now;

            while (res == null && DateTime.Now.Subtract(startTime).Seconds < timeout)
            {
                Thread.Sleep(1000);
                res = GetDTE(processId);
            }

            return res;
        }


        [DllImport("ole32.dll")]
        private static extern int CreateBindCtx(uint reserved, out IBindCtx ppbc);

        /// Gets the DTE object from any devenv process.
        private static EnvDTE.DTE GetDTE(int processId)
        {
            object runningObject = null;

            IBindCtx bindCtx = null;
            IRunningObjectTable rot = null;
            IEnumMoniker enumMonikers = null;

            try
            {
                Marshal.ThrowExceptionForHR(CreateBindCtx(reserved: 0, ppbc: out bindCtx));
                bindCtx.GetRunningObjectTable(out rot);
                rot.EnumRunning(out enumMonikers);

                IMoniker[] moniker = new IMoniker[1];
                IntPtr numberFetched = IntPtr.Zero;
                while (enumMonikers.Next(1, moniker, numberFetched) == 0)
                {
                    IMoniker runningObjectMoniker = moniker[0];

                    string name = null;

                    try
                    {
                        if (runningObjectMoniker != null)
                        {
                            runningObjectMoniker.GetDisplayName(bindCtx, null, out name);
                        }
                    }
                    catch (UnauthorizedAccessException)
                    {
                        // Do nothing, there is something in the ROT that we do not have access to.
                    }

                    Regex monikerRegex = new Regex(@"!VisualStudio.DTE\.\d+\.\d+\:" + processId, RegexOptions.IgnoreCase);
                    if (!string.IsNullOrEmpty(name) && monikerRegex.IsMatch(name))
                    {
                        Marshal.ThrowExceptionForHR(rot.GetObject(runningObjectMoniker, out runningObject));
                        break;
                    }
                }
            }
            finally
            {
                if (enumMonikers != null)
                {
                    Marshal.ReleaseComObject(enumMonikers);
                }

                if (rot != null)
                {
                    Marshal.ReleaseComObject(rot);
                }

                if (bindCtx != null)
                {
                    Marshal.ReleaseComObject(bindCtx);
                }
            }

            return runningObject as EnvDTE.DTE;
        }

    }
}