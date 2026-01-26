using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_Core
{
    public record SessionArgs
    {

        // TODO: make the public folder that the user points to and another folder inside of a sandbox
        // container
        //
        // simple gets can go against public folder, but any python code would run in the sandbox on
        // files that were copied to the sandbox folder

        /// <summary>
        /// Base folder containing files that the job will look at
        /// </summary>
        public string SourceFolder { get; set; }

        /// <summary>
        /// Where generated files should go
        /// </summary>
        public string WorkingFolder { get; init; }

        /// <summary>
        /// Where generated python scripts go
        /// </summary>
        /// <remarks>
        /// There will need to be a util that makes sure a virtual environment is created if a python script is used
        /// </remarks>
        public string PythonFolder { get; init; }

        // maybe a max text size that should be sent to an llm.  if a file is too large, the llm should recurse or be fed chunks



        // TODO: expose two event pipes:
        //  - one is a direct copy of all WorkflowEvent from all workflows
        //  - one is a filtered version of the above with an enum, something like { StartingTool, StartingSub, FinishedTool, FinishedSub, Intermediate }
        //      Starting will have data that was passed to the item
        //      Finished will have data returned from the item


        // TODO: add memory of previous calls, maybe a few different levels of detail (full, and a couple types of summaries)



    }
}
