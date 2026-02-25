using MAFTesters_Core;
using MAFTesters_PythonSandboxMockService.Models_Endpoints;
using System.Diagnostics;

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

        private const string CALL_INIT_MSG = "need to call Init() first";

        private readonly object _lock = new object();

        private string _base_folder;
        private string _dbPath;
        private string _docker_image_tag;       // "python:3.12-slim"
        private string _docker_image_filename;      // "Dockerfile_" + _docker_image_tag with special chars (:.) converted to underscores (this is what the file would be called, later code may check and actually create this if it doesn't exist)

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

            if (string.IsNullOrWhiteSpace(base_folder))
                throw new ArgumentNullException(nameof(base_folder));

            if (string.IsNullOrWhiteSpace(docker_image_tag))
                throw new ArgumentNullException(nameof(docker_image_tag));

            var instance = _instance.Value;

            lock (instance._lock)
            {
                if (instance._base_folder != null && instance._base_folder != base_folder)
                    throw new ArgumentException($"Init has already been called with a different base folder.  existing: '{instance._base_folder}', new arg: {base_folder}");

                string escaped_folder = FileSystemUtils.EscapeFilename(base_folder, true);

                instance._base_folder = escaped_folder;
                instance._dbPath = Path.Combine(escaped_folder, "settings.db");
                instance._docker_image_tag = docker_image_tag;      // don't do anything with this until they make a new session (no point building images that never get used if there are multiple version changes between sessions)
                instance._docker_image_filename = GetDockerFilename(docker_image_tag);
            }
        }

        // TODO: GetSessionDetails(key)

        public static async Task<SessionList_Response> GetSessionList()
        {
            try
            {
                if (!EnsureInitWasCalled())
                    return await Task.FromResult(SessionList_Response.BuildError(CALL_INIT_MSG));

                var sessions_db = await DAL.GetSessions(_instance.Value._dbPath);


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

                return await Task.FromResult(SessionList_Response.BuildSuccess(sessions_respone));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(SessionList_Response.BuildError($"Caught exception in GetSessionList: {ex}"));
            }
        }
        public static async Task<FindSession_Response> FindSession(string name)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(name))
                    return await Task.FromResult(FindSession_Response.BuildError("name passed in is blank"));

                // Get session list
                var existing = await GetSessionList();
                if (!existing.IsSuccess)
                    return await Task.FromResult(FindSession_Response.BuildError(existing.ErrorMessage));

                // Find name, trim, case insensitive
                var match = FindSession(name, existing.Sessions);

                if (match != null)
                    return await Task.FromResult(FindSession_Response.BuildSuccess(match));

                // It doesn't exist, create a new one
                return await Task.FromResult(FindSession_Response.BuildSuccess(null));      // not found is an expected possible outcome, so isn't an error
            }
            catch (Exception ex)
            {
                return await Task.FromResult(FindSession_Response.BuildError($"Caught exception in GetOrCreateSession: {ex}"));
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
                return await Task.FromResult(SessionAddRemove_Response.BuildError($"Caught exception in NewSession: {ex}"));
            }
        }
        public static async Task<SessionAddRemove_Response> RemoveSession(string session_key)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(session_key))
                    return await Task.FromResult(SessionAddRemove_Response.BuildError("session_key passed in is blank"));

                if (!EnsureInitWasCalled())
                    return await Task.FromResult(SessionAddRemove_Response.BuildError(CALL_INIT_MSG));

                //var session = await DAL.GetSession(_instance.Value._dbPath, session_key);
                var session = await DAL.RemoveSession(_instance.Value._dbPath, session_key);

                if (session == null)
                    return await Task.FromResult(SessionAddRemove_Response.BuildError($"session doesn't exist: {session_key}"));

                // Try to delete the folder
                // TODO: if there is an issue, log a warning
                try { Directory.Delete(session.FolderName, true); } catch (Exception) { }

                return await Task.FromResult(SessionAddRemove_Response.BuildSuccess(session_key, session.Name));
            }
            catch (Exception ex)
            {
                return await Task.FromResult(SessionAddRemove_Response.BuildError($"Caught exception in RemoveSession: {ex}"));
            }
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

        private static async Task<SessionAddRemove_Response> NewSession_Create(string name)
        {
            // TODO: make sure there is an image built based on _docker_image_tag

            var instance = _instance.Value;     // no need for a lock when using these values, because it was already verified that Init was called, and Init should only be called once

            string escaped_name = FileSystemUtils.EscapeFilename(name);

            string path = Path.Combine(instance._base_folder, escaped_name);

            if (Directory.Exists(path))
                return await Task.FromResult(SessionAddRemove_Response.BuildError($"folder already exists with that name.  name: '{name}', escaped name: '{escaped_name}'"));

            // Make sure there is a docker image for the tag
            try
            {
                await EnsureDockerExists();
            }
            catch (Exception ex)
            {
                return await Task.FromResult(SessionAddRemove_Response.BuildError($"couldn't create docker image: {ex}"));
            }

            // Create the folder first
            try
            {
                Directory.CreateDirectory(path);
            }
            catch (Exception ex)
            {
                return await Task.FromResult(SessionAddRemove_Response.BuildError($"couldn't create the folder.  path: {path}"));
            }

            // Add to DB
            string key = null;
            try
            {
                key = await DAL.AddSession(instance._dbPath, name, path);
            }
            catch (Exception ex)
            {
                // Try to delete the folder, it's not the end of the world if it fails
                // TODO: if there is an issue, log a warning
                try { Directory.Delete(path, true); } catch (Exception) { }

                return await Task.FromResult(SessionAddRemove_Response.BuildError($"couldn't add session to the db: {ex.Message}"));
            }

            return await Task.FromResult(SessionAddRemove_Response.BuildSuccess(key, name));
        }

        private static bool EnsureInitWasCalled()
        {
            string db_path = null;
            var instance = _instance.Value;
            lock (instance._lock)
                db_path = instance._dbPath;

            return db_path != null;
        }

        private static async Task EnsureDockerExists()
        {
            // NOTE: the calling function wraps this in a try/catch, so no need to do that here

            var instance = _instance.Value;

            if (!Directory.Exists(instance._base_folder))
                Directory.CreateDirectory(instance._base_folder);

            // Look for the dockerfile
            string path = Path.Combine(instance._base_folder, instance._docker_image_filename);
            if (File.Exists(path))
                return;

            // It's not there, create it
            string docker_contents =
$@"# Debian (Linux) with Python pre-installed
FROM {instance._docker_image_tag}

# install apt-utils (`useradd`, `userdel`, `usermod`, etc)
RUN apt-get update && \
    apt-get install -y --no-install-recommends apt-utils

# create python user, also containers folder (mount point for admin account)
# -M suppresses home directory creation (/home/python_user)
# admin's mount point, session subfolders will go under here
# Restricted permissions for python_user (python_user will only be allowed on the subfolders)

RUN useradd -s /bin/false -M python_user && \
    mkdir -p /containers && \
    chown root:root /containers && \
    chmod 750 /containers";

            File.WriteAllText(path, docker_contents);

            // Build it
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "docker",
                Arguments = $"build -t {instance._docker_image_filename} -f \"{path}\" .",
                UseShellExecute = false,        // critical for hiding the window
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            using Process process = new Process { StartInfo = startInfo };

            process.Start();

            // Read output and error streams asynchronously
            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            // Check exit code and errors
            //if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))        // it's always outputing to error
            if (process.ExitCode != 0)
                throw new Exception($"Docker build failed: {output}{Environment.NewLine}{error}");

            // may want to log the output
            // Debug.WriteLine(output);
        }

        private static string GetDockerFilename(string tag)
        {
            string cleaned = tag.
                Replace('.', '_').      // . is normally valid, but in this case, make it an underscore
                Replace(':', '_');

            cleaned = FileSystemUtils.EscapeFilename(cleaned);

            cleaned = cleaned.ToLower();        // docker requires tag to be lower case

            return $"dockerfile_{cleaned}";
        }

        private static SessionEntry? FindSession(string name, SessionEntry[] sessions)
        {
            if (sessions == null || sessions.Length == 0)
                return null;

            string name_trimmed = name.Trim();

            return sessions.FirstOrDefault(o => o.Name.Trim().Equals(name_trimmed, StringComparison.OrdinalIgnoreCase));
        }

        #endregion
    }
}
