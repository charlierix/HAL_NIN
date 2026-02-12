using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_Core
{
    public record ResponseMessage
    {
        public required bool IsSuccess { get; init; }
        public string? Message { get; init; }
        public string? ErrorMessage { get; init; }

        public static ResponseMessage BuildSuccess(string message) =>
            new ResponseMessage { IsSuccess = true, Message = message };

        public static ResponseMessage BuildError(string error_message) =>
            new ResponseMessage { IsSuccess = false, ErrorMessage = error_message };

        public string GetReport()
        {
            var retVal = new StringBuilder();

            retVal.Append($"Success: {IsSuccess}, ");

            if (!string.IsNullOrWhiteSpace(Message))
                retVal.AppendLine(Message);

            if (!string.IsNullOrWhiteSpace(Message) && !string.IsNullOrWhiteSpace(ErrorMessage))
                retVal.AppendLine();

            if (!string.IsNullOrWhiteSpace(ErrorMessage))
                retVal.AppendLine(ErrorMessage);

            return retVal.ToString();
        }
    }
}
