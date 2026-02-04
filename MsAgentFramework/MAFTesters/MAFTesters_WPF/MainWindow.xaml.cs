using MAFTesters_Core;
using MAFTesters_Core.MSExampleFiles;
using MAFTesters_Core.Tools;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using static MAFTesters_Core.MSExampleFiles.AgentWorkflowPatterns;

namespace MAFTesters_WPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                cboModel.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // this is from the first example on the page.  there are other good ones on that page
        // https://medium.com/@venya-brodetskiy/getting-started-with-microsoft-agent-framework-61a1112220f8
        private async void WeatherExample_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }


                // Convert the function to a tool
                var weatherFunction = AIFunctionFactory.Create(WeatherTool.GetWeather);

                var client = clientSettings.CreateClient();

                // Create the agent
                // NOTE: it doesn't look like tools can be dynamically added.  if a new tool gets created, a new agent instance would be needed
                // NOTE: added the line to not make up an answer because it would just return a random temperature if the tool returned a bad response
                var agent = client.CreateAIAgent(
                        instructions: "say 'just a second' before answering question.  if there is an error calling the tool, please don't try to make up a response",
                        tools: [weatherFunction],
                        name: "myagent");



                /*
                Thread Management: GetNewThread() creates a conversation context that maintains message history. Each thread is isolated, allowing you to manage multiple conversations independently.
                */

                // Create a thread for conversation
                var thread = agent.GetNewThread();


                // Run the agent with streaming
                // NOTE: it wan't calling the weather tool unless the prompt called for temperature
                // NOTE: it asked for a location when one wasn't provided
                var streamingResponse = agent.RunStreamingAsync(txtPrompt.Text, thread);

                txtLog.Text = "";

                await foreach (var chunk in streamingResponse)
                {
                    txtLog.Text += chunk;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PythonWriter_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sessionArgs = GetSessionArgs();

                var client = clientSettings.CreateClient();

                // Convert the function into a tool
                var pythonFunction = AIFunctionFactory.Create(
                    typeof(PythonWriter).GetMethod(nameof(PythonWriter.GeneratePythonScript)),      // reflection pointing to the function that will get invoked
                    (args) =>       // whenever the tool needs to be used, this delegate creates an instance, giving extra session info to the tool
                    {
                        var agents = PythonWriter.CreateAgents(client);
                        return new PythonWriter(sessionArgs.PythonFolder, agents.writer, agents.validator, true);
                    });


                // let's see what the agents see
                string name = pythonFunction.Name;      // this is the function name: GeneratePythonScript
                string description = pythonFunction.Description;        // this is the contents of the description attribute over the function [Description(...)]
                var schema = pythonFunction.JsonSchema;
                var return_schema = pythonFunction.ReturnJsonSchema;


                // Invoke the tool directly to test it
                var tool_args = new AIFunctionArguments();
                tool_args.Add("desiredFilename", $"{DateTime.Now:yyyyMMdd HHmmss} writer test.py");
                tool_args.Add("requirements", "What is the 32nd number in the fibonacci sequence?");

                var result = pythonFunction.InvokeAsync(tool_args);

                var temp = result.Result;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PythonWriter2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var sessionArgs = GetSessionArgs();

                // Convert the function into a tool
                var pythonFunction = AIFunctionFactory.Create(
                    typeof(PythonWriter2).GetMethod(nameof(PythonWriter2.GeneratePythonScript)),      // reflection pointing to the function that will get invoked
                    (args) =>       // whenever the tool needs to be used, this delegate creates an instance, giving extra session info to the tool
                    {
                        return new PythonWriter2(sessionArgs.PythonFolder, true, clientSettings);
                    });

                // Invoke the tool directly to test it
                var tool_args = new AIFunctionArguments();
                tool_args.Add("desiredFilename", $"{DateTime.Now:yyyyMMdd HHmmss} writer test.py");
                tool_args.Add("requirements", "What is the 32nd number in the fibonacci sequence?");

                var result = pythonFunction.InvokeAsync(tool_args);

                var temp = result.Result;



            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecutorsAndEdges_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = txtPrompt.Text.Trim() == "" ?
                    "Hello, World!" :
                    txtPrompt.Text;

                string report = await ExecutorsAndEdges.RunAsync(text);

                txtLog.Text = report;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void AgentsInWorkflows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string text = txtPrompt.Text.Trim() == "" ?
                    "Hello, World!" :
                    txtPrompt.Text;

                //string report = await AgentsInWorkflows.RunAsync_Stream(settings.Value.url, settings.Value.model, text);
                string report = await AgentsInWorkflows.RunAsync_Run(clientSettings, text);

                txtLog.Text = report;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void AgentWorkflowPatterns_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string text = txtPrompt.Text.Trim() == "" ?
                    "Hello, World!" :
                    txtPrompt.Text;

                var results = new List<(WorkflowType flow_type, string report)>();

                foreach (var flow_type in Enum.GetValues<WorkflowType>())
                {
                    results.Add((flow_type, await AgentWorkflowPatterns.RunAsync(clientSettings, flow_type, text)));
                    //break;
                }

                var report = new StringBuilder();

                report.AppendLine("Demonstrates different ways of chaining agents together.  This tester creates a few agents, each translates to the language they are set up for");

                bool is_first = true;
                foreach (var result in results)
                {
                    if (!is_first)
                    {
                        report.AppendLine();
                        report.AppendLine();
                        report.AppendLine();
                    }
                    is_first = false;

                    report.AppendLine($"---------------------- {result.flow_type} ----------------------");

                    switch (result.flow_type)
                    {
                        case WorkflowType.sequential:
                            report.AppendLine("A pipeline of agents where the output of one agent is the input to the next");
                            break;

                        case WorkflowType.concurrent:
                            report.AppendLine("agents operate concurrently on the same input, aggregating their outputs into a single collection");
                            break;

                        case WorkflowType.handoffs:
                            report.AppendLine("Handoffs between agents are achieved by the current agent invoking an AITool provided to an agent");
                            report.AppendLine("through ChatClientAgentOptions.  The AIAgent must be capable of understanding those AgentRunOptions provided. If the");
                            report.AppendLine("agent ignores the tools or is otherwise unable to advertize them to the underlying provider, handoffs will not occur");
                            break;

                        case WorkflowType.groupchat:
                            report.AppendLine("Creates a GroupChatManager.  The manager is provided with a set of agents that will participate in the group chat");
                            report.AppendLine("");
                            report.AppendLine("Handoffs between agents are achieved by the current agent invoking an AITool provided to an agent through");
                            report.AppendLine("ChatClientAgentOptions.  The AIAgent must be capable of understanding those AgentRunOptions provided. If the agent");
                            report.AppendLine("ignores the tools or is otherwise unable to advertize them to the underlying provider, handoffs will not occur");
                            break;
                    }

                    report.AppendLine();

                    report.AppendLine(result.report);
                }

                txtLog.Text = report.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void SubWorkflows_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string text = txtPrompt.Text.Trim() == "" ?
                    "Hello, World!" :
                    txtPrompt.Text;

                var report = await SubWorkflows.RunAsync(text);

                //var description = new StringBuilder();
                //txtLog.Text = BuildReport(description.ToString(), report);

                txtLog.Text = report;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void ExecutorsAndEdges2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string text = txtPrompt.Text.Trim() == "" ?
                    "Hello, World!" :
                    txtPrompt.Text;

                var report = await ExecutorsAndEdges.Run2Async(text);

                string description = "Chains two workers (executors) together (along an edge).  Stored and run in a workflow";

                txtLog.Text = BuildReport(description, report);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void AgentsInWorkflows2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string text = txtPrompt.Text.Trim() == "" ?
                    "Hello, World!" :
                    txtPrompt.Text;

                var report = await AgentsInWorkflows.Run2Async_Stream(clientSettings, text);

                var description = new StringBuilder();
                description.AppendLine("Chains three agents together in a workflow:");
                description.AppendLine("agent_to_french");
                description.AppendLine("Edge(agent_to_french, agent_to_spanish)");
                description.AppendLine("Edge(agent_to_spanish, agent_to_english)");

                txtLog.Text = BuildReport(description.ToString(), report);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void AgentWorkflowPatterns2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string text = txtPrompt.Text.Trim() == "" ?
                    "Hello, World!" :
                    txtPrompt.Text;

                var results = new List<(WorkflowType flow_type, WorkflowEventListener_Response report)>();

                foreach (var flow_type in Enum.GetValues<WorkflowType>())
                    results.Add((flow_type, await AgentWorkflowPatterns.Run2Async(clientSettings, flow_type, text)));

                var report = new StringBuilder();

                report.AppendLine("Demonstrates different ways of chaining agents together.  This tester creates a few agents, each translates to the language they are set up for");

                foreach (var result in results)
                {
                    report.AppendLine();
                    report.AppendLine();
                    report.AppendLine();

                    report.AppendLine($"---------------------- {result.flow_type} ----------------------");

                    switch (result.flow_type)
                    {
                        case WorkflowType.sequential:
                            report.AppendLine("A pipeline of agents where the output of one agent is the input to the next");
                            break;

                        case WorkflowType.concurrent:
                            report.AppendLine("agents operate concurrently on the same input, aggregating their outputs into a single collection");
                            break;

                        case WorkflowType.handoffs:
                            report.AppendLine("Handoffs between agents are achieved by the current agent invoking an AITool provided to an agent");
                            report.AppendLine("through ChatClientAgentOptions.  The AIAgent must be capable of understanding those AgentRunOptions provided. If the");
                            report.AppendLine("agent ignores the tools or is otherwise unable to advertize them to the underlying provider, handoffs will not occur");
                            break;

                        case WorkflowType.groupchat:
                            report.AppendLine("Creates a GroupChatManager.  The manager is provided with a set of agents that will participate in the group chat");
                            report.AppendLine("");
                            report.AppendLine("Handoffs between agents are achieved by the current agent invoking an AITool provided to an agent through");
                            report.AppendLine("ChatClientAgentOptions.  The AIAgent must be capable of understanding those AgentRunOptions provided. If the agent");
                            report.AppendLine("ignores the tools or is otherwise unable to advertize them to the underlying provider, handoffs will not occur");
                            break;
                    }

                    report.AppendLine();

                    report.AppendLine(BuildReport("", result.report));
                }

                txtLog.Text = report.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void SubWorkflows2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string text = txtPrompt.Text.Trim() == "" ?
                    "Hello, World!" :
                    txtPrompt.Text;

                var report = await SubWorkflows.Run2Async(text);

                var description = new StringBuilder();
                description.AppendLine("This has a main workflow with edges that add a prefix, call a sub workflow, and a post process that adds a bunch of final text");
                description.AppendLine("The subworkflow has edges for uppercase, reverse, suffix text");

                txtLog.Text = BuildReport(description.ToString(), report);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void MixedWorkflowAgentsAndExecutors_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var clientSettings = GetOllamaValues();
                if (clientSettings == null)
                {
                    MessageBox.Show("Please select a model", Title, MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                //string text = txtPrompt.Text.Trim() == "" ?
                //    "Hello, World!" :
                //    txtPrompt.Text;

                string text = null;     // there's logic in the run to use its own prompt when null

                var results = await MixedWorkflowWithAgentsAndExecutors.RunAsync(clientSettings, text);

                var report = new StringBuilder();

                for (int i = 0; i < results.calls.Length; i++)
                {
                    string description = "";
                    if (i == 0)
                        description =
@"This demonstrates how to chain executors with agents

Standard executors take and return basic datatypes (like string), agents use ChatMessage

This tester sets up both with type transformers in between

The tester itself tells an agent to detect prompt injection with instruction on how to output";

                    description += $"{Environment.NewLine}{Environment.NewLine}prompt: {results.calls[i].prompt}{Environment.NewLine}{Environment.NewLine}";

                    report.AppendLine(BuildReport(description, results.calls[i].response));
                }

                report.AppendLine();
                report.AppendLine();
                report.AppendLine("************************************************");
                report.AppendLine("              Log of all calls");
                report.AppendLine("************************************************");
                report.AppendLine();
                report.AppendLine();
                report.AppendLine(results.log);

                txtLog.Text = report.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AsFunctionTool_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/GettingStarted/Agents/Agent_Step12_AsFunctionTool
                
                // this is the weather service demo
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void WorkflowAsAnAgent_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // using code that doesn't exist
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UnitTests_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new UnitTestWindow().Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void PromptInterview_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                new PromptInterviewWindow().Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), Title, MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ClientSettings? GetOllamaValues()
        {
            var model_item = cboModel.SelectedItem as ComboBoxItem;
            string model = model_item?.Content?.ToString();

            if (string.IsNullOrEmpty(txtOllamaURL.Text) || string.IsNullOrEmpty(model))
                return null;

            return new ClientSettings
            {
                Ollama_URL = txtOllamaURL.Text,
                Ollama_Model = model,
            };
        }

        private SessionArgs GetSessionArgs()
        {
            if (!Directory.Exists(txtSourceFolder.Text))
                throw new ApplicationException($"Source folder doesn't exist: {txtSourceFolder.Text}");

            if (!Directory.Exists(txtWorkingFolder.Text))
                throw new ApplicationException($"Folder doesn't exist: {txtWorkingFolder.Text}");

            return new SessionArgs
            {
                SourceFolder = txtSourceFolder.Text,
                WorkingFolder = txtWorkingFolder.Text,
                PythonFolder = Path.Combine(txtWorkingFolder.Text, "PythonSandbox"),
            };
        }

        private static string BuildReport(string description, WorkflowEventListener_Response response)
        {
            var retVal = new StringBuilder();

            retVal.AppendLine(description);
            retVal.AppendLine();
            retVal.AppendLine();

            retVal.AppendLine("------------ FINAL ------------");

            foreach (var message in response.Messages_Final)
                retVal.AppendLine($"{message.AuthorName} {message.MessageId} ({message.Role}): {message.Text}");

            retVal.AppendLine();
            retVal.AppendLine();
            retVal.AppendLine("------------ BUILDING ------------");

            foreach (var message in response.Messages_Building)
                retVal.AppendLine($"{message.AuthorName} {message.MessageId} ({message.Role}): {message.Text}");

            retVal.AppendLine();
            retVal.AppendLine();
            retVal.AppendLine("------------ LOG ------------");
            retVal.AppendLine();
            retVal.AppendLine(response.LogReport);

            return retVal.ToString();
        }
    }
}