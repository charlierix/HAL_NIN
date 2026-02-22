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

        public static SessionList_Response BuildError(string error_msg)
        {
            return new SessionList_Response
            {
                IsSuccess = false,
                ErrorMessage = error_msg,
            };
        }
        public static SessionList_Response BuildSuccess(SessionEntry[] sessions)
        {
            return new SessionList_Response
            {
                IsSuccess = true,
                Sessions = sessions,
            };
        }
    }

    public record FindSession_Response
    {
        public required bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; }

        public SessionEntry Session { get; init; }

        public static FindSession_Response BuildError(string error_msg)
        {
            return new FindSession_Response
            {
                IsSuccess = false,
                ErrorMessage = error_msg,
            };
        }
        public static FindSession_Response BuildSuccess(SessionEntry session)
        {
            return new FindSession_Response
            {
                IsSuccess = true,
                Session = session,
            };
        }
    }

    public record SessionEntry
    {
        public string Name { get; init; }
        public string Key { get; init; }
        public DateTime CreateDate { get; init; }
        public DateTime LastModifyDate { get; init; }


        // TODO: list of folders, list of python scripts

        public override string ToString()
        {
            return $"{Name} | {Key} | {CreateDate.ToLocalTime():yyyyMMdd} | {LastModifyDate.ToLocalTime():yyyyMMdd}";
        }
    }
}
