using System;
using System.Collections.Generic;
using System.Text;

namespace MAFTesters_PythonSandboxMockService.Models_Endpoints
{
    public record SessionFolder_Response
    {
        public required bool IsSuccess { get; init; }

        public string ErrorMessage { get; init; }

        public string SessionID { get; init; }
        public string SessionName { get; init; }

        public string Host_Path { get; init; }
        /// <summary>
        /// The folder to give to python scripts
        /// </summary>
        /// <remarks>
        /// The folder is copied under a session folder in the docker container, sibling of the python folder
        /// 
        /// So if the host path is "c:\blah\blah\desktop\sample files", then python relative path would be something
        /// like "../sample files" (container is linux)
        /// </remarks>
        public string PythonRelative_Path { get; init; }
    }
}
