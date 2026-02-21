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
        #region Declaration Section

        private readonly object _lock = new object();

        private string _base_folder;
        private string _dbPath;
        private string _docker_image_tag;

        private static Lazy<PythonSandboxMockService> _instance = new Lazy<PythonSandboxMockService>();

        #endregion

        public static async Task Init(string base_folder, string docker_image_tag)
        {
            // base folder
            //  make a sqlite db for settings
            //
            //  make folders for each session
            //      python sandbox folder
            //      any folders that were added for the scripts to work on

            var instance = _instance.Value;

            lock (instance._lock)
            {
                if (instance._base_folder != null && instance._base_folder != base_folder)
                    throw new ArgumentException($"Init has already been called with a different base folder.  existing: '{instance._base_folder}', new arg: {base_folder}");

                instance._base_folder = base_folder;
                instance._dbPath = Path.Combine(base_folder, "settings.db");
                instance._docker_image_tag = docker_image_tag;      // don't do anything with this until they make a new session (no point building images that never get used if there are multiple version changes between sessions)
            }
        }

        public static async Task<SessionList_Response> GetSessionList()
        {
            try
            {
                string db_path = null;
                var instance = _instance.Value;
                lock (instance._lock)
                    db_path = instance._dbPath;

                if (db_path == null)
                    return await Task.FromResult(new SessionList_Response
                    {
                        IsSuccess = false,
                        ErrorMessage = "Need to call Init() first",
                    });

                var sessions_db = await DAL.GetSessions(db_path);


                // TODO: also get folders/python scripts from each session's working folder
                // if db thinks a session exists, but it's not out in the base folder, mark it as errored/orphaned


                var sessions_respone = (sessions_db ?? []).
                    Select(o => new SessionEntry
                    {
                        Name = o.Name,
                        Key = o.Key,
                        CreateDate = o.CreateDate,
                        LastModifyDate = o.LastModifyDate,
                    }).
                    ToArray();

                return await Task.FromResult(new SessionList_Response
                {
                    IsSuccess = true,
                    Sessions = sessions_respone,
                });
            }
            catch (Exception ex)
            {
                return await Task.FromResult(new SessionList_Response
                {
                    IsSuccess = false,
                    ErrorMessage = $"Caught exception in GetSessionList: {ex}",
                });
            }
        }

        public static async Task<SessionAddRemove_Response> GetOrCreateSession(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return await Task.FromResult(SessionAddRemove_Response.BuildError("name passed in is blank"));

                // Get session list
                var existing = await GetSessionList();
                if (!existing.IsSuccess)
                    return await Task.FromResult(SessionAddRemove_Response.BuildError(existing.ErrorMessage));

                // Find name, trim, case insensitive
                var match = FindSession(name, existing.Sessions);

                if (match != null)
                    return await Task.FromResult(SessionAddRemove_Response.BuildSuccess(match.Key, match.Name));

                // It doesn't exist, create a new one
                return await NewSession_Create(name);
            }
            catch (Exception ex)
            {
                return await Task.FromResult(SessionAddRemove_Response.BuildError($"Caught exception in GetOrCreateSession: {ex}"));
            }
        }
        public static async Task<SessionAddRemove_Response> NewSession(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return await Task.FromResult(SessionAddRemove_Response.BuildError("name passed in is blank"));

                // Make sure there isn't an existing session with that name
                var existing = await GetSessionList();
                if (!existing.IsSuccess)
                    return await Task.FromResult(SessionAddRemove_Response.BuildError(existing.ErrorMessage));

                var match = FindSession(name, existing.Sessions);

                if (match != null)
                    return await Task.FromResult(SessionAddRemove_Response.BuildError($"session already exists.  key: {match.Key}, name: {match.Name}"));

                // It doesn't exist, create a new one
                return await NewSession_Create(name);
            }
            catch (Exception ex)
            {
                return await Task.FromResult(SessionAddRemove_Response.BuildError($"Caught exception in GetOrCreateSession: {ex}"));
            }
        }
        public static async Task<SessionAddRemove_Response> RemoveSession(string session_key)
        {
            return await Task.FromResult(SessionAddRemove_Response.BuildError("finish this"));
        }

        public static async Task<SessionFolder_Response> AddFolder(string session_key, string path)
        {
            return await Task.FromResult(new SessionFolder_Response
            {
                IsSuccess = false,
                ErrorMessage = "finish this",
            });
        }

        public static async Task<Script_Response> RunNewScript(string session_key, string script_name, string script_contents)
        {
            return await Task.FromResult(new Script_Response
            {
                IsSuccess = false,
                ErrorMessage = "finish this",
            });
        }
        public static async Task<Script_Response> RunExistingScript()
        {
            return await Task.FromResult(new Script_Response
            {
                IsSuccess = false,
                ErrorMessage = "finish this",
            });
        }

        #region Private Methods

        private static SessionEntry? FindSession(string name, SessionEntry[] sessions)
        {
            if (sessions == null || sessions.Length == 0)
                return null;

            string name_trimmed = name.Trim();

            return sessions.FirstOrDefault(o => o.Name.Trim().Equals(name_trimmed, StringComparison.OrdinalIgnoreCase));
        }

        private static async Task<SessionAddRemove_Response> NewSession_Create(string name)
        {
            // TODO: make sure there is an image built based on _docker_image_tag

            var instance = _instance.Value;     // no need for a lock when using these values, because it was already verified that Init was called, and Init should only be called once

            //string escaped_name = filesystemtools

            string path = Path.Combine(instance._base_folder, name);



        }

        #endregion
    }
}
