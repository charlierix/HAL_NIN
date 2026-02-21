using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_PythonSandboxMockService.Models_Endpoints
{
    public record SessionList_Response
    {
        public required bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; }

        public SessionEntry[] Sessions { get; init; }
    }

    public record SessionEntry
    {
        public string Name { get; init; }
        public string Key { get; init; }
        public DateTime CreateDate { get; init; }
        public DateTime LastModifyDate { get; init; }


        // TODO: list of folders, list of python scripts

    }
}
