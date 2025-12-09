using System;
using System.Collections.Generic;
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

            // Run migration first (safe no-op if already migrated)
            MigrateIfNeeded();

            // Ensure tables exist (for fresh DB)
            EnsureCreated();

        }

        private void MigrateIfNeeded()
        {
            // Detect if Profiles has ProfileKey column
            bool profilesHasProfileKey = false;
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(Profiles);";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var colName = reader.GetString(1);
                    if (string.Equals(colName, "ProfileKey", StringComparison.OrdinalIgnoreCase))
                    {
                        profilesHasProfileKey = true;
                        break;
                    }
                }
            }

            // Detect if Sections has ProfileKey column
            bool sectionsHasProfileKey = false;
            using (var cmd = _conn.CreateCommand())
            {
                cmd.CommandText = "PRAGMA table_info(Sections);";
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var colName = reader.GetString(1);
                    if (string.Equals(colName, "ProfileKey", StringComparison.OrdinalIgnoreCase))
                    {
                        sectionsHasProfileKey = true;
                        break;
                    }
                }
            }

            // If both already have ProfileKey, nothing to do
            if (profilesHasProfileKey && sectionsHasProfileKey)
                return;

            // Begin migration transaction
            using var tx = _conn.BeginTransaction();

            // ------------- Migrate Profiles -------------
            if (!profilesHasProfileKey)
            {
                // Create new table with correct schema
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Profiles_new (
                    Username    TEXT NOT NULL,
                    ProfileKey  TEXT NOT NULL,
                    DisplayName TEXT,
                    Brief       TEXT,
                    PhotoPath   TEXT,
                    PRIMARY KEY(Username, ProfileKey)
                );";
                    cmd.ExecuteNonQuery();
                }

                // Copy old rows into Profiles_new with a default ProfileKey ('Box1')
                // If your old DB had only one profile per user, we assume it's Box1.
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    // Try to read old columns: Username, Brief, PhotoPath (old schema)
                    // DisplayName fallback to empty string
                    cmd.CommandText = @"
                INSERT INTO Profiles_new(Username, ProfileKey, DisplayName, Brief, PhotoPath)
                SELECT 
                    Username,
                    'Box1' AS ProfileKey,
                    '' AS DisplayName,
                    Brief,
                    PhotoPath
                FROM Profiles;";
                    cmd.ExecuteNonQuery();
                }

                // Drop old table and rename new
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"DROP TABLE Profiles;";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"ALTER TABLE Profiles_new RENAME TO Profiles;";
                    cmd.ExecuteNonQuery();
                }
            }

            // ------------- Migrate Sections -------------
            if (!sectionsHasProfileKey)
            {
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Sections_new (
                    Username   TEXT NOT NULL,
                    ProfileKey TEXT NOT NULL,
                    Section    TEXT NOT NULL,
                    Body       TEXT,
                    UpdatedAt  TEXT,
                    PRIMARY KEY(Username, ProfileKey, Section)
                );";
                    cmd.ExecuteNonQuery();
                }

                // Copy old rows into Sections_new with default ProfileKey ('Box1')
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                INSERT INTO Sections_new(Username, ProfileKey, Section, Body, UpdatedAt)
                SELECT
                    Username,
                    'Box1' AS ProfileKey,
                    Section,
                    Body,
                    UpdatedAt
                FROM Sections;";
                    cmd.ExecuteNonQuery();
                }

                // Drop old table and rename
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"DROP TABLE Sections;";
                    cmd.ExecuteNonQuery();

                    cmd.CommandText = @"ALTER TABLE Sections_new RENAME TO Sections;";
                    cmd.ExecuteNonQuery();
                }
            }

            tx.Commit();
        }

        private void EnsureCreated()
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                -- Profiles: one account can own multiple profiles
                CREATE TABLE IF NOT EXISTS Profiles (
                    Username   TEXT NOT NULL,
                    ProfileKey TEXT NOT NULL,          -- 'Box1','Box2','Box3','Box4' (or any ID)
                    DisplayName TEXT,                  -- optional: name shown on About Me
                    Brief      TEXT,
                    PhotoPath  TEXT,
                    PRIMARY KEY(Username, ProfileKey)
                );

                -- Sections per profile
                CREATE TABLE IF NOT EXISTS Sections (
                    Username   TEXT NOT NULL,
                    ProfileKey TEXT NOT NULL,
                    Section    TEXT NOT NULL,          -- 'Education','Hobbies','Skills','Message'
                    Body       TEXT,
                    UpdatedAt  TEXT,
                    PRIMARY KEY(Username, ProfileKey, Section)
                );";
            cmd.ExecuteNonQuery();
        }

        /// <summary>
        /// Create 4 distinct profiles for the user with unique defaults.
        /// Safe to call on each login (INSERT OR IGNORE).
        /// </summary>
        public void SeedDefaultsForUserWithProfiles(string username)
        {
            var profiles = new[]
            {
                new { Key = "Box1", Name = "Lance R.",     Brief = "Hi! I'm Lance. I enjoy Java and C# projects." },
                new { Key = "Box2", Name = "Anna C.",      Brief = "Hello, I'm Anna—UI/UX enthusiast and coder." },
                new { Key = "Box3", Name = "Marc D.",      Brief = "Marc here. I like data and backend APIs." },
                new { Key = "Box4", Name = "Kyla P.",      Brief = "Kyla—frontend lover, curious about design." },
            };

            foreach (var p in profiles)
            {
                // Profiles
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT OR IGNORE INTO Profiles(Username, ProfileKey, DisplayName, Brief, PhotoPath)
                                        VALUES ($u, $k, $n, $b, $p);";
                    cmd.Parameters.AddWithValue("$u", username);
                    cmd.Parameters.AddWithValue("$k", p.Key);
                    cmd.Parameters.AddWithValue("$n", p.Name);
                    cmd.Parameters.AddWithValue("$b", p.Brief);
                    cmd.Parameters.AddWithValue("$p", ""); // optionally set per-profile image paths
                    cmd.ExecuteNonQuery();
                }

                // Sections defaults vary by profile to demonstrate uniqueness
                var defaults = DefaultSectionsForProfile(p.Key);

                foreach (var kvp in defaults)
                {
                    using var cmd2 = _conn.CreateCommand();
                    cmd2.CommandText = @"INSERT OR IGNORE INTO Sections(Username, ProfileKey, Section, Body, UpdatedAt)
                                         VALUES ($u, $k, $s, $b, $t);";
                    cmd2.Parameters.AddWithValue("$u", username);
                    cmd2.Parameters.AddWithValue("$k", p.Key);
                    cmd2.Parameters.AddWithValue("$s", kvp.Key);
                    cmd2.Parameters.AddWithValue("$b", kvp.Value);
                    cmd2.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("o"));
                    cmd2.ExecuteNonQuery();
                }
            }
        }

        private Dictionary<string, string> DefaultSectionsForProfile(string profileKey)
        {
            switch (profileKey)
            {
                case "Box1":
                    return new Dictionary<string, string>
                    {
                        ["Education"] = "PUP — BSCS (current). Focus: OOP, Data Structures.",
                        ["Hobbies"] = "Coding Java/C#, rhythm games, podcasts.",
                        ["Skills"] = "C#, Java, WinForms, SQL, Git.",
                        ["Message"] = "Salamat, Sir Bill! Your clarity helps a lot."
                    };
                case "Box2":
                    return new Dictionary<string, string>
                    {
                        ["Education"] = "UE — IT Program. Focus: UI/UX, Web Basics.",
                        ["Hobbies"] = "Figma design, journaling, music.",
                        ["Skills"] = "HTML/CSS/JS basics, Figma, C# beginner.",
                        ["Message"] = "Thank you, Sir Bill! We appreciate your support."
                    };
                case "Box3":
                    return new Dictionary<string, string>
                    {
                        ["Education"] = "TIP — CS major. Focus: DB & APIs.",
                        ["Hobbies"] = "Data viz, chess, backend challenges.",
                        ["Skills"] = "Python, SQL, REST, C# console apps.",
                        ["Message"] = "Thanks, Sir Bill—motivating lessons and feedback."
                    };
                case "Box4":
                    return new Dictionary<string, string>
                    {
                        ["Education"] = "PLM — SE track. Focus: front-end.",
                        ["Hobbies"] = "CSS art, React basics, typography.",
                        ["Skills"] = "UI layout, components, C# WinForms UI.",
                        ["Message"] = "Sir Bill, thank you! Very inspiring teaching."
                    };
                default:
                    return new Dictionary<string, string>
                    {
                        ["Education"] = "Add education here.",
                        ["Hobbies"] = "Add hobbies here.",
                        ["Skills"] = "Add skills here.",
                        ["Message"] = "Add your message about kay Sir Bill here."
                    };
            }
        }

        // Profile
        public (string DisplayName, string Brief, string PhotoPath) GetProfile(string username, string profileKey)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT DisplayName, Brief, PhotoPath
                                FROM Profiles WHERE Username=$u AND ProfileKey=$k;";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$k", profileKey);
            using var reader = cmd.ExecuteReader();
            if (reader.Read())
                return (
                    reader.IsDBNull(0) ? "" : reader.GetString(0),
                    reader.IsDBNull(1) ? "" : reader.GetString(1),
                    reader.IsDBNull(2) ? "" : reader.GetString(2)
                );
            return ("", "", "");
        }

        public void SaveProfile(string username, string profileKey, string displayName, string brief, string photoPath)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Profiles(Username, ProfileKey, DisplayName, Brief, PhotoPath)
                                VALUES ($u, $k, $n, $b, $p)
                                ON CONFLICT(Username, ProfileKey) DO UPDATE SET
                                    DisplayName=$n, Brief=$b, PhotoPath=$p;";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$k", profileKey);
            cmd.Parameters.AddWithValue("$n", displayName ?? "");
            cmd.Parameters.AddWithValue("$b", brief ?? "");
            cmd.Parameters.AddWithValue("$p", photoPath ?? "");
            cmd.ExecuteNonQuery();
        }

        // Sections
        public string GetSection(string username, string profileKey, string section)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"SELECT Body FROM Sections WHERE Username=$u AND ProfileKey=$k AND Section=$s;";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$k", profileKey);
            cmd.Parameters.AddWithValue("$s", section);
            var result = cmd.ExecuteScalar();
            return result == null ? "" : result.ToString() ?? "";
        }

        public void SaveSection(string username, string profileKey, string section, string body)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"INSERT INTO Sections(Username, ProfileKey, Section, Body, UpdatedAt)
                                VALUES ($u, $k, $s, $b, $t)
                                ON CONFLICT(Username, ProfileKey, Section) DO UPDATE SET Body=$b, UpdatedAt=$t;";
            cmd.Parameters.AddWithValue("$u", username);
            cmd.Parameters.AddWithValue("$k", profileKey);
            cmd.Parameters.AddWithValue("$s", section);
            cmd.Parameters.AddWithValue("$b", body);
            cmd.Parameters.AddWithValue("$t", DateTime.UtcNow.ToString("o"));
            cmd.ExecuteNonQuery();
        }

        public void Dispose() => _conn?.Dispose();
    }
}