using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_Core.MSExampleFiles
{
    // https://github.com/microsoft/agent-framework/tree/main/dotnet/samples/GettingStarted/Workflows/_Foundational/06_SubWorkflows

    /// <summary>
    /// This sample demonstrates how to compose workflows hierarchically by using
    /// a workflow as an executor within another workflow (sub-workflows).
    ///
    /// A sub-workflow is a workflow that is embedded as an executor within a parent workflow.
    /// This allows you to:
    /// 1. Encapsulate and reuse complex workflow logic as modular components
    /// 2. Build hierarchical workflow structures
    /// 3. Create composable, maintainable workflow architectures
    ///
    /// In this example, we create:
    /// - A text processing sub-workflow (uppercase → reverse → append suffix)
    /// - A parent workflow that adds a prefix, processes through the sub-workflow, and post-processes
    ///
    /// For input "hello", the workflow produces: "INPUT: [FINAL] OLLEH [PROCESSED] [END]"
    /// </summary>
    public static class SubWorkflows
    {
        public static async Task<string> RunAsync(string ollama_url, string ollama_model, string text)
        {
            var console = new StringBuilder();

            console.AppendLine();
            console.AppendLine("=== Sub-Workflow Demonstration ===");
            console.AppendLine();

            // Step 1: Build a simple text processing sub-workflow
            console.AppendLine("Building sub-workflow: Uppercase → Reverse → Append Suffix...");
            console.AppendLine();

            UppercaseExecutor uppercase = new();
            ReverseExecutor reverse = new();
            AppendSuffixExecutor append = new(" [PROCESSED]");

            var subWorkflow = new WorkflowBuilder(uppercase)
                .AddEdge(uppercase, reverse)
                .AddEdge(reverse, append)
                .WithOutputFrom(append)
                .Build();

            // Step 2: Configure the sub-workflow as an executor for use in the parent workflow
            ExecutorBinding subWorkflowExecutor = subWorkflow.BindAsExecutor("TextProcessingSubWorkflow");

            // Step 3: Build a main workflow that uses the sub-workflow as an executor
            console.AppendLine("Building main workflow that uses the sub-workflow as an executor...");
            console.AppendLine();

            PrefixExecutor prefix = new("INPUT: ");
            PostProcessExecutor postProcess = new();

            var mainWorkflow = new WorkflowBuilder(prefix)
                .AddEdge(prefix, subWorkflowExecutor)
                .AddEdge(subWorkflowExecutor, postProcess)
                .WithOutputFrom(postProcess)        // NOTE: this WithOutputFrom is what populates WorkflowOutputEvent
                .Build();

            // Step 4: Execute the main workflow
            console.AppendLine($"Executing main workflow with input: '{text}'");
            console.AppendLine();

            await using Run run = await InProcessExecution.RunAsync(mainWorkflow, text);

            // Display results
            foreach (WorkflowEvent evt in run.NewEvents)
            {
                if (evt is ExecutorCompletedEvent executorComplete && executorComplete.Data is not null)
                {
                    console.AppendLine($"[{executorComplete.ExecutorId}] {executorComplete.Data}");
                }
                else if (evt is WorkflowOutputEvent output)
                {
                    console.AppendLine();
                    console.AppendLine("=== Main Workflow Completed ===");
                    console.AppendLine($"Final Output: {output.Data}");
                }
            }

            // Optional: Visualize the workflow structure
            console.AppendLine();
            console.AppendLine("=== Workflow Visualization ===");
            console.AppendLine("main:");
            console.AppendLine();
            console.AppendLine(mainWorkflow.ToMermaidString());     // - Note that sub-workflows are not rendered
            console.AppendLine();
            console.AppendLine("sub:");
            console.AppendLine();
            console.AppendLine(subWorkflow.ToMermaidString());
            console.AppendLine();

            console.AppendLine();
            console.AppendLine("✅ Sample Complete: Workflows can be composed hierarchically using sub-workflows\n");

            return console.ToString();
        }
        public static async Task<WorkflowEventListener_Response> Run2Async(string ollama_url, string ollama_model, string text)
        {
            var console = new StringBuilder();

            console.AppendLine();
            console.AppendLine("=== Sub-Workflow Demonstration ===");
            console.AppendLine();

            // Step 1: Build a simple text processing sub-workflow
            console.AppendLine("Building sub-workflow: Uppercase → Reverse → Append Suffix...");
            console.AppendLine();

            UppercaseExecutor uppercase = new();
            ReverseExecutor reverse = new();
            AppendSuffixExecutor append = new(" [PROCESSED]");

            var subWorkflow = new WorkflowBuilder(uppercase)
                .AddEdge(uppercase, reverse)
                .AddEdge(reverse, append)
                .WithOutputFrom(append)
                .Build();

            // Step 2: Configure the sub-workflow as an executor for use in the parent workflow
            ExecutorBinding subWorkflowExecutor = subWorkflow.BindAsExecutor("TextProcessingSubWorkflow");

            // Step 3: Build a main workflow that uses the sub-workflow as an executor
            console.AppendLine("Building main workflow that uses the sub-workflow as an executor...");
            console.AppendLine();

            PrefixExecutor prefix = new("INPUT: ");
            PostProcessExecutor postProcess = new();

            var mainWorkflow = new WorkflowBuilder(prefix)
                .AddEdge(prefix, subWorkflowExecutor)
                .AddEdge(subWorkflowExecutor, postProcess)
                .WithOutputFrom(postProcess)        // NOTE: this WithOutputFrom is what populates WorkflowOutputEvent
                .Build();

            // Step 4: Execute the main workflow
            console.AppendLine($"Executing main workflow with input: '{text}'");
            console.AppendLine();

            //await using Run run = await InProcessExecution.RunAsync(mainWorkflow, text);
            await using var run = await InProcessExecution.StreamAsync(mainWorkflow, text);

            var retVal = await WorkflowEventListener.ListenToStream(run);

            return retVal;
        }

        // ====================================
        // Text Processing Executors
        // ====================================

        /// <summary>
        /// Adds a prefix to the input text.
        /// </summary>
        internal sealed class PrefixExecutor(string prefix) : Executor<string, string>("PrefixExecutor")
        {
            public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                string result = prefix + message;
                Console.WriteLine($"[Prefix] '{message}' → '{result}'");
                return ValueTask.FromResult(result);
            }
        }

        /// <summary>
        /// Converts input text to uppercase.
        /// </summary>
        internal sealed class UppercaseExecutor() : Executor<string, string>("UppercaseExecutor")
        {
            public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                string result = message.ToUpperInvariant();
                Console.WriteLine($"[Uppercase] '{message}' → '{result}'");
                return ValueTask.FromResult(result);
            }
        }

        /// <summary>
        /// Reverses the input text.
        /// </summary>
        internal sealed class ReverseExecutor() : Executor<string, string>("ReverseExecutor")
        {
            public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                string result = string.Concat(message.Reverse());
                Console.WriteLine($"[Reverse] '{message}' → '{result}'");
                return ValueTask.FromResult(result);
            }
        }

        /// <summary>
        /// Appends a suffix to the input text.
        /// </summary>
        internal sealed class AppendSuffixExecutor(string suffix) : Executor<string, string>("AppendSuffixExecutor")
        {
            public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                string result = message + suffix;
                Console.WriteLine($"[AppendSuffix] '{message}' → '{result}'");
                return ValueTask.FromResult(result);
            }
        }

        /// <summary>
        /// Performs final post-processing by wrapping the text.
        /// </summary>
        internal sealed class PostProcessExecutor() : Executor<string, string>("PostProcessExecutor")
        {
            public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
            {
                string result = $"[FINAL] {message} [END]";
                Console.WriteLine($"[PostProcess] '{message}' → '{result}'");
                return ValueTask.FromResult(result);
            }
        }
    }
}
