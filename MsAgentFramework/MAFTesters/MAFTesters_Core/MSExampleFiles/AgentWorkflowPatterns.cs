using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using OllamaSharp;
using System.Text;
using System.Text.Json;

namespace MAFTesters_Core.MSExampleFiles
{
    // https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/GettingStarted/Workflows/_Foundational/04_AgentWorkflowPatterns

    // Demonstrates different ways of chaining agents together.  This tester creates a few agents, each translates to the language they are set up for
    public static class AgentWorkflowPatterns
    {
        public enum WorkflowType
        {
            sequential,
            concurrent,
            handoffs,
            groupchat,
        }

        public async static Task<string> RunAsync(ClientSettings clientSettings, WorkflowType workflow_type, string text)
        {
            var client = clientSettings.CreateClient();

            (string log, ChatMessage[] responses) retVal = ("", []);

            switch (workflow_type)
            {
                case WorkflowType.sequential:
                    retVal = await RunWorkflowAsync(
                       AgentWorkflowBuilder.BuildSequential(new string[] { "French", "Spanish", "English" }.Select(o => GetTranslationAgent(o, client))),
                       [new(ChatRole.User, text)]);
                    break;

                case WorkflowType.concurrent:
                    retVal = await RunWorkflowAsync(
                        AgentWorkflowBuilder.BuildConcurrent(new string[] { "French", "Spanish", "English" }.Select(o => GetTranslationAgent(o, client))),
                        [new(ChatRole.User, text)]);
                    break;

                case WorkflowType.handoffs:
                    ChatClientAgent historyTutor = new(client,
                        "You provide assistance with historical queries. Explain important events and context clearly. Only respond about history.",
                        "history_tutor",
                        "Specialist agent for historical questions");

                    ChatClientAgent mathTutor = new(client,
                        "You provide help with math problems. Explain your reasoning at each step and include examples. Only respond about math.",
                        "math_tutor",
                        "Specialist agent for math questions");

                    ChatClientAgent triageAgent = new(client,
                        "You determine which agent to use based on the user's homework question. ALWAYS handoff to another agent.",
                        "triage_agent",
                        "Routes messages to the appropriate specialist agent");

                    var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
                        .WithHandoffs(triageAgent, [mathTutor, historyTutor])
                        .WithHandoffs([mathTutor, historyTutor], triageAgent)
                        .Build();

                    List<ChatMessage> messages = [];
                    //while (true)        // infinite loop, not sure what they wanted to do here.  looking at history, it's always been an infinite loop
                    for (int i = 0; i < 3; i++)
                    {
                        messages.Add(new(ChatRole.User, text));
                        //messages.AddRange(await RunWorkflowAsync(workflow, messages));
                        var temp = await RunWorkflowAsync(workflow, messages);
                        messages.AddRange(temp.responses);

                        if (i == 0)
                            retVal = temp;
                        else
                            retVal = ($"{retVal.log}{Environment.NewLine}-------- iteration {i} --------{Environment.NewLine}{temp.log}", retVal.responses.Concat(temp.responses).ToArray());
                    }
                    break;

                case WorkflowType.groupchat:
                    retVal = await RunWorkflowAsync(
                        AgentWorkflowBuilder.CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 5 })
                            .AddParticipants(new string[] { "French", "Spanish", "English" }.Select(o => GetTranslationAgent(o, client)))
                            .Build(),
                        [new(ChatRole.User, text)]);
                    break;

                default:
                    throw new InvalidOperationException("Invalid workflow type.");
            }

            return retVal.log;
        }
        public async static Task<WorkflowEventListener_Response> Run2Async(ClientSettings clientSettings, WorkflowType workflow_type, string text)
        {
            var client = clientSettings.CreateClient();

            WorkflowEventListener_Response retVal = null;

            switch (workflow_type)
            {
                case WorkflowType.sequential:
                    await using (StreamingRun run1 = await InProcessExecution.StreamAsync(
                        AgentWorkflowBuilder.BuildSequential(new string[] { "French", "Spanish", "English" }.Select(o => GetTranslationAgent(o, client))),
                        new List<ChatMessage>([new(ChatRole.User, text)])))
                    {
                        retVal = await WorkflowEventListener.ListenToStream(run1);
                    }
                    break;

                case WorkflowType.concurrent:
                    await using (StreamingRun run2 = await InProcessExecution.StreamAsync(
                        AgentWorkflowBuilder.BuildConcurrent(new string[] { "French", "Spanish", "English" }.Select(o => GetTranslationAgent(o, client))),
                        new List<ChatMessage>([new(ChatRole.User, text)])))
                    {
                        retVal = await WorkflowEventListener.ListenToStream(run2);
                    }
                    break;

                case WorkflowType.handoffs:
                    ChatClientAgent historyTutor = new(client,
                        "You provide assistance with historical queries. Explain important events and context clearly. Only respond about history.",
                        "history_tutor",
                        "Specialist agent for historical questions");

                    ChatClientAgent mathTutor = new(client,
                        "You provide help with math problems. Explain your reasoning at each step and include examples. Only respond about math.",
                        "math_tutor",
                        "Specialist agent for math questions");

                    ChatClientAgent triageAgent = new(client,
                        "You determine which agent to use based on the user's homework question. ALWAYS handoff to another agent.",
                        "triage_agent",
                        "Routes messages to the appropriate specialist agent");

                    var workflow = AgentWorkflowBuilder.CreateHandoffBuilderWith(triageAgent)
                        .WithHandoffs(triageAgent, [mathTutor, historyTutor])
                        .WithHandoffs([mathTutor, historyTutor], triageAgent)
                        .Build();

                    List<ChatMessage> messages = [];
                    //while (true)        // infinite loop, not sure what they wanted to do here.  looking at history, it's always been an infinite loop
                    for (int i = 0; i < 3; i++)
                    {
                        messages.Add(new(ChatRole.User, text));

                        await using StreamingRun run3 = await InProcessExecution.StreamAsync(workflow, messages);
                        var temp = await WorkflowEventListener.ListenToStream(run3);

                        messages.AddRange(temp.Messages_Final ?? []);

                        if (i == 0)
                            retVal = temp;
                        else
                            retVal = retVal with
                            {
                                LogReport = $"{retVal.LogReport}{Environment.NewLine}-------- iteration {i} --------{Environment.NewLine}{temp.LogReport}",
                                Messages_Building = retVal.Messages_Building.Concat(temp.Messages_Building).ToArray(),
                                Messages_Final = retVal.Messages_Final.Concat(temp.Messages_Final).ToArray(),
                            };
                    }
                    break;

                case WorkflowType.groupchat:
                    await using (StreamingRun run4 = await InProcessExecution.StreamAsync(
                        AgentWorkflowBuilder.CreateGroupChatBuilderWith(agents => new RoundRobinGroupChatManager(agents) { MaximumIterationCount = 5 })
                            .AddParticipants(new string[] { "French", "Spanish", "English" }.Select(o => GetTranslationAgent(o, client)))
                            .Build(),
                        new List<ChatMessage>([new(ChatRole.User, text)])))
                    {
                        retVal = await WorkflowEventListener.ListenToStream(run4);
                    }
                    break;

                default:
                    throw new InvalidOperationException("Invalid workflow type.");
            }

            return retVal;
        }

        private static async Task<(string log, ChatMessage[] responses)> RunWorkflowAsync(Workflow workflow, List<ChatMessage> messages)
        {
            string? lastExecutorId = null;

            await using StreamingRun run = await InProcessExecution.StreamAsync(workflow, messages);
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

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

            var full_log = new StringBuilder();
            var end_results = new StringBuilder();
            ChatMessage[] output_msgs = null;

            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                full_log.AppendLine(evt.ToString());

                //if (evt is ExecutorCompletedEvent completedEvent)
                //    end_results.AppendLine();

                //if (evt is AgentResponseUpdateEvent e)        // this doesn't exist
                if (evt is AgentRunUpdateEvent e)
                {
                    if (e.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = e.ExecutorId;
                        end_results.AppendLine();
                        //Console.WriteLine(e.ExecutorId);
                    }

                    end_results.Append(e.Update.Text);

                    if (e.Update.Contents.OfType<FunctionCallContent>().FirstOrDefault() is FunctionCallContent call)
                    {
                        full_log.AppendLine();
                        full_log.AppendLine($"  [Calling function '{call.Name}' with arguments: {JsonSerializer.Serialize(call.Arguments)}]");
                    }
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    var test = output.As<List<ChatMessage>>()!;     // it won't cast directly to array
                    output_msgs = test?.ToArray() ?? [];
                    //break;        // original tutorial returned here, so maybe final code should break?  during testing, there were just a couple iterations, but none of the if statements hit, just full_log
                }
            }

            var retVal = new StringBuilder();


            // NOTE: description of what this is doing is added in the code that calls this class (the button click)

            retVal.AppendLine("--- RESULTS ---");
            retVal.AppendLine(end_results.ToString());
            retVal.AppendLine();
            retVal.AppendLine("--- OUTPUT MESSAGES ---");
            retVal.AppendLine(output_msgs?.ToString() ?? "null");
            retVal.AppendLine();
            retVal.AppendLine("--- FULL LOG ---");
            retVal.AppendLine(full_log.ToString());

            return (retVal.ToString(), output_msgs);
        }

        /// <summary>Creates a translation agent for the specified target language.</summary>
        private static ChatClientAgent GetTranslationAgent(string targetLanguage, IChatClient chatClient)
        {
            string instructions = $"You are a translation assistant who only responds in {targetLanguage}. Respond to any input by outputting the name of the input language (in {targetLanguage}) and then translating the input to {targetLanguage}.  For example if target language is spanish and the input is 'hello', then respond with 'espanol: hola'.  Your response will be parsed programmatically, so please don't add any extra text";

            return new(chatClient, name: $"Translator_{targetLanguage}", instructions: instructions);
        }
    }
}
