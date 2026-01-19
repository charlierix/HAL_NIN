using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_Core.MSExampleFiles
{
    // https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/GettingStarted/Workflows/_Foundational/01_ExecutorsAndEdges

    /// <summary>
    /// This sample introduces the concepts of executors and edges in a workflow.
    ///
    /// Workflows are built from executors (processing units) connected by edges (data flow paths).
    /// In this example, we create a simple text processing pipeline that:
    /// 1. Takes input text and converts it to uppercase using an UppercaseExecutor
    /// 2. Takes the uppercase text and reverses it using a ReverseTextExecutor
    ///
    /// The executors are connected sequentially, so data flows from one to the next in order.
    /// For input "Hello, World!", the workflow produces "!DLROW ,OLLEH".
    /// </summary>
    public static class ExecutorsAndEdges
    {
        public static async Task<string> RunAsync()
        {
            // Create the executors
            Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
            var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

            ReverseTextExecutor reverse = new();

            // Build the workflow by connecting executors sequentially
            WorkflowBuilder builder = new(uppercase);
            builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
            var workflow = builder.Build();

            // Execute the workflow with input data
            await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");

            var full_log = new StringBuilder();
            var end_results = new StringBuilder();

            foreach (WorkflowEvent evt in run.NewEvents)
            {

                full_log.AppendLine(evt.ToString());


                if (evt is ExecutorCompletedEvent executorComplete)
                    end_results.AppendLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
            }

            var retVal = new StringBuilder();
            retVal.AppendLine("Chains two workers (executors) together (along an edge).  Stored and run in a workflow");
            retVal.AppendLine();
            retVal.AppendLine("--- RESULTS ---");
            retVal.AppendLine(end_results.ToString());
            retVal.AppendLine();
            retVal.AppendLine("--- FULL LOG ---");
            retVal.AppendLine(full_log.ToString());

            return retVal.ToString();
        }

        /// <summary>
        /// Second executor: reverses the input text and completes the workflow.
        /// </summary>
        internal sealed class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
        {
            /// <summary>
            /// Processes the input message by reversing the text.
            /// </summary>
            /// <param name="message">The input text to reverse</param>
            /// <param name="context">Workflow context for accessing workflow services and adding events</param>
            /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
            /// The default is <see cref="CancellationToken.None"/>.</param>
            /// <returns>The input text reversed</returns>
            public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                // Because we do not suppress it, the returned result will be yielded as an output from this executor.
                return ValueTask.FromResult(string.Concat(message.Reverse()));
            }
        }
    }
}
