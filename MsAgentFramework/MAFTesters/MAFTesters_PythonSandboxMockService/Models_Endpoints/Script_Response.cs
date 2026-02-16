using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_PythonSandboxMockService.Models_Endpoints
{
    public record Script_Response
    {
        public required bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; }

        public string ScriptName_Proposed { get; init; }
        public string ScriptName_Final { get; init; }

        public string OutputText {  get; init; }
    }
}
