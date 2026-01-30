using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Text;

namespace MAFTesters_Core.MSExampleFiles
{
    // https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/GettingStarted/Workflows/_Foundational/03_AgentsInWorkflows

    /// <summary>
    /// This sample introduces the use of AI agents as executors within a workflow.
    ///
    /// Instead of simple text processing executors, this workflow uses three translation agents:
    /// 1. French Agent - translates input text to French
    /// 2. Spanish Agent - translates French text to Spanish
    /// 3. English Agent - translates Spanish text back to English
    ///
    /// The agents are connected sequentially, creating a translation chain that demonstrates
    /// how AI-powered components can be seamlessly integrated into workflow pipelines.
    /// </summary>
    /// <remarks>
    /// Pre-requisites:
    /// - An Azure OpenAI chat completion deployment must be configured.
    /// </remarks>
    public static class AgentsInWorkflows
    {
        // I noticed this was slightly different from ExecutorsAndEdges example and realized it's stream vs run.  So copied the
        // function and made slight tweaks for the run
        public static async Task<string> RunAsync_Stream(string ollama_url, string ollama_model, string text)
        {
            //// Set up the Azure OpenAI client
            //var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
            //var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
            //var chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential()).GetChatClient(deploymentName).AsIChatClient();

            // Using ollama
            var client = new OllamaApiClient(ollama_url, ollama_model);

            // Create agents
            AIAgent frenchAgent = GetTranslationAgent("French", client);
            AIAgent spanishAgent = GetTranslationAgent("Spanish", client);
            AIAgent englishAgent = GetTranslationAgent("English", client);

            // Build the workflow by adding executors and connecting them
            var workflow = new WorkflowBuilder(frenchAgent)
                .AddEdge(frenchAgent, spanishAgent)
                .AddEdge(spanishAgent, englishAgent)
                .Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, text));

            // Must send the turn token to trigger the agents.
            // The agents are wrapped as executors. When they receive messages,
            // they will cache the messages and only start processing when they receive a TurnToken.
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            var full_log = new StringBuilder();
            var end_results = new StringBuilder();
            var end_results2 = new StringBuilder();

            /* https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/core-concepts/events?pivots=programming-language-csharp

            // Workflow lifecycle events
            WorkflowStartedEvent     // Workflow execution begins
            WorkflowOutputEvent      // Workflow outputs data
            WorkflowErrorEvent       // Workflow encounters an error
            WorkflowWarningEvent     // Workflow encountered a warning

            // Executor events
            ExecutorInvokedEvent     // Executor starts processing
            ExecutorCompletedEvent   // Executor finishes processing
            ExecutorFailedEvent      // Executor encounters an error
            AgentResponseEvent       // An agent run produces output
            AgentResponseUpdateEvent // An agent run produces a streaming update

            // Superstep events
            SuperStepStartedEvent    // Superstep begins
            SuperStepCompletedEvent  // Superstep completes

            // Request events
            RequestInfoEvent         // A request is issued

            */

            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                full_log.AppendLine(evt.ToString());

                if (evt is ExecutorCompletedEvent completedEvent)
                    end_results.AppendLine();

                //if (evt is AgentResponseUpdateEvent executorComplete)     // this type doesn't exist
                else if (evt is AgentRunUpdateEvent updateEvent)
                    end_results.Append($"{updateEvent.Data}");

                else if (evt is WorkflowOutputEvent outputEvent)        // this never fires (maybe because this isn't using .WithOutputFrom in the workflow builder?)
                    end_results2.AppendLine($"{outputEvent.Data}");
            }

            var retVal = new StringBuilder();
            retVal.AppendLine("Chains three agents together in a workflow:");
            retVal.AppendLine("agent_to_french");
            retVal.AppendLine("Edge(agent_to_french, agent_to_spanish)");
            retVal.AppendLine("Edge(agent_to_spanish, agent_to_english)");
            retVal.AppendLine();
            retVal.AppendLine("--- RESULTS ---");
            retVal.AppendLine(end_results.ToString());
            retVal.AppendLine();
            retVal.AppendLine("--- RESULTS 2 ---");
            retVal.AppendLine(end_results2.ToString());
            retVal.AppendLine();
            retVal.AppendLine("--- FULL LOG ---");
            retVal.AppendLine(full_log.ToString());

            return retVal.ToString();
        }
        public static async Task<string> RunAsync_Run(string ollama_url, string ollama_model, string text)
        {
            //// Set up the Azure OpenAI client
            //var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
            //var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
            //var chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential()).GetChatClient(deploymentName).AsIChatClient();

            // Using ollama
            var client = new OllamaApiClient(ollama_url, ollama_model);

            // Create agents
            AIAgent frenchAgent = GetTranslationAgent("French", client);
            AIAgent spanishAgent = GetTranslationAgent("Spanish", client);
            AIAgent englishAgent = GetTranslationAgent("English", client);

            // Build the workflow by adding executors and connecting them
            var workflow = new WorkflowBuilder(frenchAgent)
                .AddEdge(frenchAgent, spanishAgent)
                .AddEdge(spanishAgent, englishAgent)
                .Build();

            // Execute the workflow
            await using Run run = await InProcessExecution.RunAsync(workflow, new ChatMessage(ChatRole.User, text));

            var full_log = new StringBuilder();
            var end_results = new StringBuilder();
            var end_results2 = new StringBuilder();

            /* https://learn.microsoft.com/en-us/agent-framework/user-guide/workflows/core-concepts/events?pivots=programming-language-csharp

            // Workflow lifecycle events
            WorkflowStartedEvent     // Workflow execution begins
            WorkflowOutputEvent      // Workflow outputs data
            WorkflowErrorEvent       // Workflow encounters an error
            WorkflowWarningEvent     // Workflow encountered a warning

            // Executor events
            ExecutorInvokedEvent     // Executor starts processing
            ExecutorCompletedEvent   // Executor finishes processing
            ExecutorFailedEvent      // Executor encounters an error
            AgentResponseEvent       // An agent run produces output
            AgentResponseUpdateEvent // An agent run produces a streaming update

            // Superstep events
            SuperStepStartedEvent    // Superstep begins
            SuperStepCompletedEvent  // Superstep completes

            // Request events
            RequestInfoEvent         // A request is issued

            */

            foreach (WorkflowEvent evt in run.NewEvents)
            {
                full_log.AppendLine(evt.ToString());

                if (evt is ExecutorCompletedEvent completedEvent)
                    end_results.AppendLine();

                //if (evt is AgentResponseUpdateEvent executorComplete)     // this type doesn't exist
                else if (evt is AgentRunUpdateEvent updateEvent)
                    end_results.Append($"{updateEvent.Data}");

                else if (evt is WorkflowOutputEvent outputEvent)        // this never fires (maybe because this isn't using .WithOutputFrom in the workflow builder?)
                    end_results2.AppendLine($"{outputEvent.Data}");
            }

            var retVal = new StringBuilder();
            retVal.AppendLine("Chains three agents together in a workflow:");
            retVal.AppendLine("agent_to_french");
            retVal.AppendLine("Edge(agent_to_french, agent_to_spanish)");
            retVal.AppendLine("Edge(agent_to_spanish, agent_to_english)");
            retVal.AppendLine();
            retVal.AppendLine("--- RESULTS ---");
            retVal.AppendLine(end_results.ToString());
            retVal.AppendLine();
            retVal.AppendLine("--- RESULTS 2 ---");
            retVal.AppendLine(end_results2.ToString());
            retVal.AppendLine();
            retVal.AppendLine("--- FULL LOG ---");
            retVal.AppendLine(full_log.ToString());

            return retVal.ToString();
        }

        public static async Task<WorkflowEventListener_Response> Run2Async_Stream(string ollama_url, string ollama_model, string text)
        {
            //// Set up the Azure OpenAI client
            //var endpoint = Environment.GetEnvironmentVariable("AZURE_OPENAI_ENDPOINT") ?? throw new InvalidOperationException("AZURE_OPENAI_ENDPOINT is not set.");
            //var deploymentName = Environment.GetEnvironmentVariable("AZURE_OPENAI_DEPLOYMENT_NAME") ?? "gpt-4o-mini";
            //var chatClient = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential()).GetChatClient(deploymentName).AsIChatClient();

            // Using ollama
            var client = new OllamaApiClient(ollama_url, ollama_model);

            // Create agents
            AIAgent frenchAgent = GetTranslationAgent("French", client);
            AIAgent spanishAgent = GetTranslationAgent("Spanish", client);
            AIAgent englishAgent = GetTranslationAgent("English", client);

            // Build the workflow by adding executors and connecting them
            var workflow = new WorkflowBuilder(frenchAgent)
                .AddEdge(frenchAgent, spanishAgent)
                .AddEdge(spanishAgent, englishAgent)
                .Build();

            // Execute the workflow
            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, new ChatMessage(ChatRole.User, text));

            return await WorkflowEventListener.ListenToStream(run);
        }

        /// <summary>
        /// Creates a translation agent for the specified target language.
        /// </summary>
        /// <param name="targetLanguage">The target language for translation</param>
        /// <param name="chatClient">The chat client to use for the agent</param>
        /// <returns>A ChatClientAgent configured for the specified language</returns>
        private static ChatClientAgent GetTranslationAgent(string targetLanguage, IChatClient chatClient) =>
            new(chatClient, name: $"To {targetLanguage}", instructions: $"You are a translation assistant that translates the provided text to {targetLanguage}.  When the user prompt comes in, please reply with the translation in {targetLanguage}.  Your response will be parsed programmatically, so please don't add any extra text");
    }
}
