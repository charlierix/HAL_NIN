using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.ComponentModel;
using System.Text;

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

        [Description("Creates a python script that will satisfy the requirements.  This script could be for anything: complex math, analyze a file, etc.  Returns the filename that was created, or an error message")]
        public Task<ResponseMessage> GeneratePythonScriptAsync(
            [Description("What the python file should be called (the folder will be defined by the session, so only need filename, .py will automatically be added to the end of the filename)")]
            string desiredFilename,
            [Description("Details about what this script should do, expected parameters, what it should return")]
            string requirements)
        {
            try
            {
                return GeneratePythonScript_private(desiredFilename, requirements);
            }
            catch (Exception ex)
            {
                return Task.FromResult(ResponseMessage.BuildError($"Caught exception in python writer: {ex}"));
            }
        }

        private async Task<ResponseMessage> GeneratePythonScript_private(string desiredFilename, string requirements)
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



            // do an intial pass at the requirements and see if it's simple enough for a one shot or if it needs to be broken up and recursed



            // TODO: make another agent that documents the script, merging requirements with code
            //  this should help a future agent that looks through generated scripts and builds tools out of them (trying to find common
            //  generated solutions and make a dedicated robust tool)




            var error_history = new List<string>();
            bool success = false;

            for (int i = 0; i < 12; i++)
            {
                // Generate python
                var script = await GetScriptContents(filename_escaped, requirements, error_history, agents.writer);
                if (!script.IsSuccess)
                    return script;

                // Do static analysis of generated python
                File.WriteAllText(fullFilename, script.Message);

                string? lint_error = PythonUtils.CheckForErrors(_pythonFolder, filename);

                // if this is a partial file, delete it

                if (!string.IsNullOrWhiteSpace(lint_error))
                {
                    error_history.Add(lint_error);
                    continue;
                }

                // Ask llm to look for issues
                string? python_error_msg = await ValidatePythonContents(filename_escaped, requirements, script, agents.validator);

                if (string.IsNullOrWhiteSpace(python_error_msg))
                {
                    success = true;
                    break;
                }

                error_history.Add(python_error_msg);
            }

            if (!success)
            {
                // TODO: decide whether to kick back to the parent or attempt a divide and recurse

                return BuildRetryHistoryErrorResponse(requirements, error_history);
            }





            // gather up all the results and build a final file (if there were partial files)

            // if there were partial files, do another lint




            return ResponseMessage.BuildSuccess(fullFilename);
        }

        private async Task<ResponseMessage> GetScriptContents(string script_name, string requirements, List<string> error_history, ChatClientAgent agent)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine($"filename: {script_name}");
            prompt.AppendLine();
            prompt.AppendLine("```requirements");
            prompt.AppendLine(requirements);
            prompt.AppendLine("```");

            if (error_history.Count > 0)
            {
                prompt.AppendLine();
                string s = error_history.Count > 1 ? "s" : "";
                prompt.AppendLine($"error message{s} from previous attempt{s} at writing this:");

                for (int i = 0; i < error_history.Count; i++)
                {
                    if (i > 0)
                        prompt.AppendLine();

                    prompt.AppendLine("```error");
                    prompt.AppendLine(error_history[i]);
                    prompt.AppendLine("```");
                }
            }

            var workflow = new WorkflowBuilder(agent).
                WithOutputFrom(agent).
                Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, prompt.ToString()));

            var response = await WorkflowEventListener.ListenToStream(run);

            // Get the response text
            var retVal = response.GetSingleMessage("writer");

            if (!retVal.IsSuccess)
                return retVal;

            // Despite prompting not to, the model kept wrapping the script in a markdown block.  Call a dedicated function
            // to make sure it's not wrapped in markdown
            string cleaned = MarkdownParser.ExtractOnlyText(retVal.Message);

            return ResponseMessage.BuildSuccess(cleaned);
        }

        // Calls an agent to validate the python text, returns an error message or null
        private async Task<string?> ValidatePythonContents(string script_name, string requirements, ResponseMessage source_code, ChatClientAgent agent)
        {
            string prompt =
$@"filename: {script_name}

```requirements
{requirements}
```

```python
{source_code.Message}
```";

            var workflow = new WorkflowBuilder(agent)
                .Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, prompt));

            var response = await WorkflowEventListener.ListenToStream(run);

            var message = response.GetSingleMessage("validator");
            if (!message.IsSuccess)
                return string.IsNullOrWhiteSpace(message.ErrorMessage) ?
                    "No response back from the python validator" :
                    message.ErrorMessage;

            else if (message.Message.Trim().Equals("success", StringComparison.OrdinalIgnoreCase))
                return null;

            return message.Message;
        }

        private static ResponseMessage BuildRetryHistoryErrorResponse(string requirements, List<string> error_history)
        {
            var report = new StringBuilder();

            report.AppendLine("Had trouble building python script after multiple retries");
            report.AppendLine();
            report.AppendLine("```requirements");
            report.AppendLine(requirements);
            report.AppendLine("```");

            foreach (string error in error_history)
            {
                report.AppendLine();
                report.AppendLine("```error");
                report.AppendLine(error);
                report.AppendLine("```");
            }

            return ResponseMessage.BuildError(report.ToString());
        }

        private static (ChatClientAgent writer, ChatClientAgent validator) CreateAgents(IChatClient client)
        {

            // TODO: make slightly different agents: one if an entire script is desired, one if just a function is desired

            var writer = client.AsAIAgent(
                instructions:
@"You are an agent inside of a tool that generates python scripts.  The user prompt will be the desired name of the python script as well as a detailed description of what the script needs to do.

Please generate the contents of the script (script file will be created using your generated code).

If the instrutions are too vague or contradictory, please respond with: 'ERROR: reason(s) for error'.

When writing the script, please think about edge cases, potential bugs, infinite loops, etc.

Please only output the contents of the script that can be pasted directly into a python script (don't wrap in markdown), since your response will be used programatically",
                //tools: [],
                name: $"{nameof(PythonWriter)}_WriterAgent");

            var validator = client.AsAIAgent(
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
