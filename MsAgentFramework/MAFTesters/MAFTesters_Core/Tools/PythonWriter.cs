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


    // TODO: make this return an object
    //  error message about why it failed
    //  generated python script filename
    //  markdown filename - this contains requirements, struggles.  it can be mined by some other process that looks for groups of similar scripts to turn into more robust tools
    //
    // another 'file' that could be useful is a usage tracker (probably a db).  every time one of these scripts is used, a log
    // entry is made.  maybe not full input, but a score if it works as expected.  if there was an error, store a bug report with
    // inputs and outcome



    // TODO: it's outside the scope of this tool, but a caller tool could try to break the problem down...this tool builds the code
    // top down.  take in a problem, if it's too big for a one shot, divide and recurse
    //
    // another approach is how this tool was written:  get one piece working and tested, then add functionality.  basically growing
    // the code from an initial simplified requirement, then adding requirements and rewriting, always satisfying unit tests as it
    // grows


    public class PythonWriter
    {
        private readonly string _pythonFolder;
        private readonly bool _is_windows;
        private readonly ClientSettings _clientSettings;

        public PythonWriter(string pythonFolder, bool is_windows, ClientSettings clientSettings)
        {
            _pythonFolder = pythonFolder;
            _is_windows = is_windows;
            _clientSettings = clientSettings;
        }

        // This interface will probably evolve over time.  Start with a single string that is just requirements.  As this
        // gets used over time, other overloads may be wanted with more specialized params

        [Description("Creates a python script that will satisfy the requirements.  This script could be for anything: complex math, analyze a file, etc.  Returns the filename that was created, or an error message")]
        public Task<PythonWriter_Response> GeneratePythonScriptAsync(
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
                return Task.FromResult(PythonWriter_Response.BuildError($"Caught exception in python writer: {ex}"));
            }
        }

        private async Task<PythonWriter_Response> GeneratePythonScript_private(string desiredFilename, string requirements)
        {
            // Make sure there is a virtual environment
            PythonUtils.EnsurePythonFolderInitialized(_pythonFolder);

            // Filename needs to have invalid characters removed
            string filename_escaped = _is_windows ?
                FileSystemUtils.EscapeFilename_Windows(desiredFilename) :
                FileSystemUtils.EscapeFilename_Linux(desiredFilename);

            // Put a number suffix if the proposed file already exists
            var filename_base = FileSystemUtils.GetUniqueFilename(_pythonFolder, filename_escaped, "py", "md");
            var filename_py = filename_base.GetFinalName("py");
            var filename_md = filename_base.GetFinalName("md");

            // Create agents
            var client = _clientSettings.CreateClient();
            var agent_writer = CreateAgent_Writer(client);
            var agent_validator = CreateAgent_Validator(client);
            var agent_requirement_refiner = CreateAgent_RequirementRefiner(client);



            // do an intial pass at the requirements and see if it's simple enough for a one shot or if it needs to be broken up and recursed



            // TODO: make another agent that documents the script, merging requirements with code
            //  this should help a future agent that looks through generated scripts and builds tools out of them (trying to find common
            //  generated solutions and make a dedicated robust tool)



            string script_prompt = "";
            string script_contents = "";
            var error_history = new List<ValidatorResponse[]>();
            bool success = false;

            for (int i = 0; i < 12; i++)
            {
                // Possibly refine the prompt if there have been errors
                script_prompt = await GetScriptPrompt(filename_escaped, requirements, error_history, client, agent_requirement_refiner);

                // Generate python
                var script = await GetScriptContents(script_prompt, agent_writer);
                if (!script.IsSuccess)
                    return PythonWriter_Response.BuildError(script.ErrorMessage);

                // Do static analysis of generated python
                script_contents = script.Message;
                File.WriteAllText(filename_py.full_path, script.Message);

                string? lint_error = PythonUtils.CheckForErrors(_pythonFolder, filename_py.name_nofolder);

                // if this is a partial file, delete it

                if (!string.IsNullOrWhiteSpace(lint_error))
                {
                    error_history.Add([new ValidatorResponse { Severity = IssueSeverity.Error, Description = lint_error }]);
                    continue;
                }

                // Ask llm to look for issues
                var python_error_msgs = await ValidatePythonContents(filename_escaped, requirements, script, client, agent_validator);

                if (!python_error_msgs.Any(o => o.Severity == IssueSeverity.Error))
                {
                    success = true;
                    break;
                }

                File.Delete(filename_py.full_path);

                error_history.Add(python_error_msgs);
            }

            if (!success)
            {
                // TODO: decide whether to kick back to the parent or attempt a divide and recurse
                // this would be an agent that is told to make an executive decision
                //  - what are the contentious points, for each
                //      - is it way too vague?
                //      - can it be solved by returning solutions for each possible interpretation of the requirement?
                //  - is it simply failing because the code is buggy?
                //      - can it be broken into cleaner sets of requirements?
                //
                // after typing this out, it looks like a fragile decision tree.  it may be better to have a few agents
                // that sit in committee, each agent playing a certain role

                return BuildRetryHistoryErrorResponse(requirements, error_history);
            }





            // gather up all the results and build a final file (if there were partial files)

            // if there were partial files, do another lint



            // Generate a markdown that documents the script
            var agent_documenter = CreateAgent_Documentation(client);

            string documentation = await DocumentScript(requirements, script_prompt, script_contents, agent_documenter);
            if(documentation != null)
                File.WriteAllText(filename_md.full_path, documentation);


            return PythonWriter_Response.BuildSuccess(filename_py.full_path, filename_md.full_path);
        }

        #region Private Methods - agent calls

        private async Task<string> GetScriptPrompt(string script_name, string requirements, List<ValidatorResponse[]> error_history, IChatClient client, ChatClientAgent agent)
        {
            string new_requirements;
            string? error_summary = null;

            if (error_history.Count < 2)
            {
                new_requirements = requirements;

                if (error_history.Count == 1)
                    error_summary = GetValidatorResponseReport(error_history[0]);
            }
            else
            {
                var refined = await GetScriptPrompt_Refine(requirements, error_history, client, agent);
                if (refined == null)
                    throw new ApplicationException($"Couldn't cast refine call result to {nameof(PromptRefine)} object");

                new_requirements = refined.NewPrompt;
                error_summary = GetValidatorResponseReport(refined.SummarizedErrorList);
            }

            var prompt = new StringBuilder();

            prompt.AppendLine($"filename: {script_name}");
            prompt.AppendLine();
            prompt.AppendLine("```requirements");
            prompt.AppendLine(new_requirements);
            prompt.AppendLine("```");

            if (error_summary != null)
            {
                prompt.AppendLine();
                prompt.AppendLine($"error messages from previous attempt at writing this:");
                prompt.AppendLine();
                prompt.AppendLine(error_summary);
            }

            return prompt.ToString();
        }
        private async Task<PromptRefine> GetScriptPrompt_Refine(string requirements, List<ValidatorResponse[]> error_history, IChatClient client, ChatClientAgent agent)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine("```requirements");
            prompt.AppendLine(requirements);
            prompt.AppendLine("```");

            foreach (var error_set in error_history)
            {
                prompt.AppendLine();
                prompt.AppendLine(GetValidatorResponseReport(error_set));
            }

            var workflow = new WorkflowBuilder(agent).
                WithOutputFrom(agent).
                Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, prompt.ToString()));

            var response = await WorkflowEventListener.ListenToStream(run);

            // Get the response text
            var refine_result = response.GetSingleMessage(agent.Name);

            if (!refine_result.IsSuccess)
                throw new ApplicationException($"Had trouble calling prompt refiner: {refine_result.GetReport()}");

            // Cast to response objct
            return await StronglyTypedPromptHelper<PromptRefine>.ParseResponse(refine_result.Message, client: client);
        }

        private async Task<ResponseMessage> GetScriptContents(string prompt, ChatClientAgent agent)
        {
            var workflow = new WorkflowBuilder(agent).
                WithOutputFrom(agent).
                Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, prompt));

            var response = await WorkflowEventListener.ListenToStream(run);

            // Get the response text
            var retVal = response.GetSingleMessage(agent.Name);

            if (!retVal.IsSuccess)
                return retVal;

            // Despite prompting not to, the model kept wrapping the script in a markdown block.  Call a dedicated function
            // to make sure it's not wrapped in markdown
            string cleaned = MarkdownParser.ExtractOnlyText(retVal.Message);

            return ResponseMessage.BuildSuccess(cleaned);
        }

        // Calls an agent to validate the python text, returns an error message or null
        private async Task<ValidatorResponse[]> ValidatePythonContents(string script_name, string requirements, ResponseMessage source_code, IChatClient client, ChatClientAgent agent)
        {
            string prompt =
$@"filename: {script_name}

```requirements
{requirements}
```

```python
{source_code.Message}
```";

            var workflow = new WorkflowBuilder(agent).
                WithOutputFrom(agent).
                Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, prompt));

            var response = await WorkflowEventListener.ListenToStream(run);

            var message = response.GetSingleMessage(agent.Name);
            if (!message.IsSuccess)
                return string.IsNullOrWhiteSpace(message.ErrorMessage) ?
                    [new ValidatorResponse { Severity = IssueSeverity.Error, Description = "No response back from the python validator" }] :
                    [new ValidatorResponse { Severity = IssueSeverity.Error, Description = message.ErrorMessage }];

            else if (string.IsNullOrWhiteSpace(message.Message))
                return [];

            var validations = await StronglyTypedPromptHelper<ValidatorResponse[]>.ParseResponse(message.Message, client: client);

            return validations;
        }

        private async Task<string> DocumentScript(string requirements, string script_prompt, string script_contents, ChatClientAgent agent)
        {
            var prompt = new StringBuilder();

            prompt.AppendLine(script_prompt);       // see GetScriptPrompt(), it already includes filename, new requirements, error list

            if (requirements != script_prompt)
            {
                // Since these are different, add the original requirements
                prompt.AppendLine();
                prompt.AppendLine("```original_requirements");
                prompt.AppendLine(requirements);
                prompt.AppendLine("```");
            }

            var workflow = new WorkflowBuilder(agent).
                WithOutputFrom(agent).
                Build();

            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, prompt.ToString()));

            var response = await WorkflowEventListener.ListenToStream(run);

            var message = response.GetSingleMessage(agent.Name);

            if (!message.IsSuccess || string.IsNullOrWhiteSpace(message.Message))
                return null;

            var retVal = new StringBuilder();

            retVal.AppendLine(MarkdownParser.RemoveOuterFence(message.Message));

            retVal.AppendLine();
            retVal.AppendLine();
            retVal.AppendLine("# Prompts");
            retVal.AppendLine("These are snippets that were passed to the generator when making the script");
            retVal.AppendLine();
            retVal.AppendLine();
            retVal.AppendLine(prompt.ToString());

            return retVal.ToString();
        }

        #endregion
        #region Private Methods

        private static PythonWriter_Response BuildRetryHistoryErrorResponse(string requirements, List<ValidatorResponse[]> error_history)
        {
            var report = new StringBuilder();

            report.AppendLine("Had trouble building python script after multiple retries");
            report.AppendLine();
            report.AppendLine("```requirements");
            report.AppendLine(requirements);
            report.AppendLine("```");

            foreach (var run in error_history)
            {
                report.AppendLine();
                report.AppendLine(GetValidatorResponseReport(run));
            }

            return PythonWriter_Response.BuildError(report.ToString());
        }

        private static string GetValidatorResponseReport(ValidatorResponse[] errors, bool wrap_in_markdown_block = true)
        {
            var retVal = new StringBuilder();

            if (wrap_in_markdown_block)
                retVal.AppendLine($"```error{(errors.Length > 1 ? "s" : "")}");

            foreach (var error in errors)
                retVal.AppendLine($"{error.Severity}: {error.Description}");

            if (wrap_in_markdown_block)
                retVal.AppendLine("```");

            return retVal.ToString();
        }

        private static ChatClientAgent CreateAgent_Writer(IChatClient client)
        {

            // TODO: make slightly different agents: one if an entire script is desired, one if just a function is desired

            return client.AsAIAgent(
                instructions:
@"You are an agent inside of a tool that generates python scripts.  The user prompt will be the desired name of the python script as well as a detailed description of what the script needs to do.

Please generate the contents of the script (script file will be created using your generated code).

If the instrutions are too vague or contradictory, please respond with: 'ERROR: reason(s) for error'.

When writing the script, please think about edge cases, potential bugs, infinite loops, etc.

Please only output the contents of the script that can be pasted directly into a python script (don't wrap in markdown), since your response will be used programatically",
                //tools: [],
                name: $"{nameof(PythonWriter)}_WriterAgent");
        }
        private static ChatClientAgent CreateAgent_Validator(IChatClient client)
        {
            string prompt =
@"You are an agent responsible for validating generated python scripts.

The user prompt will be name and requirements of the script, as well as the script that was generated.

The script will already have passed static code analysis before it gets to you.

Please examine the code for adherence to requirements, flaws in design, security holes, etc.  Think about possible values of each param, does the script account for odd values?  Does the script try to do more than what the requirements want?  Are there confusing pieces of code that could be simplified?

It's ok to return an empty list if the script has no issues";

            prompt = StronglyTypedPromptHelper<ValidatorResponse[]>.AppendToPrompt(prompt);

            return client.AsAIAgent(
                instructions: prompt,
                //tools: [],
                name: $"{nameof(PythonWriter)}_ValidatorAgent");
        }
        private static ChatClientAgent CreateAgent_RequirementRefiner(IChatClient client)
        {
            string prompt =
@"There have been a couple attempts at generating a python script, but the validation agent is having problems.

Please come up with improved requirements based on the original requirements and error reports.

Also, please combine the sets of error messages into a single distinct list.";

            prompt = StronglyTypedPromptHelper<PromptRefine>.AppendToPrompt(prompt);

            return client.AsAIAgent(
                instructions: prompt,
                //tools: [],
                name: $"{nameof(PythonWriter)}_RequirementRefiner");
        }
        private static ChatClientAgent CreateAgent_Documentation(IChatClient client)
        {
            string prompt =
@"You are an agent that needs to document a generated python script.

You will get several items in the input prompt:
- filename of the python script
- original requirements
- possibly refined requirements based on previous attempts
- possibly error list from previous attempts
- the generated python

Your output will be markdown that will be directly saved into a .md file.

The items passed to you will also be added to the end of the markdown that you respond with, so there's no need for you to include any of that.

Please don't add any conversation, this will be used as documentation for the script.

Sections for you to respond with:
- Title and brief description
- Detailed description of input params
- Detailed description of either return value or expected console output
- Use cases of the script
- Any limitations or warnings about how the script may be misused (possible wrong assumptions that could be made about the script)";

            return client.AsAIAgent(
                instructions: prompt,
                //tools: [],
                name: $"{nameof(PythonWriter)}_DocumentationAgent");
        }

        #endregion

        #region class: ValidatorResponse

        public class ValidatorResponse
        {
            public IssueSeverity Severity { get; set; }
            public string Description { get; set; }
        }

        public enum IssueSeverity
        {
            //Info,
            Warning,
            Error,
        }

        #endregion
        #region class: PromptRefine

        public class PromptRefine
        {
            public string NewPrompt { get; set; }
            public ValidatorResponse[] SummarizedErrorList { get; set; }
        }

        #endregion
    }

    #region record: PythonWriter_Response

    public record PythonWriter_Response
    {
        public required bool IsSuccess { get; init; }

        public string? ErrorMessage { get; init; }

        public string? Python_FullFilenamePath { get; init; }
        public string? MarkdownDocumentation_FullFilenamePath { get; init; }

        public static PythonWriter_Response BuildSuccess(string python_filename, string documentation_filename)
        {
            return new PythonWriter_Response
            {
                IsSuccess = true,
                Python_FullFilenamePath = python_filename,
                MarkdownDocumentation_FullFilenamePath = documentation_filename,
            };
        }
        public static PythonWriter_Response BuildError(string error_message)
        {
            return new PythonWriter_Response
            {
                IsSuccess = false,
                ErrorMessage = error_message,
            };
        }
    }

    #endregion
}
