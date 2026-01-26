using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_Core
{
    // TODO: come up with proper names

    public static class WorkflowEventListener
    {
        // examples seem to like to use workflow.stream instead of run
        // but this function is written like a run, so call it run

        // TODO: add a param that can accept events as they happen
        public static Task<WorkflowEventListener_1> AttemptStream(Workflow workflow)
        {



            return null;
        }


    }

    public record WorkflowEventListener_1
    {
        public required ChatMessage[] Messages { get; init; }

        public required string LogReport { get; init; }
    }
}
