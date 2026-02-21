using MAFTesters_PythonSandboxMockService.Models_DB;
using Microsoft.Data.Sqlite;

namespace MAFTesters_PythonSandboxMockService
{
    public static class DAL
    {
        public static async Task<Session[]> GetSessions(string db_path)
        {
            await EnsureTablesExist(db_path);

            List<Session> sessions = new List<Session>();

            using SqliteConnection connection = new SqliteConnection($"Data Source={db_path}");
            await connection.OpenAsync();

            string selectQuery = "SELECT * FROM Sessions";
            using SqliteCommand command = new SqliteCommand(selectQuery, connection);
            using SqliteDataReader reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                long sessionId = reader.GetInt64(reader.GetOrdinal("SessionID"));
                string key = reader.GetString(reader.GetOrdinal("Key"));
                string name = reader.GetString(reader.GetOrdinal("Name"));
                string folder_name = reader.GetString(reader.GetOrdinal("FolderName"));
                DateTime createDate = reader.GetDateTime(reader.GetOrdinal("CreateDate"));
                DateTime lastModify = reader.GetDateTime(reader.GetOrdinal("LastModifyDate"));

                sessions.Add(new Session
                {
                    SessionID = sessionId,
                    Key = key,
                    Name = name,
                    FolderName = folder_name,
                    CreateDate = createDate,
                    LastModifyDate = lastModify
                });
            }

            return sessions.ToArray();
        }

        //public static async Task<Session> GetSession(string db_path, string key)
        //{

        //}

        public static async Task<string> AddSession(string db_path, string name, string folder_name)
        {
            await EnsureTablesExist(db_path);

            using SqliteConnection connection = new SqliteConnection($"Data Source={db_path}");
            await connection.OpenAsync();

            string insertQuery =
@"INSERT INTO Sessions (Name, CreateDate, LastModifyDate)
VALUES (@Key, @Name, @FolderName, @CreateDate, @LastModifyDate)";

            string key = Guid.NewGuid().ToString();
            DateTime now = DateTime.UtcNow;

            using SqliteCommand command = new SqliteCommand(insertQuery, connection);
            command.Parameters.AddWithValue("@Key", key);
            command.Parameters.AddWithValue("@Name", name);
            command.Parameters.AddWithValue("@FolderName", name);
            command.Parameters.AddWithValue("@CreateDate", now);
            command.Parameters.AddWithValue("@LastModifyDate", now);

            await command.ExecuteNonQueryAsync();

            return await Task.FromResult(key);
        }

        private static async Task EnsureTablesExist(string db_path)
        {
            using SqliteConnection connection = new SqliteConnection($"Data Source={db_path}");
            await connection.OpenAsync();

            // NOTE: sqlite's integer is 64 bit

            await EnsureTableExists_Session(connection);
        }
        private static async Task EnsureTableExists_Session(SqliteConnection connection)
        {
            string createTableQuery =
@"CREATE TABLE IF NOT EXISTS Sessions (
    SessionID INTEGER PRIMARY KEY AUTOINCREMENT,
    Key TEXT NOT NULL UNIQUE,
    Name TEXT NOT NULL,
    FolderName TEXT NOT NULL,
    CreateDate DATETIME NOT NULL,
    LastModifyDate DATETIME)";

            using SqliteCommand command = new SqliteCommand(createTableQuery, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}
