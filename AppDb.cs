using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;

namespace ProfileApp
{
    /// <summary>
    /// SQL Server LocalDB-backed data access for Profiles and Sections.
    /// Switches the app from SQLite to Microsoft SQL Server.
    /// </summary>
    public sealed class AppDb : IDisposable
    {
        private readonly SqlConnection _conn;

        /// <summary>
        /// Opens a LocalDB connection and ensures the schema exists.
        /// </summary>
        public AppDb()
        {
            // LocalDB connection string:
            // - (localdb)\MSSQLLocalDB : default LocalDB instance
            // - Initial Catalog         : your database name
            // - Integrated Security     : Windows auth (no user/password)
            // - MARS                    : multiple active readers
            const string instance = @"(localdb)\MSSQLLocalDB";
            const string dbName = "ProfileAppDb";

            // Ensure the database exists by connecting to the master database first.
            // If the DB is missing, create it. This avoids trying to open a connection
            // directly to a non-existent database (which causes "Cannot open database" errors).
            var masterCs = $@"Data Source={instance};Initial Catalog=master;Integrated Security=True;";
            using (var master = new SqlConnection(masterCs))
            {
                master.Open();
                using var cmd = master.CreateCommand();
                cmd.CommandText = $@"
                    IF DB_ID(N'{dbName}') IS NULL
                    BEGIN
                        CREATE DATABASE [{dbName}];
                    END;";
                cmd.ExecuteNonQuery();
            }

            // Now open a connection directly to the target DB
            var cs = $@"Data Source={instance};
                       Initial Catalog={dbName};
                       Integrated Security=True;
                       MultipleActiveResultSets=True";

            _conn = new SqlConnection(cs);
            _conn.Open();

            // Make sure our DB/tables exist (idempotent)
            EnsureCreatedSqlServer();
        }

        /// <summary>
        /// Creates the database objects (tables) if missing.
        /// This is safe to call at startup—does nothing if already created.
        /// </summary>
        private void EnsureCreatedSqlServer()
        {
            // NOTE: In ADO.NET, "USE ProfileAppDb" changes the context for this connection.
            // We set Initial Catalog=ProfileAppDb in the connection string, so we're already in the DB.
            // We still guard table existence with OBJECT_ID checks.
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                IF OBJECT_ID(N'dbo.Profiles', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Profiles (
                        Username     nvarchar(100) NOT NULL,
                        ProfileKey   nvarchar(50)  NOT NULL,  -- 'Box1','Box2','Box3','Box4' (or any profile id)
                        DisplayName  nvarchar(200) NULL,
                        Brief        nvarchar(max) NULL,
                        PhotoPath    nvarchar(400) NULL,
                        CONSTRAINT PK_Profiles PRIMARY KEY CLUSTERED (Username, ProfileKey)
                    );
                END;

                IF OBJECT_ID(N'dbo.Sections', N'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Sections (
                        Username     nvarchar(100) NOT NULL,
                        ProfileKey   nvarchar(50)  NOT NULL,
                        Section      nvarchar(50)  NOT NULL,  -- 'Education','Hobbies','Skills','Message'
                        Body         nvarchar(max) NULL,
                        UpdatedAt    datetime2(3)  NULL,
                        CONSTRAINT PK_Sections PRIMARY KEY CLUSTERED (Username, ProfileKey, Section)
                    );
                END;";
            cmd.ExecuteNonQuery();
        }

        // --------------------------------------------------------------------
        // Seeding (four distinct profiles per user, with unique defaults)
        // --------------------------------------------------------------------

        /// <summary>
        /// Seeds Box1–Box4 profiles for the given username, with unique defaults.
        /// Safe to call on each login—uses INSERT IF NOT EXISTS semantics.
        /// </summary>
        public void SeedDefaultsForUserWithProfiles(string username)
        {
            var profiles = new[]
            {
                new { Key = "Box1", Name = "Lance R.", Brief = "Hi! I'm Lance. I enjoy Java and C# projects." },
                new { Key = "Box2", Name = "Anna C.",  Brief = "Hello, I'm Anna—UI/UX enthusiast and coder." },
                new { Key = "Box3", Name = "Marc D.",  Brief = "Marc here. I like data and backend APIs." },
                new { Key = "Box4", Name = "Kyla P.",  Brief = "Kyla—frontend lover, curious about design." },
            };

            foreach (var p in profiles)
            {
                // Upsert-like: insert only if missing
                using (var cmd = _conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        IF NOT EXISTS (SELECT 1 FROM dbo.Profiles WHERE Username=@u AND ProfileKey=@k)
                            INSERT INTO dbo.Profiles(Username, ProfileKey, DisplayName, Brief, PhotoPath)
                            VALUES(@u, @k, @n, @b, @p);";
                    cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 100) { Value = username });
                    cmd.Parameters.Add(new SqlParameter("@k", SqlDbType.NVarChar, 50) { Value = p.Key });
                    cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 200) { Value = p.Name });
                    cmd.Parameters.Add(new SqlParameter("@b", SqlDbType.NVarChar, -1) { Value = p.Brief });
                    cmd.Parameters.Add(new SqlParameter("@p", SqlDbType.NVarChar, 400) { Value = "" });
                    cmd.ExecuteNonQuery();
                }

                // Sections defaults vary by profile to demonstrate uniqueness
                var defaults = DefaultSectionsForProfile(p.Key);
                foreach (var kvp in defaults)
                {
                    using var cmd2 = _conn.CreateCommand();
                    cmd2.CommandText = @"
                        IF NOT EXISTS (SELECT 1 FROM dbo.Sections WHERE Username=@u AND ProfileKey=@k AND Section=@s)
                            INSERT INTO dbo.Sections(Username, ProfileKey, Section, Body, UpdatedAt)
                            VALUES(@u, @k, @s, @b, SYSUTCDATETIME());";
                    cmd2.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 100) { Value = username });
                    cmd2.Parameters.Add(new SqlParameter("@k", SqlDbType.NVarChar, 50) { Value = p.Key });
                    cmd2.Parameters.Add(new SqlParameter("@s", SqlDbType.NVarChar, 50) { Value = kvp.Key });
                    cmd2.Parameters.Add(new SqlParameter("@b", SqlDbType.NVarChar, -1) { Value = kvp.Value });
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

        // --------------------------------------------------------------------
        // Profiles
        // --------------------------------------------------------------------

        /// <summary>
        /// Reads a single profile (display name, brief, photo path).
        /// </summary>
        public (string DisplayName, string Brief, string PhotoPath) GetProfile(string username, string profileKey)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT DisplayName, Brief, PhotoPath
                FROM dbo.Profiles
                WHERE Username=@u AND ProfileKey=@k;";
            cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 100) { Value = username });
            cmd.Parameters.Add(new SqlParameter("@k", SqlDbType.NVarChar, 50) { Value = profileKey });

            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                return (
                    r.IsDBNull(0) ? "" : r.GetString(0),
                    r.IsDBNull(1) ? "" : r.GetString(1),
                    r.IsDBNull(2) ? "" : r.GetString(2)
                );
            }
            return ("", "", "");
        }

        /// <summary>
        /// Upserts a profile row (insert if missing, else update).
        /// </summary>
        public void SaveProfile(string username, string profileKey, string displayName, string brief, string photoPath)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                IF EXISTS (SELECT 1 FROM dbo.Profiles WHERE Username=@u AND ProfileKey=@k)
                    UPDATE dbo.Profiles
                    SET DisplayName=@n, Brief=@b, PhotoPath=@p
                    WHERE Username=@u AND ProfileKey=@k;
                ELSE
                    INSERT INTO dbo.Profiles(Username, ProfileKey, DisplayName, Brief, PhotoPath)
                    VALUES(@u, @k, @n, @b, @p);";
            cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 100) { Value = username });
            cmd.Parameters.Add(new SqlParameter("@k", SqlDbType.NVarChar, 50) { Value = profileKey });
            cmd.Parameters.Add(new SqlParameter("@n", SqlDbType.NVarChar, 200) { Value = displayName ?? "" });
            cmd.Parameters.Add(new SqlParameter("@b", SqlDbType.NVarChar, -1) { Value = brief ?? "" });
            cmd.Parameters.Add(new SqlParameter("@p", SqlDbType.NVarChar, 400) { Value = photoPath ?? "" });
            cmd.ExecuteNonQuery();
        }

        // --------------------------------------------------------------------
        // Sections
        // --------------------------------------------------------------------

        /// <summary>
        /// Reads a single section body for the specified (user, profile, section).
        /// </summary>
        public string GetSection(string username, string profileKey, string section)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                SELECT Body
                FROM dbo.Sections
                WHERE Username=@u AND ProfileKey=@k AND Section=@s;";
            cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 100) { Value = username });
            cmd.Parameters.Add(new SqlParameter("@k", SqlDbType.NVarChar, 50) { Value = profileKey });
            cmd.Parameters.Add(new SqlParameter("@s", SqlDbType.NVarChar, 50) { Value = section });

            var result = cmd.ExecuteScalar();
            return result == null ? "" : (string)result;
        }

        /// <summary>
        /// Upserts a section body.
        /// </summary>
        public void SaveSection(string username, string profileKey, string section, string body)
        {
            using var cmd = _conn.CreateCommand();
            cmd.CommandText = @"
                IF EXISTS (SELECT 1 FROM dbo.Sections WHERE Username=@u AND ProfileKey=@k AND Section=@s)
                    UPDATE dbo.Sections
                    SET Body=@b, UpdatedAt=SYSUTCDATETIME()
                    WHERE Username=@u AND ProfileKey=@k AND Section=@s;
                ELSE
                    INSERT INTO dbo.Sections(Username, ProfileKey, Section, Body, UpdatedAt)
                    VALUES(@u, @k, @s, @b, SYSUTCDATETIME());";
            cmd.Parameters.Add(new SqlParameter("@u", SqlDbType.NVarChar, 100) { Value = username });
            cmd.Parameters.Add(new SqlParameter("@k", SqlDbType.NVarChar, 50) { Value = profileKey });
            cmd.Parameters.Add(new SqlParameter("@s", SqlDbType.NVarChar, 50) { Value = section });
            cmd.Parameters.Add(new SqlParameter("@b", SqlDbType.NVarChar, -1) { Value = body ?? "" });
            cmd.ExecuteNonQuery();
        }

        // --------------------------------------------------------------------
        // Disposal
        // --------------------------------------------------------------------
        public void Dispose() => _conn?.Dispose();
    }
}
