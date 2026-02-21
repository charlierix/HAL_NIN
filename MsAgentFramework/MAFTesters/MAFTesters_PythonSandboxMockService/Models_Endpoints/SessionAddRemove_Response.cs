using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_PythonSandboxMockService.Models_Endpoints
{
    public record SessionAddRemove_Response
    {
        public required bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; }

        public string SessionKey { get; init; }
        public string SessionName { get; init; }

        public static SessionAddRemove_Response BuildError(string error_msg)
        {
            return new SessionAddRemove_Response
            {
                IsSuccess = false,
                ErrorMessage = error_msg,
            };
        }
        public static SessionAddRemove_Response BuildSuccess(string key, string name)
        {
            return new SessionAddRemove_Response
            {
                IsSuccess = true,
                SessionKey = key,
                SessionName = name,
            };
        }
    }
}
