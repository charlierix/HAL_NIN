using MAFTesters_PythonSandboxMockService.Models_Endpoints;

namespace MAFTesters_PythonSandboxMockService
{
    /// <summary>
    /// Manages the docker python/admin containers (used to run generated python scripts in a sandbox)
    /// </summary>
    /// <remarks>
    /// The contents of this project need to eventually be turned into a rest service, since a client crashing
    /// could leave containers hanging out there forever, or there may be multiple clients that need to use the
    /// same session in a container, etc
    /// </remarks>
    public class PythonSandboxMockService
    {
        public static Task<SessionAddRemove_Response> NewSession(string name)
        {
            return Task.FromResult(SessionAddRemove_Response.BuildError("finish this"));
        }
        public static Task<SessionAddRemove_Response> RemoveSession(string id)
        {
            return Task.FromResult(SessionAddRemove_Response.BuildError("finish this"));
        }

        public static Task<SessionFolder_Response> AddFolder(string id, string path)
        {
            return Task.FromResult(new SessionFolder_Response
            {
                IsSuccess = false,
                ErrorMessage = "finish this",
            });
        }

        public static Task<Script_Response> RunNewScript(string id, string script_name, string script_contents)
        {
            return Task.FromResult(new Script_Response
            {
                IsSuccess = false,
                ErrorMessage = "finish this",
            });
        }
        public static Task<Script_Response> RunExistingScript()
        {
            return Task.FromResult(new Script_Response
            {
                IsSuccess = false,
                ErrorMessage = "finish this",
            });
        }
    }
}
