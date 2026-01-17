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

            Process.Start("python", $"-m venv {VIRTUAL_ENV}");

            // figure out how to run pip install in virtual environment using process


            // pip install --upgrade pyflakes


        }

    }
}
