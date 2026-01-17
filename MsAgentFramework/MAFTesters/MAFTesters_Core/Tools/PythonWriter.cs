using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System.ComponentModel;

namespace MAFTesters_Core.Tools
{

    // TODO: make an up front agent that looks at the requirements and decides if it's:
    //  - simple enough for a one shot
    //  - if it should be sent to an agent tasked with making an outline of major functions.  then each block sent to a generator

    public class PythonWriter
    {
        private readonly string _pythonFolder;
        private readonly ChatClientAgent _agent_writer;
        private readonly ChatClientAgent _agent_validator;

        public PythonWriter(string pythonFolder, ChatClientAgent agent_writer, ChatClientAgent agent_validator)
        {
            _pythonFolder = pythonFolder;
            _agent_writer = agent_writer;
            _agent_validator = agent_validator;
        }

        public static (ChatClientAgent writer, ChatClientAgent validator) CreateAgents(IChatClient client)
        {
            var writer = client.CreateAIAgent(
                instructions:
@"You are an agent inside of a tool that generates python scripts.  The user prompt will be the desired name of the python script as well as a detailed description of what the script needs to do.

Please generate the contents of the script (script file will be created using your generated code).

If the instrutions are too vague or contradictory, please respond with: 'ERROR: reason(s) for error'.

When writing the script, please think about edge cases, potential bugs, infinite loops, etc.",
                //tools: [],
                name: $"{nameof(PythonWriter)}_WriterAgent");

            var validator = client.CreateAIAgent(
                instructions:
@"You are an agent responsible for validating generated python scripts.

The user prompt will be name and requirements of the script, as well as the script that was generated.

The script will already have passed static code analysis before it gets to you.

Please examine the code for adherence to requirements, flaws in design, security holes, etc.

Your response will be parsed programmatically, so either respond with:
SUCCESS

or with:
ERROR: reason(s) for error",
                //tools: [],
                name: $"{nameof(PythonWriter)}_ValidatorAgent");

            return (writer, validator);
        }

        // This interface will probably evolve over time.  Start with a single string that is just requirements.  As this
        // gets used over time, other overloads may be wanted with more specialized params

        [Description("Creates a python script that will satisfy the requirements.  This script could be for anything: complex math, analyze a file, etc.  Returns the filename that was created")]
        public string GeneratePythonScript(
            [Description("What the python file should be called (the folder will be defined by the session, so only need filename, .py will automatically be added to the end of the filename)")]
            string desiredFilename,
            [Description("Details about what this script should do, expected parameters, what it should return")]
            string requirements)
        {
            try
            {
                PythonUtils.EnsurePythonFolderInitialized(_pythonFolder);


                // filename needs to have invalid characters removed


                // run the generated script through code analysis, repeat the cycle if there is an error
                // https://pypi.org/project/pyflakes/




                return "";
            }
            catch (Exception ex)
            {
                return $"Exception: {ex.Message}";
            }
        }

        /// <summary>
        /// If there are errors, they are returned in the string.  Returns null if no errors
        /// </summary>
        private static string Validate_PyFlakes(string filename)
        {
            // pip install --upgrade pyflakes

            // pyflakes example.py

            // errors are listed one line at a time:
            /*
            (.venv) C:\Users\PerfNormBeast\Desktop\agent playground\working folders\a>pyflakes example.py
            example.py:1:1: 'sys' imported but unused
            example.py:4:5: local variable 'greeting' is assigned to but never used
            example.py:5:11: undefined name 'greating'

            (.venv) C:\Users\PerfNormBeast\Desktop\agent playground\working folders\a>
            */

            // if no issues, then it exits with no text:
            /*
            (.venv) C:\Users\PerfNormBeast\Desktop\agent playground\working folders\a>pyflakes example2.py

            (.venv) C:\Users\PerfNormBeast\Desktop\agent playground\working folders\a>
            */



            return "FINISH THIS";
        }
    }
}
