using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace MAFTesters_Core
{
    public static class PythonUtils
    {
        public const string VIRTUAL_ENV = ".venv";

        /// <summary>
        /// Creates the folder if necessary, makes sure virtual environment is created, does core pip installs
        /// </summary>
        public static void EnsurePythonFolderInitialized(string folder)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            if (Directory.Exists(Path.Combine(folder, VIRTUAL_ENV)))
                return;

            // Create the virtual environment
            //Process.Start("python", $"-m venv {VIRTUAL_ENV}");        // need to wait for it to finish
            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "python",
                    Arguments = $"-m venv {VIRTUAL_ENV}",

                    UseShellExecute = false,
                    WorkingDirectory = folder,

                    WindowStyle = ProcessWindowStyle.Hidden,

                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                }
            };
            process.Start();
            process.WaitForExit();

            ThrowIfError(process, "Error setting up virtual environment");

            // pip install --upgrade pyflakes
            PipInstall(folder, "pyflakes", true);
        }

        /// <summary>
        /// Runs static analysis on the python script
        /// </summary>
        /// <param name="python_folder">folder that has .venv sub folder</param>
        /// <param name="script_name">filename of the python script, should be in python_folder</param>
        /// <returns>
        /// null: file looks good (no errors reported)
        /// non null: error report
        /// </returns>
        public static string? CheckForErrors(string python_folder, string script_name)
        {
            string pyflakesExePath = Path.Combine(python_folder, VIRTUAL_ENV, "Scripts", "pyflakes.exe");

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pyflakesExePath,
                    Arguments = $"\"{script_name}\"",

                    UseShellExecute = false,
                    WorkingDirectory = python_folder,

                    WindowStyle = ProcessWindowStyle.Hidden,

                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                }
            };
            process.Start();
            process.WaitForExit();

            ThrowIfError(process, "Error running pyflakes");

            string errors = process.StandardOutput.ReadToEnd();     // pyflakes doesn't print anything if the file is valid

            return string.IsNullOrWhiteSpace(errors) ? null : errors;
        }

        // This will do a pip install, virtual environment must be set before this
        private static void PipInstall(string folder, string packageName, bool isUpgrade)
        {
            string pythonExePath = Path.Combine(folder, VIRTUAL_ENV, "Scripts", "python.exe");

            string args = "-m pip install";

            if (isUpgrade)
                args += " --upgrade";

            args += $" {packageName}";

            Process process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = pythonExePath,
                    Arguments = args,

                    UseShellExecute = false,
                    WorkingDirectory = folder,

                    WindowStyle = ProcessWindowStyle.Hidden,

                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                }
            };
            process.Start();
            process.WaitForExit();

            ThrowIfError(process, "Error running pip install");
        }

        private static void ThrowIfError(Process process, string message)
        {
            if (!process.StartInfo.RedirectStandardError)
                return;

            string exception = process.StandardError.ReadToEnd();
            if (string.IsNullOrEmpty(exception))
                return;

            throw new ApplicationException($"{message}:{Environment.NewLine}{process.StartInfo.FileName} {process.StartInfo.ArgumentList}{Environment.NewLine}{exception}");
        }
    }
}
