using MAFTesters_PythonSandboxMockService.Models_Endpoints;
using Microsoft.Data.Sqlite;
using System.Text.Json;
using System.Text.Json.Nodes;

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

        private static Lazy<PythonSandboxMockService> _instance = new Lazy<PythonSandboxMockService>();

        #endregion

        public static async Task Init(string base_folder)
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
                instance._base_folder = base_folder;
                instance._dbPath = Path.Combine(base_folder, "settings.db");


                // don't need to use db until requests come in
                //// Connect to the SQLite database (creates file if missing)
                //using SqliteConnection connection = new SqliteConnection($"Data Source={instance._dbPath}");
                //connection.Open();


                //    // Create the Settings table if it doesn't exist
                //    string createTableQuery = @"
                //CREATE TABLE IF NOT EXISTS Settings (
                //    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                //    Key TEXT NOT NULL UNIQUE,
                //    Value TEXT,
                //    CreatedAt TEXT,
                //    UpdatedAt TEXT
                //)";

                //    using SqliteCommand command = new SqliteCommand(createTableQuery, connection);
                //    command.ExecuteNonQuery();

                //    Console.WriteLine("Database and table initialized successfully.");


            }
        }

        public static async Task<SessionAddRemove_Response> NewSession(string name)
        {
            string latest_image_tag = await GetLatestPythonSlimTag();






            return await Task.FromResult(SessionAddRemove_Response.BuildError("finish this"));
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

        #region Private Methods

        private static async Task<string> GetLatestPythonSlimTag()
        {
            try
            {
                using HttpClient client = new HttpClient();

                string path = "https://hub.docker.com/v2/repositories/python/tags/";

                HttpResponseMessage response = await client.GetAsync(path);
                response.EnsureSuccessStatusCode();

                string responseBody = await response.Content.ReadAsStringAsync();




                return await Task.FromResult("finish this");
            }
            catch (Exception ex)
            {
                return await Task.FromResult((string)null);
            }
        }

        /*
        private static async Task<string> GetLatestPythonSlimTag()
        {
            try
            {
                using (HttpClient client = new HttpClient())
                {
                    client.UserAgent.ParseAdd("PythonSlimChecker/1.0");

                    string apiUrl = "https://hub.docker.com/v2/repositories/python/tags/";

                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    response.EnsureSuccessStatusCode();

                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Parse JSON response
                    var doc = JsonDocument.Parse(responseBody);
                    JsonElement root = doc.RootElement;
                    JsonArray tags = root.GetArray("results") ?? Array.Empty<JsonElement>();

                    string latestTag = null;
                    string latestVersion = null;

                    foreach (JsonElement tag in tags)
                    {
                        string tagName = tag.GetProperty("name").GetString();
                        if (!tagName.EndsWith("-slim")) continue;

                        string version = tagName.Replace("-slim", "").Trim();

                        if (string.IsNullOrEmpty(latestVersion) || CompareVersion(version, latestVersion) > 0)
                        {
                            latestVersion = version;
                            latestTag = tagName;
                        }
                    }

                    return latestTag ?? "3.12-slim"; // Fallback to common version
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving tag: {ex.Message}");
                return "3.12-slim"; // Default fallback
            }
        }

        private static int CompareVersion(string v1, string v2)
        {
            var parts1 = v1.Split('.').Select(int.Parse).ToList();
            var parts2 = v2.Split('.').Select(int.Parse).ToList();

            // Remove trailing zeros
            while (parts1.Count > 0 && parts1[^1] == 0) parts1.RemoveAt(parts1.Count - 1);
            while (parts2.Count > 0 && parts2[^1] == 0) parts2.RemoveAt(parts2.Count - 1);

            if (parts1.Count != parts2.Count)
                return parts1.Count > parts2.Count ? 1 : -1;

            for (int i = 0; i < parts1.Count; i++)
            {
                if (parts1[i] != parts2[i])
                    return parts1[i].CompareTo(parts2[i]);
            }
            return 0;
        }
        */

        #endregion
    }
}
