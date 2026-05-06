using System;
using System.Configuration;
using BACKUPANDRESTORETOOL.Services;

namespace BACKUPANDRESTORETOOL.Model
{
    public class DatabaseConfig
    {
        public string Server { get; private set; }
        public string Port { get; private set; }
        public string Database { get; set; }
        public string UserId { get; private set; }
        public string Password { get; private set; }

        // ── These are no longer read from App.config ──
        // They are resolved from the system PATH in BackupRestoreController
        // and kept here only so existing code that references them still compiles.
        public string MySqlPath { get; set; } = string.Empty;
        public string SevenZipPath { get; set; } = string.Empty;

        public DatabaseConfig()
        {
            var cs = ConfigurationManager.ConnectionStrings["MySqlConnection"];
            if (cs == null)
                throw new Exception("MySqlConnection not found in App.config");

            foreach (var part in cs.ConnectionString.Split(';'))
            {
                var kv = part.Split(new[] { '=' }, 2);
                if (kv.Length != 2) continue;

                var key = kv[0].Trim().ToLower();
                var val = kv[1].Trim();

                switch (key)
                {
                    case "server":
                    case "host": Server = val; break;
                    case "port": Port = val; break;
                    case "database":
                    case "initial catalog": Database = val; break;
                    case "uid":
                    case "user id":
                    case "username": UserId = val; break;
                    case "password":
                    case "pwd": Password = val; break;
                }
            }

            if (string.IsNullOrWhiteSpace(Port)) Port = "3306";

            // ── Decrypt password from connection string ──
            if (!string.IsNullOrWhiteSpace(Password))
            {
                try
                {
                    var crypto = new ACryptoServiceProvider();
                    Password = crypto.Decrypt(Password, "pullasciiencrypt");
                }
                catch
                {
                    // If decryption fails, use as-is (plain text)
                }
            }

            // ── MySqlDumpPath, MySqlPath, SevenZipPath are intentionally NOT loaded here ──
            // They are resolved at runtime from the system PATH environment variable
            // inside BackupRestoreController.ResolveFromPath()
        }

        // ── Only EncrpytedPassword remains in App.config ──
        // SavePath is kept in case other parts of the app still use it for the password key
        public static void SavePath(string key, string value)
        {
            // Guard: do not allow saving path keys back to App.config
            if (key == "MySqlDumpPath" || key == "MySqlPath" || key == "SevenZipPath")
                throw new InvalidOperationException(
                    "'" + key + "' is no longer stored in App.config.\n" +
                    "Add it to the system PATH environment variable instead.");

            var config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (config.AppSettings.Settings[key] == null)
                config.AppSettings.Settings.Add(key, value);
            else
                config.AppSettings.Settings[key].Value = value;

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection("appSettings");
        }
    }
}