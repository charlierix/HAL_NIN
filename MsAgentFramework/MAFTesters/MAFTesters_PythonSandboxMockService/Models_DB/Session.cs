using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_PythonSandboxMockService.Models_DB
{
    public record Session
    {
        // Primary Key
        public long SessionID { get; set; }

        public string Name { get; init; }
        public string Key { get; init; }
        public DateTime CreateDate { get; init; }
        public DateTime LastModifyDate { get; init; }
    }
}
