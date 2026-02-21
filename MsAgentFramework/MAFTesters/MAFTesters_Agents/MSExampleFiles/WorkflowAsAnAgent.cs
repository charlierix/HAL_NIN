using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_Agents.MSExampleFiles
{
    // https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/GettingStarted/Workflows/Agents/WorkflowAsAnAgent

    /// <summary>
    /// This sample introduces the concepts workflows as agents, where a workflow can be
    /// treated as an <see cref="AIAgent"/>. This allows you to interact with a workflow
    /// as if it were a single agent.
    ///
    /// In this example, we create a workflow that uses two language agents to process
    /// input concurrently, one that responds in French and another that responds in English.
    ///
    /// You will interact with the workflow in an interactive loop, sending messages and receiving
    /// streaming responses from the workflow as if it were an agent who responds in both languages.
    /// </summary>
    /// <remarks>
    /// Pre-requisites:
    /// - Foundational samples should be completed first.
    /// - This sample uses concurrent processing.
    /// - An Azure OpenAI endpoint and deployment name.
    /// </remarks>
    public static class WorkflowAsAnAgent
    {
        // this is using session function that doesn't exist

        /*

        public static async Task RunAsync(string ollama_url, string ollama_model, string text)
        {
            var client = new OllamaApiClient(ollama_url, ollama_model);



            // Create the workflow and turn it into an agent
            var workflow = BuildWorkflow(client);
            var agent = workflow.AsAgent("workflow-agent", "Workflow Agent");
            var session = await agent.GetNewSessionAsync();

            // Start an interactive loop to interact with the workflow as if it were an agent
            while (true)
            {
                Console.WriteLine();
                Console.Write("User (or 'exit' to quit): ");
                string? input = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(input) || input.Equals("exit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                await ProcessInputAsync(agent, session, input);




            }
        }


        // Helper method to process user input and display streaming responses. To display
        // multiple interleaved responses correctly, we buffer updates by message ID and
        // re-render all messages on each update.
        static async Task ProcessInputAsync(AIAgent agent, AgentSession? session, string input)
        {
            Dictionary<string, List<AgentResponseUpdate>> buffer = [];
            await foreach (AgentResponseUpdate update in agent.RunStreamingAsync(input, session))
            {
                if (update.MessageId is null || string.IsNullOrEmpty(update.Text))
                {
                    // skip updates that don't have a message ID or text
                    continue;
                }
                Console.Clear();

                if (!buffer.TryGetValue(update.MessageId, out List<AgentResponseUpdate>? value))
                {
                    value = [];
                    buffer[update.MessageId] = value;
                }
                value.Add(update);

                foreach (var (messageId, segments) in buffer)
                {
                    string combinedText = string.Concat(segments);
                    Console.WriteLine($"{segments[0].AuthorName}: {combinedText}");
                    Console.WriteLine();
                }
            }
        }



        /// <summary>
        /// Creates a workflow that uses two language agents to process input concurrently.
        /// </summary>
        /// <param name="chatClient">The chat client to use for the agents</param>
        /// <returns>A workflow that processes input using two language agents</returns>
        internal static Workflow BuildWorkflow(IChatClient chatClient)
        {
            // Create executors
            var startExecutor = new ChatForwardingExecutor("Start");
            var aggregationExecutor = new ConcurrentAggregationExecutor();
            AIAgent frenchAgent = GetLanguageAgent("French", chatClient);
            AIAgent englishAgent = GetLanguageAgent("English", chatClient);

            // Build the workflow by adding executors and connecting them
            return new WorkflowBuilder(startExecutor)
                .AddFanOutEdge(startExecutor, [frenchAgent, englishAgent])
                .AddFanInEdge([frenchAgent, englishAgent], aggregationExecutor)
                .WithOutputFrom(aggregationExecutor)
                .Build();
        }

        /// <summary>
        /// Creates a language agent for the specified target language.
        /// </summary>
        /// <param name="targetLanguage">The target language for translation</param>
        /// <param name="chatClient">The chat client to use for the agent</param>
        /// <returns>A ChatClientAgent configured for the specified language</returns>
        private static ChatClientAgent GetLanguageAgent(string targetLanguage, IChatClient chatClient) =>
            new(chatClient, instructions: $"You're a helpful assistant who always responds in {targetLanguage}.", name: $"{targetLanguage}Agent");

        /// <summary>
        /// Executor that aggregates the results from the concurrent agents.
        /// </summary>
        private sealed class ConcurrentAggregationExecutor() :
            Executor<List<ChatMessage>>("ConcurrentAggregationExecutor"), IResettableExecutor
        {
            private readonly List<ChatMessage> _messages = [];

            /// <summary>
            /// Handles incoming messages from the agents and aggregates their responses.
            /// </summary>
            /// <param name="message">The messages from the agent</param>
            /// <param name="context">Workflow context for accessing workflow services and adding events</param>
            /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
            /// The default is <see cref="CancellationToken.None"/>.</param>
            public override async ValueTask HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                this._messages.AddRange(message);

                if (this._messages.Count == 2)
                {
                    var formattedMessages = string.Join(Environment.NewLine, this._messages.Select(m => $"{m.Text}"));
                    await context.YieldOutputAsync(formattedMessages, cancellationToken);
                }
            }

            /// <inheritdoc/>
            public ValueTask ResetAsync()
            {
                this._messages.Clear();
                return default;
            }
        }

        */
    }
}
