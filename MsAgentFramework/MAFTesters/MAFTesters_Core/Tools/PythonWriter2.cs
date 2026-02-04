using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using static System.Net.Mime.MediaTypeNames;

namespace MAFTesters_Core.Tools
{
    // TODO: make an up front agent that looks at the requirements and decides if it's:
    //  - simple enough for a one shot
    //  - if it should be sent to an agent tasked with making an outline of major functions.  then each block sent to a generator
    //
    // instead of asking that kind of question out of the blue, task an agent with creating a high level outline of what components
    // (major functions) would be needed.  then pass that to another agent that decides if a single script could be made of the 
    // whole thing, or if each component needs to break into sub components

    public class PythonWriter2
    {
        private readonly string _pythonFolder;
        private readonly bool _is_windows;
        private readonly ClientSettings _clientSettings;

        public PythonWriter2(string pythonFolder, bool is_windows, ClientSettings clientSettings)
        {
            _pythonFolder = pythonFolder;
            _is_windows = is_windows;
            _clientSettings = clientSettings;
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
            // Make sure there is a virtual environment
            PythonUtils.EnsurePythonFolderInitialized(_pythonFolder);

            // Filename needs to have invalid characters removed
            string filename_escaped = _is_windows ?
                FileSystemUtils.EscapeFilename_Windows(desiredFilename) :
                FileSystemUtils.EscapeFilename_Linux(desiredFilename);

            // Put a number suffix if the proposed file already exists
            var (filename, fullFilename) = FileSystemUtils.GetUniqueFilename(_pythonFolder, filename_escaped, "py");

            // Create agents
            var client = _clientSettings.CreateClient();
            var agents = CreateAgents(client);


            // TODO: figure out the best way to implement tasks within the function while keeping the outer caller non async

            Task<(string? contents, string? error_msg)> script = GetScriptContents(filename, requirements, agents.writer);







            return "";
        }

        // This never finishes.  Instead try a workflow
        private async Task<(string? contents, string? error_msg)> GetScriptContents(string script_name, string requirements, ChatClientAgent agent)
        {
            string prompt = $"{script_name}{Environment.NewLine}{Environment.NewLine}{requirements}";

            var workflow = new WorkflowBuilder(agent)
                .Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, prompt));

            var response = await WorkflowEventListener.ListenToStream(run);


            // there should just be one
            //response.Messages_Final       

            if (response.Messages_Final == null || response.Messages_Final.Length == 1)
                return (null, "ERROR: didn't get a response from writer agent");

            else if (response.Messages_Final.Length > 1)
                return (null, $"ERROR: multiple responses were returned from the writer agent: {response.Messages_Final.Length}");

            else if(string.IsNullOrWhiteSpace(response.Messages_Final[0].Text))
                return (null, "ERROR: got a blank response from writer agent");

            return (response.Messages_Final[0].Text, null);
        }




        private static (ChatClientAgent writer, ChatClientAgent validator) CreateAgents(IChatClient client)
        {

            // TODO: make slightly different agents: one if an entire script is desired, one if just a function is desired

            var writer = client.CreateAIAgent(
                instructions:
@"You are an agent inside of a tool that generates python scripts.  The user prompt will be the desired name of the python script as well as a detailed description of what the script needs to do.

Please generate the contents of the script (script file will be created using your generated code).

If the instrutions are too vague or contradictory, please respond with: 'ERROR: reason(s) for error'.

When writing the script, please think about edge cases, potential bugs, infinite loops, etc.

Please only output the contents of the script, since your response will be used programatically",
                //tools: [],
                name: $"{nameof(PythonWriter)}_WriterAgent");

            var validator = client.CreateAIAgent(
                instructions:
@"You are an agent responsible for validating generated python scripts.

The user prompt will be name and requirements of the script, as well as the script that was generated.

The script will already have passed static code analysis before it gets to you.

Please examine the code for adherence to requirements, flaws in design, security holes, etc.  Think about possible values of each param, does the script account for odd values?  Does the script try to do more than what the requirements want?  Are there confusing pieces of code that could be simplified?

Your response will be parsed programmatically, so either respond with:
SUCCESS

or with:
ERROR: reason(s) for error",
                //tools: [],
                name: $"{nameof(PythonWriter)}_ValidatorAgent");

            return (writer, validator);
        }
    }
}
