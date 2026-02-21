using System.ComponentModel;

namespace MAFTesters_Agents.Tools
{
    public class PythonRunner
    {
        private readonly string _pythonFolder;

        public PythonRunner(string pythonFolder) 
        {
            _pythonFolder = pythonFolder;
        }

        [Description("Runs a python script.  This tool will make sure a virtual environment is set up and all necessary pip installs are performed before running the script.  Response string is whatever the script is made to return")]
        public string ExecutePythonScript(
            [Description("filename of the python script (folder isn't needed, that was passed in this tool's constructor)")]
            string scriptName,
            [Description("Parameters to pass to the script (names and values)")]
            Dictionary<string, string> parameters)
        {

            // if scriptName contains folder, strip that, make sure that the remaining filename is in _pythonFolder


            return "";
        }
    }
}
