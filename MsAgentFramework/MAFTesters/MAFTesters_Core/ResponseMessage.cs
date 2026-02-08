using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_Core
{
    public record ResponseMessage
    {
        public string? Message { get; init; }
        public string? ErrorMessage { get; init; }
        public required bool IsSuccess { get; init; }

        public static ResponseMessage BuildSuccess(string message) =>
            new ResponseMessage { IsSuccess = true, Message = message };

        public static ResponseMessage BuildError(string error_message) =>
            new ResponseMessage { IsSuccess = false, ErrorMessage = error_message };
    }
}
