
using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace ProfileApp
{
    public class AppDb : IDisposable
    {
        private readonly string _dbPath;
        private readonly SqliteConnection _conn;

        public AppDb()
        {
            _dbPath = Path.Combine(AppContext.BaseDirectory, "AppData.db");
            _conn = new SqliteConnection($"Data Source={_dbPath}");
            _conn.Open();
            EnsureCreated();
        }

        private void EnsureCreated()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Profiles (
                    Username TEXT PRIMARY KEY,
                    Brief TEXT,
                    PhotoPath TEXT
                );

                CREATE TABLE IF NOT EXISTS Sections (
                    Username TEXT NOT NULL,
                    Section  TEXT NOT NULL, -- 'Education','Hobbies','Skills','Message'
                    Body     TEXT,
                    UpdatedAt TEXT,
                    PRIMARY KEY(Username, Section)
                );";
            cmd.ExecuteNonQuery();
        }

        public void SeedDefaultsForUser(string username)
        {
            // Seed Profiles if missing
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR IGNORE INTO Profiles(Username, Brief, PhotoPath)
                            VALUES ($u, $b, $p);";
                cmd.Parameters.AddWithValue("$u", username);
                cmd.Parameters.AddWithValue("$b", $"Hi, I'm {username}. I'm passionate about software development and continuous learning.");
                cmd.Parameters.AddWithValue("$p", ""); // set a default photo path if you want
                cmd.ExecuteNonQuery();
            }

            // Default section bodies (customize freely)
            var defaults = new Dictionary<string, string>
            {
                ["Education"] =
                    "• University: Polytechnic University of the Philippines\n" +
                    "• Program: BS in Computer Science (in progress)\n" +
                    "• Favorite Courses: Data Structures, OOP, Database Systems\n" +
                    "• Activities: Programming clubs, hackathons, study groups",

                ["Hobbies"] =
                    "• Coding side projects (C#, Java)\n" +
                    "• Gaming (strategy & RPG)\n" +
                    "• Music & Podcasts (tech, startups)\n" +
                    "• Reading tech blogs, documentation, and research",

                ["Skills"] =
                    "• Languages: C#, Java, Python\n" +
                    "• App Dev: WinForms/WPF, basic REST APIs\n" +
                    "• CS Fundamentals: Algorithms & Data Structures\n" +
                    "• Tools: Git/GitHub, SQLite, Visual Studio",

                ["Message"] =
                    "Maraming salamat po, Sir Bill! Your teaching is inspiring—\n" +
                    "clear, supportive, and motivating. We appreciate your guidance."
            };

            foreach (var kvp in defaults)
            {
                using var cmd2 = _conn.CreateCommand();
                cmd2.CommandText = @"INSERT OR IGNORE INTO Sections(Username, Section, Body, UpdatedAt)
                             VALUES ($u, $s, $b, $t);";
                cmd2.Parameters.AddWithValue("$u", username);
                cmd2.Parameters.AddWithValue("$s", kvp.Key);
                cmd2.Parameters.AddWithValue("$b", kvp.Value);
                cmd2.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("o"));
                cmd2.ExecuteNonQuery();
            }
        }


        private string DefaultBody(string section) => section switch
        {
            "Education" => "Add your education details here.",
            "Hobbies" => "Add your hobbies here.",
            "Skills" => "Add your skills here.",
            "Message" => "Add your message about kay Sir Bill here.",
            _ => ""
        };

        // Profile
        public (string Brief, string PhotoPath) GetProfile(string username)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Brief, PhotoPath FROM Profiles WHERE Username=$u;";
            cmd.Parameters.AddWithValue("$u", username);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return (reader.GetString(0), reader.IsDBNull(1) ? "" : reader.GetString(1));
            return ("", "");
        }

        public void SaveProfile(string username, string brief, string photoPath)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Profiles(Username, Brief, PhotoPath)
                                VALUES ($u, $b, $p)
                                ON CONFLICT(Username) DO UPDATE SET Brief=$b, PhotoPath=$p;";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$b", brief);
            cmd.Parameters.AddWithValue("$p", photoPath ?? "");
            cmd.ExecuteNonQuery();
        }

        // Sections
        public string GetSection(string username, string section)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Body FROM Sections WHERE Username=$u AND Section=$s;";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$s", section);
            var result = cmd.ExecuteScalar();
            return result == null ? "" : result.ToString() ?? "";
        }

        public void SaveSection(string username, string section, string body)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Sections(Username, Section, Body, UpdatedAt)
                                VALUES ($u, $s, $b, $t)
                                ON CONFLICT(Username, Section) DO UPDATE SET Body=$b, UpdatedAt=$t;";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$s", section);
            cmd.Parameters.AddWithValue("$b", body);
            cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public void Dispose()
        {
            _conn?.Dispose();
        }
    }
}
