using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System.Text;

namespace MAFTesters_Core
{
    public static class WorkflowEventListener
    {
        // examples seem to like to use workflow.stream instead of run
        // but this function is written like a run, so call it run

        // run comes from some overload of InProcessExecution.StreamAsync(workflow, ...)

        // TODO: add a param that can accept events as they happen
        public async static Task<WorkflowEventListener_Response> ListenToStream(StreamingRun run)
        {
            // Ask the run to send accumulated ChatMessage objects
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            // Receive events, building up buckets
            var messages_by_id = new Dictionary<string, string>();

            var log = new StringBuilder();
            var building_building = new List<ChatMessage>();
            var building_final = new List<ChatMessage>();

            await foreach (WorkflowEvent evt in run.WatchStreamAsync())
            {
                log.AppendLine(evt.ToString());

                ProcessEvent(evt, building_building, building_final, messages_by_id);
            }

            // Make sure both arrays are populated, either distinct or copies
            ChatMessage[] building = [];
            ChatMessage[] final = [];

            if (building_building.Count > 0 && building_final.Count > 0)
            {
                building = building_building.ToArray();
                final = building_final.ToArray();
            }
            else if (building_final.Count > 0)
            {
                building = building_final.ToArray();
                final = building_final.ToArray();
            }
            else if (building_building.Count > 0)
            {
                building = building_building.ToArray();
                final = building_building.ToArray();
            }

            return new WorkflowEventListener_Response
            {
                Messages_Building = building,
                Messages_Final = final,
                LogReport = log.ToString(),
            };
        }

        private static void ProcessEvent(WorkflowEvent evt, List<ChatMessage> building, List<ChatMessage> final, Dictionary<string, string> messages_by_id)
        {
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

            // ----------------- BUILDING -----------------

            // These two pair together.  build up a string when ExecutorCompletedEvent, then commit it as a chatmessage when
            // AgentRunUpdateEvent (using a dictionary by ExecutorId if they are running in parallel)
            if (evt is AgentRunUpdateEvent updateEvent)
            {
                string key = updateEvent.ExecutorId ?? "";
                string data = updateEvent.Data?.ToString() ?? "";

                if (messages_by_id.TryGetValue(key, out string existing))
                    messages_by_id[key] = existing + data;
                else
                    messages_by_id.Add(key, data);
            }
            else if (evt is ExecutorCompletedEvent completedEvent)
            {
                string key = completedEvent.ExecutorId ?? "";

                if (messages_by_id.TryGetValue(key, out string existing))
                {
                    // The value for this key is complete.  Add to return and remove the building value from dictionary
                    building.Add(CreateChatMessage(key, existing));
                    messages_by_id.Remove(key);
                }
                else
                {
                    string text = completedEvent.Data?.ToString();

                    if (text != null)
                        building.Add(CreateChatMessage(key, text));
                }
            }

            // ----------------- FINAL -----------------

            else if (evt is WorkflowOutputEvent output)
            {
                if (output.Is<List<ChatMessage>>())
                {
                    var msg_list = output.As<List<ChatMessage>>();     // it needs to be list, it won't cast directly to array
                    final.AddRange(msg_list ?? []);
                }
                else if (output.Is<string>())
                {
                    string? msg = output.As<string>();

                    var message = CreateChatMessage(output.SourceId, msg ?? "");
                    final.Add(message);
                }
                else
                {
                    string raw = output.Data?.ToString() ?? "UNKNOWN DATA TYPE";
                    final.Add(CreateChatMessage(output.SourceId, raw));
                }
            }
        }

        private static ChatMessage CreateChatMessage(string messageID, string text, ChatRole? role = null)
        {
            return new ChatMessage
            {
                MessageId = messageID,
                Contents = [new TextContent(text)],
                CreatedAt = DateTimeOffset.UtcNow,
                Role = role ?? ChatRole.Tool,

                // TODO: fill out the rest of the props
                //AuthorName
            };
        }

        private static void Examples()
        {

            // all of these seem to focus on the same three:

            //---RunAsync-- -
            //ExecutorCompletedEvent
            //AgentRunUpdateEvent
            //WorkflowOutputEvent


            //---WatchStreamAsync-- -
            //await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            //ExecutorCompletedEvent
            //AgentRunUpdateEvent
            //WorkflowOutputEvent




            /*

            // ------------------ ExecutorsAndEdges ------------------

            await using Run run = await InProcessExecution.RunAsync(workflow, text);

            var full_log = new StringBuilder();
            var end_results = new StringBuilder();
            var end_results2 = new StringBuilder();

            foreach (WorkflowEvent evt in run.NewEvents)
            {
                full_log.AppendLine(evt.ToString());

                if (evt is ExecutorCompletedEvent executorComplete)
                    end_results.AppendLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");

                if (evt is WorkflowOutputEvent outputEvent)
                    end_results2.AppendLine($"{outputEvent.Data}");
            }



            // ------------------ AgentsInWorkflows (Run) ------------------

            // Execute the workflow
            await using Run run = await InProcessExecution.RunAsync(workflow, new ChatMessage(ChatRole.User, text));

            var full_log = new StringBuilder();
            var end_results = new StringBuilder();
            var end_results2 = new StringBuilder();

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



            // ------------------ AgentsInWorkflows (Stream) ------------------

            // Must send the turn token to trigger the agents.
            // The agents are wrapped as executors. When they receive messages,
            // they will cache the messages and only start processing when they receive a TurnToken.
            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

            var full_log = new StringBuilder();
            var end_results = new StringBuilder();
            var end_results2 = new StringBuilder();

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



            // ------------------ AgentWorkflowPatterns ------------------


            string? lastExecutorId = null;

            await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

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


            */
        }
    }

    public record WorkflowEventListener_Response
    {
        // Sometimes one set of events fire with data, sometimes another.  Both of these will be populated if anything returns data.  Sometimes
        // they will be copies of each other, sometimes different
        //
        // If you're only going to look at one of these lists, you'll probably want to use final
        public required ChatMessage[] Messages_Final { get; init; }
        public required ChatMessage[] Messages_Building { get; init; }

        public required string LogReport { get; init; }

        public ResponseMessage GetSingleMessage(string agent_name, bool error_if_emptymessage = true)
        {
            // there should just be one chat message
            if (Messages_Final == null || Messages_Final.Length == 1)
                return ResponseMessage.BuildError($"ERROR: didn't get a response from {agent_name} agent");

            else if (Messages_Final.Length > 1)
                return ResponseMessage.BuildError($"ERROR: multiple responses were returned from the {agent_name} agent: {Messages_Final.Length}");

            else if (error_if_emptymessage && string.IsNullOrWhiteSpace(Messages_Final[0].Text))
                return ResponseMessage.BuildError($"ERROR: got a blank response from {agent_name} agent");

            return ResponseMessage.BuildSuccess(Messages_Final[0].Text ?? "");
        }
    }
}
