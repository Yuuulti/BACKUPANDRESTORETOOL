using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Configuration;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using BACKUPANDRESTORETOOL.Model;
using BACKUPANDRESTORETOOL.Services;

namespace BACKUPANDRESTORETOOL.Controller
{
    public class BackupRestoreController
    {
        private readonly DatabaseConfig _config;
        private readonly ACryptoServiceProvider _crypto;

        // ── Resolved full paths from PATH environment variable ──
        private readonly string _sevenZipGuiExe;   // 7zg.exe or 7z.exe — used for compress/extract
        private readonly string _sevenZipCliExe;   // 7z.exe             — used for listing only
        private readonly string _mysqlDumpExe;     // mysqldump.exe
        private readonly string _mysqlExe;         // mysql.exe

        // ── Base folder for all temp files and logs ──
        private const string BackupTempFolder = @"D:\backup";

        public BackupRestoreController()
        {
            _config = new DatabaseConfig();
            _crypto = new ACryptoServiceProvider();

            // ── Resolve all executables from PATH ──
            // 7zg.exe may not exist in all 7-Zip installations; fall back to 7z.exe
            _sevenZipGuiExe = TryResolveFromPath("7zg.exe")
               ?? TryResolveFromPath("7zFM.exe")
               ?? ResolveFromPath("7z.exe", "7-Zip (7z.exe)");

            _sevenZipCliExe = ResolveFromPath("7z.exe", "7-Zip CLI (7z.exe)");
            _mysqlDumpExe = ResolveFromPath("mysqldump.exe", "MySQL (mysqldump.exe)");
            _mysqlExe = ResolveFromPath("mysql.exe", "MySQL (mysql.exe)");
        }

        public DatabaseConfig GetConfig() => _config;

        // ════════════════════════════════════════════════════════════
        // ── LIVE PROGRESS LOG HELPERS ──
        // ════════════════════════════════════════════════════════════

        /// <summary>
        /// Returns the path of the live progress log for a given session.
        /// Format: D:\backup\progress_SESSIONID.log
        /// </summary>
        private static string GetProgressLogPath(string sessionId)
        {
            return Path.Combine(BackupTempFolder, string.Format("progress_{0}.log", sessionId));
        }

        /// <summary>
        /// Appends a timestamped status line to the live progress log.
        /// Safe to call from any thread — never throws.
        /// </summary>
        private static void LogProgress(string progressLogPath, string message)
        {
            try
            {
                string line = string.Format("[{0}] {1}{2}",
                    DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt").ToLower(),
                    message,
                    Environment.NewLine);

                File.AppendAllText(progressLogPath, line, Encoding.UTF8);
            }
            catch { /* never let progress logging crash the backup */ }
        }

        // ════════════════════════════════════════════════════════════
        // ── RESOLVE EXE FROM SYSTEM PATH ENVIRONMENT VARIABLE ──
        // ════════════════════════════════════════════════════════════

        private static string ResolveFromPath(string exeName, string friendlyName)
        {
            string result = TryResolveFromPath(exeName);
            if (result != null)
                return result;

            throw new FileNotFoundException(
                string.Format(
                    "{0} not found in PATH.\n\n" +
                    "Please add the folder containing '{1}' to your System PATH.\n\n" +
                    "Steps:\n" +
                    "  1. Right-click 'This PC' > Properties > Advanced system settings\n" +
                    "  2. Click 'Environment Variables'\n" +
                    "  3. Under System variables, select 'Path' and click Edit\n" +
                    "  4. Click New and add the folder (e.g. C:\\Program Files\\7-Zip)\n" +
                    "  5. Click OK and restart the application.",
                    friendlyName, exeName));
        }

        private static string TryResolveFromPath(string exeName)
        {
            string machinePath = Environment.GetEnvironmentVariable(
                                     "PATH", EnvironmentVariableTarget.Machine) ?? string.Empty;
            string userPath = Environment.GetEnvironmentVariable(
                                     "PATH", EnvironmentVariableTarget.User) ?? string.Empty;
            string procesPath = Environment.GetEnvironmentVariable(
                                     "PATH", EnvironmentVariableTarget.Process) ?? string.Empty;

            var allPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string bucket in new[] { procesPath, machinePath, userPath })
            {
                foreach (string folder in bucket.Split(
                    new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    string trimmed = folder.Trim().Trim('"');
                    if (!string.IsNullOrWhiteSpace(trimmed))
                        allPaths.Add(trimmed);
                }
            }

            foreach (string folder in allPaths)
            {
                try
                {
                    string candidate = Path.Combine(folder, exeName);
                    if (File.Exists(candidate))
                        return candidate;
                }
                catch { /* skip malformed paths */ }
            }

            return null;
        }

        // ── Call this from your Form Load event to give a clear early error ──
        public string ValidateEnvironment()
        {
            var missing = new System.Text.StringBuilder();

            if (!File.Exists(_mysqlDumpExe))
                missing.AppendLine("  - mysqldump.exe  (MySQL bin folder not in PATH)");
            if (!File.Exists(_mysqlExe))
                missing.AppendLine("  - mysql.exe      (MySQL bin folder not in PATH)");
            if (!File.Exists(_sevenZipCliExe))
                missing.AppendLine("  - 7z.exe         (7-Zip folder not in PATH)");
            if (missing.Length == 0)
                return null; // all good

            return
                "The following required programs were not found in PATH:\n\n" +
                missing.ToString() +
                "\nSteps to fix on this machine:\n" +
                "  1. Right-click 'This PC' > Properties > Advanced system settings\n" +
                "  2. Click 'Environment Variables'\n" +
                "  3. Under System variables, find 'Path' and click Edit\n" +
                "  4. Add the MySQL bin folder  (e.g. C:\\Program Files\\MySQL\\MySQL Server 8.0\\bin)\n" +
                "  5. Add the 7-Zip folder      (e.g. C:\\Program Files\\7-Zip)\n" +
                "  6. Click OK and RESTART this application.";
        }

        // ── GET ALL DATABASES ──
        public List<string> GetDatabases()
        {
            var databases = new List<string>();

            string connStr = string.Format(
                "Server={0};Port={1};Uid={2};Password={3};",
                _config.Server, _config.Port, _config.UserId, _config.Password);

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();
                using (var cmd = new MySqlCommand("SHOW DATABASES;", conn))
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string dbName = reader.GetString(0);
                        if (dbName != "information_schema" &&
                            dbName != "performance_schema" &&
                            dbName != "mysql" &&
                            dbName != "sys")
                            databases.Add(dbName);
                    }
                }
            }

            return databases;
        }

        // ── GET DATABASES INSIDE A ZIP FILE ──
        public List<string> GetDatabasesFromZip(string zipPath)
        {
            string encryptedPassword = ConfigurationManager.AppSettings["EncrpytedPassword"];
            if (string.IsNullOrWhiteSpace(encryptedPassword))
                throw new Exception("No encrypted password found in App.config.");

            string plainPassword = _crypto.Decrypt(encryptedPassword, "pullasciiencrypt");

            var databases = new List<string>();

            // ── Always use 7z.exe for listing (7zg.exe does not support the 'l' command) ──
            string args = string.Format("l \"{0}\" \"-p{1}\"", zipPath, plainPassword);

            var psi = new ProcessStartInfo
            {
                FileName = _sevenZipCliExe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();
                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                foreach (var line in output.Split('\n'))
                {
                    var trimmed = line.Trim();
                    if (trimmed.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
                    {
                        var parts = trimmed.Split(
                            new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length > 0)
                        {
                            string fileName = Path.GetFileNameWithoutExtension(
                                parts[parts.Length - 1]);
                            if (!string.IsNullOrWhiteSpace(fileName))
                                databases.Add(fileName);
                        }
                    }
                }
            }

            return databases;
        }

        public void SetDatabase(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Please select a database.");
            _config.Database = databaseName;
        }

        // ════════════════════════════════════════════════════════════
        // ── BACKUP SINGLE ──
        // ════════════════════════════════════════════════════════════
        public void Backup(string databaseName, string destinationZipPath, string plainPassword)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
                throw new ArgumentException("Database name cannot be empty.");
            if (string.IsNullOrWhiteSpace(destinationZipPath))
                throw new ArgumentException("Destination path cannot be empty.");
            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new ArgumentException("Password cannot be empty.");

            _config.Database = databaseName;

            string dir = Path.GetDirectoryName(destinationZipPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // ── Ensure D:\backup exists ──
            if (!Directory.Exists(BackupTempFolder))
                Directory.CreateDirectory(BackupTempFolder);

            string zipBaseName = Path.GetFileNameWithoutExtension(destinationZipPath);
            string tempSql = Path.Combine(BackupTempFolder, zipBaseName + ".sql");
            string progressLog = GetProgressLogPath(zipBaseName);

            try
            {
                LogProgress(progressLog, "========================================");
                LogProgress(progressLog, "BACKUP STARTED");
                LogProgress(progressLog, string.Format("Database       : {0}", databaseName));
                LogProgress(progressLog, string.Format("Destination    : {0}", destinationZipPath));
                LogProgress(progressLog, string.Format("Temp SQL file  : {0}", tempSql));
                LogProgress(progressLog, "========================================");

                // ── Step 1: Dump ──
                LogProgress(progressLog, string.Format("[1/2] Dumping database '{0}' to temp file...", databaseName));
                DumpDatabase(tempSql, BackupTempFolder);

                var sqlInfo = new FileInfo(tempSql);
                LogProgress(progressLog, string.Format("      Dump complete. File size: {0:N0} bytes", sqlInfo.Length));

                // ── Step 2: Compress directly to final destination (no .tmp.7z) ──
                LogProgress(progressLog, string.Format("[2/2] Compressing temp file to: {0}", destinationZipPath));

                if (File.Exists(destinationZipPath))
                    File.Delete(destinationZipPath);

                CompressWithSevenZip(tempSql, destinationZipPath, plainPassword);
                LogProgress(progressLog, "      Compression complete.");

                LogProgress(progressLog, "========================================");
                LogProgress(progressLog, "BACKUP FINISHED SUCCESSFULLY.");
                LogProgress(progressLog, "========================================");
            }
            catch (Exception ex)
            {
                LogProgress(progressLog, "========================================");
                LogProgress(progressLog, string.Format("BACKUP FAILED: {0}", ex.Message));
                LogProgress(progressLog, "========================================");
                throw;
            }
            finally
            {
                // ── Clean up temp SQL — progress log stays so user can review it ──
                try { if (File.Exists(tempSql)) File.Delete(tempSql); } catch { }
            }
        }

        // ════════════════════════════════════════════════════════════
        // ── BACKUP MULTIPLE DATABASES INTO ONE ZIP ──
        // ════════════════════════════════════════════════════════════
        public void BackupMultiple(List<string> databaseNames, string destinationZipPath, string plainPassword)
        {
            if (databaseNames == null || databaseNames.Count == 0)
                throw new ArgumentException("No databases selected.");
            if (string.IsNullOrWhiteSpace(destinationZipPath))
                throw new ArgumentException("Destination path cannot be empty.");
            if (string.IsNullOrWhiteSpace(plainPassword))
                throw new ArgumentException("Password cannot be empty.");

            string dir = Path.GetDirectoryName(destinationZipPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!destinationZipPath.EndsWith(".7z", StringComparison.OrdinalIgnoreCase))
                destinationZipPath = destinationZipPath + ".7z";

            // ── Ensure D:\backup exists ──
            if (!Directory.Exists(BackupTempFolder))
                Directory.CreateDirectory(BackupTempFolder);

            string zipBaseName = Path.GetFileNameWithoutExtension(destinationZipPath);
            string progressLog = GetProgressLogPath(zipBaseName);

            var tempSqlFiles = new List<string>();

            string branch = Environment.GetEnvironmentVariable("branch", EnvironmentVariableTarget.Machine)
                         ?? Environment.GetEnvironmentVariable("branch");

            try
            {
                LogProgress(progressLog, "========================================");
                LogProgress(progressLog, "BACKUP MULTIPLE STARTED");
                LogProgress(progressLog, string.Format("Databases      : {0}", string.Join(", ", databaseNames)));
                LogProgress(progressLog, string.Format("Destination    : {0}", destinationZipPath));
                LogProgress(progressLog, string.Format("Temp folder    : {0}", BackupTempFolder));
                if (!string.IsNullOrWhiteSpace(branch))
                    LogProgress(progressLog, string.Format("Branch         : {0}", branch));
                LogProgress(progressLog, "========================================");

                // ── Step 1: Dump each database ──
                for (int i = 0; i < databaseNames.Count; i++)
                {
                    string dbName = databaseNames[i];
                    _config.Database = dbName;

                    string tempSql = string.IsNullOrWhiteSpace(branch)
                        ? Path.Combine(BackupTempFolder,
                            string.Format("{0}_{1:yyyy_MM_dd}.sql", dbName, DateTime.Now))
                        : Path.Combine(BackupTempFolder,
                            string.Format("{0}_{1}_{2:yyyy_MM_dd}.sql", dbName, branch, DateTime.Now));

                    if (File.Exists(tempSql))
                        File.Delete(tempSql);

                    LogProgress(progressLog, string.Format(
                        "[{0}/{1}] Dumping '{2}' → {3}",
                        i + 1, databaseNames.Count, dbName, tempSql));

                    DumpDatabase(tempSql, BackupTempFolder);

                    var sqlInfo = new FileInfo(tempSql);
                    if (!sqlInfo.Exists || sqlInfo.Length == 0)
                    {
                        string errMsg = string.Format(
                            "mysqldump produced an empty file for '{0}'.\nCheck the log at: {1}",
                            dbName, BackupTempFolder);
                        LogProgress(progressLog, string.Format("      ERROR: {0}", errMsg));
                        throw new Exception(errMsg);
                    }

                    LogProgress(progressLog, string.Format(
                        "      Dump complete. File size: {0:N0} bytes", sqlInfo.Length));

                    tempSqlFiles.Add(tempSql);
                }

                // ── Step 2: Compress all SQL files directly into the final zip (no .tmp.7z) ──
                LogProgress(progressLog, string.Format(
                    "[{0}/{0}] Compressing {1} file(s) into: {2}",
                    databaseNames.Count + 1, tempSqlFiles.Count, destinationZipPath));

                if (File.Exists(destinationZipPath))
                    File.Delete(destinationZipPath);

                CompressMultipleWithSevenZip(tempSqlFiles, destinationZipPath, plainPassword);
                LogProgress(progressLog, "      Compression complete.");

                LogProgress(progressLog, "========================================");
                LogProgress(progressLog, "BACKUP MULTIPLE FINISHED SUCCESSFULLY.");
                LogProgress(progressLog, string.Format("Archive: {0}", destinationZipPath));
                LogProgress(progressLog, "========================================");
            }
            catch (Exception ex)
            {
                LogProgress(progressLog, "========================================");
                LogProgress(progressLog, string.Format("BACKUP FAILED: {0}", ex.Message));
                LogProgress(progressLog, "========================================");
                throw;
            }
            finally
            {
                // ── Clean up temp SQL files — progress log stays for review ──
                foreach (var f in tempSqlFiles)
                {
                    try { if (File.Exists(f)) File.Delete(f); } catch { }
                }
            }
        }

        // ── RESTORE ──
        public void Restore(string sourceZipPath, string targetDatabase)
        {
            if (string.IsNullOrWhiteSpace(sourceZipPath))
                throw new ArgumentException("Source path cannot be empty.");
            if (!File.Exists(sourceZipPath))
                throw new FileNotFoundException("Backup file not found.", sourceZipPath);
            if (string.IsNullOrWhiteSpace(targetDatabase))
                throw new Exception("No target database selected.");

            string encryptedPassword = ConfigurationManager.AppSettings["EncrpytedPassword"];
            if (string.IsNullOrWhiteSpace(encryptedPassword))
                throw new Exception(
                    "No encrypted password found in App.config.\n\n" +
                    "Please add 'EncrpytedPassword' key under <appSettings>.");

            string plainPassword = _crypto.Decrypt(encryptedPassword, "pullasciiencrypt");

            _config.Database = targetDatabase;

            string tempFolder = @"D:\backup\restore\" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(tempFolder);

            string sqlFile;

            if (sourceZipPath.EndsWith(".sql", StringComparison.OrdinalIgnoreCase))
            {
                sqlFile = sourceZipPath;
            }
            else
            {
                ExtractWithSevenZip(sourceZipPath, tempFolder, plainPassword);

                var sqlFiles = Directory.GetFiles(tempFolder, "*.sql", SearchOption.AllDirectories);
                if (sqlFiles.Length == 0)
                    throw new FileNotFoundException("No .sql file found inside the backup archive.");

                sqlFile = sqlFiles[0];
            }

            RestoreDatabase(sqlFile, sourceZipPath);

            try { Directory.Delete(tempFolder, true); } catch { 
            }
            finally
{
        // Always clean up temp folder, success or failure
             try { Directory.Delete(tempFolder, true); } catch { }
}
        }

        // ── RESTORE SPECIFIC DATABASE FROM ZIP ──
        public void RestoreSpecific(string sourceZipPath, string targetDatabase, string sqlFileName)
        {
            if (string.IsNullOrWhiteSpace(sourceZipPath))
                throw new ArgumentException("Source path cannot be empty.");
            if (!File.Exists(sourceZipPath))
                throw new FileNotFoundException("Backup file not found.", sourceZipPath);
            if (string.IsNullOrWhiteSpace(targetDatabase))
                throw new Exception("No target database selected.");
            if (string.IsNullOrWhiteSpace(sqlFileName))
                throw new Exception("No database selected from zip.");

            string encryptedPassword = ConfigurationManager.AppSettings["EncrpytedPassword"];
            if (string.IsNullOrWhiteSpace(encryptedPassword))
                throw new Exception("No encrypted password found in App.config.");

            string plainPassword = _crypto.Decrypt(encryptedPassword, "pullasciiencrypt");

            _config.Database = targetDatabase;

            string tempFolder = @"D:\backup\restore\" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(tempFolder);

            string args = string.Format(
                "e \"{0}\" -o\"{1}\" \"-p{2}\" -y",
                sourceZipPath, tempFolder, plainPassword);

            RunProcessGui(_sevenZipGuiExe, args, "7-Zip extraction failed", sourceZipPath);

            string sqlFile = null;

            string exactMatch = Path.Combine(tempFolder, sqlFileName + ".sql");
            if (File.Exists(exactMatch))
            {
                sqlFile = exactMatch;
            }
            else
            {
                var matched = Directory.GetFiles(tempFolder, sqlFileName + "*.sql");
                if (matched.Length > 0)
                    sqlFile = matched[0];
            }

            if (sqlFile == null)
                throw new FileNotFoundException(
                    string.Format("Could not find {0}.sql in the archive.", sqlFileName));

            RestoreDatabase(sqlFile, sourceZipPath);

            try { Directory.Delete(tempFolder, true); } catch { 
            }
            finally
            {
                try { Directory.Delete(tempFolder, true); } catch { }
            }
        }

        // ════════════════════════════════════════════
        // ── PRIVATE HELPERS ──
        // ════════════════════════════════════════════

        private void DumpDatabase(string outputSqlPath, string logRef)
        {
            string sqlDir = Path.GetDirectoryName(outputSqlPath);
            if (!string.IsNullOrEmpty(sqlDir) && !Directory.Exists(sqlDir))
                Directory.CreateDirectory(sqlDir);

            string args = string.Format(
                "--host={0} --port={1} --user={2} --password={3} " +
                "--single-transaction --routines --triggers -v \"{4}\"",
                _config.Server, _config.Port, _config.UserId,
                _config.Password, _config.Database);

            var psi = new ProcessStartInfo
            {
                FileName = _mysqlDumpExe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string stderr = string.Empty;
            int exitCode = 0;

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();

                // ── Read stderr on a separate thread to prevent deadlock ──
                var stderrTask = System.Threading.Tasks.Task.Run(
                    () => process.StandardError.ReadToEnd());

                // ── Read stdout (the actual SQL dump) directly to file ──
                using (var fs = new FileStream(outputSqlPath, FileMode.Create, FileAccess.Write))
                {
                    process.StandardOutput.BaseStream.CopyTo(fs);
                }

                stderr = stderrTask.Result;
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            WriteLog(logRef, "mysqldump", _mysqlDumpExe, exitCode, stderr);

            if (exitCode != 0)
                throw new Exception(string.Format(
                    "mysqldump failed (exit {0}):\n{1}", exitCode, stderr));
        }

        // ════════════════════════════════════════════════════════════
        // ── RESTORE DATABASE
        //
        //    Handles big data (10 GB+) by:
        //      1. Stripping USE statements from the SQL file
        //      2. Running mysql.exe via ProcessStartInfo with stdin
        //         stream fed from FileStream in a background thread
        //         — avoids pipe deadlock and buffer limits
        //      3. Key large-file flags:
        //           --max_allowed_packet=512M  → big INSERT rows
        //           --net_read_timeout=3600    → 1 hr read timeout
        //           --connect_timeout=3600     → 1 hr connect timeout
        //           --init-command             → sets session packet size
        // ════════════════════════════════════════════════════════════
        private void RestoreDatabase(string sqlFilePath, string logRef)
        {
            // ── Ensure the target database exists ──
            string connStr = string.Format(
                "Server={0};Port={1};Uid={2};Password={3};",
                _config.Server, _config.Port, _config.UserId, _config.Password);

            using (var conn = new MySqlConnection(connStr))
            {
                conn.Open();

                // Increase packet size for large restores
                using (var cmd = new MySqlCommand("SET GLOBAL max_allowed_packet=536870912;", conn))
                {
                    try { cmd.ExecuteNonQuery(); } catch { /* ignore if no SUPER privilege */ }
                }

                using (var cmd = new MySqlCommand(
                    string.Format("CREATE DATABASE IF NOT EXISTS `{0}`;", _config.Database), conn))
                    cmd.ExecuteNonQuery();
            }

            // ── Strip USE statements into a clean SQL file ──
            string cleanSqlPath = sqlFilePath + ".clean.sql";
            using (var reader = new StreamReader(sqlFilePath, Encoding.UTF8))
            using (var writer = new StreamWriter(cleanSqlPath, false, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.TrimStart().StartsWith("USE ", StringComparison.OrdinalIgnoreCase))
                        continue;
                    writer.WriteLine(line);
                }
            }

            // ── Build mysql.exe arguments with all large-file flags ──
            string args = string.Format(
             "--host={0} --port={1} --user={2} --password={3}" +
             " --max_allowed_packet=512M" +
             " --connect_timeout=3600" +
             " {4}",
                _config.Server,
                _config.Port,
                _config.UserId,
                _config.Password,
                 _config.Database);

            var psi = new ProcessStartInfo
            {
                FileName = _mysqlExe,
                Arguments = args,
                RedirectStandardInput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string stderr = string.Empty;
            int exitCode = 0;

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();

                // ── Read stderr asynchronously to prevent pipe deadlock ──
                var stderrTask = System.Threading.Tasks.Task.Run(
                    () => process.StandardError.ReadToEnd());

                // ── Feed the SQL file into stdin on a background thread
                //    using a large 4 MB buffer — avoids blocking the main
                //    thread and handles files of any size safely ──
                var stdinTask = System.Threading.Tasks.Task.Run(() =>
                {
                    try
                    {
                        using (var fs = new FileStream(
                            cleanSqlPath,
                            FileMode.Open,
                            FileAccess.Read,
                            FileShare.Read,
                            bufferSize: 4 * 1024 * 1024))   // 4 MB buffer
                        {
                            fs.CopyTo(process.StandardInput.BaseStream);
                        }
                    }
                    catch { /* process may have exited on error — ignore */ }
                    finally
                    {
                        try { process.StandardInput.Close(); } catch { }
                    }
                });

                // ── Wait for stdin feed to finish, then collect stderr ──
                stdinTask.Wait();
                stderr = stderrTask.Result;
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            WriteLog(logRef, "mysql restore", _mysqlExe, exitCode, stderr);

            // ── Clean up temp file ──
            try { if (File.Exists(cleanSqlPath)) File.Delete(cleanSqlPath); } catch { }

            if (exitCode != 0)
                throw new Exception(string.Format(
                    "mysql restore failed (exit {0}):\n{1}", exitCode, stderr));
        }

        private void CompressWithSevenZip(string sourceFile, string destZip, string password)
        {
            string args = string.Format(
                "a -t7z -m0=lzma2 -ms=on -md=32m -mhe=on -mx=7 -p{0} \"{1}\" \"{2}\"",
                password, destZip, sourceFile);

            RunProcessGui(_sevenZipGuiExe, args, "7-Zip compression failed", destZip);
        }

        private void CompressMultipleWithSevenZip(List<string> sourceFiles, string destZip, string password)
        {
            var quotedFiles = new StringBuilder();
            foreach (var f in sourceFiles)
                quotedFiles.AppendFormat(" \"{0}\"", f);

            string args = string.Format(
                "a -t7z -m0=lzma2 -ms=on -md=32m -mhe=on -mx=7 \"-p{0}\" \"{1}\"{2}",
                password, destZip, quotedFiles.ToString());

            RunProcessGui(_sevenZipGuiExe, args, "7-Zip compression failed", destZip);
        }

        private void ExtractWithSevenZip(string sourceZip, string destFolder, string password)
        {
            string args = string.Format(
                "e \"{0}\" -o\"{1}\" \"-p{2}\" -y",
                sourceZip, destFolder, password);

            RunProcessGui(_sevenZipGuiExe, args, "7-Zip extraction failed", sourceZip);
        }

        // ── Main log writer (verbose/error log per backup job) ──
        private static void WriteLog(string zipPath, string section, string exe,
                                     int exitCode, string stderr)
        {
            try
            {
                string dbname = Environment.GetEnvironmentVariable("dbname", EnvironmentVariableTarget.Machine)
                             ?? Environment.GetEnvironmentVariable("dbname")
                             ?? "unknown_db";

                string logFileName = string.Format("{0}_{1:yyyy_MM_dd}.log", dbname, DateTime.Now);

                string logFolder = Directory.Exists(Path.GetDirectoryName(zipPath) ?? string.Empty)
                    ? Path.GetDirectoryName(zipPath)
                    : BackupTempFolder;

                string logPath = Path.Combine(logFolder, logFileName);

                var sb = new StringBuilder();
                sb.AppendLine(string.Format("=== {0} log: {1} ===",
                    section, DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss tt").ToLower()));
                sb.AppendLine(string.Format("EXE : {0}", exe));
                sb.AppendLine(string.Format("EXIT: {0}", exitCode));

                if (!string.IsNullOrWhiteSpace(stderr))
                {
                    sb.AppendLine("--- STDERR (verbose) ---");
                    sb.AppendLine(stderr.TrimEnd());
                }

                sb.AppendLine();
                File.AppendAllText(logPath, sb.ToString(), Encoding.UTF8);
            }
            catch { /* never let logging crash the backup */ }
        }

        // ── Silent runner for mysqldump / mysql ──
        private static void RunProcessWithLog(string exe, string args, string errorMsg,
                                              string logRef, string section)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            string stdout = string.Empty;
            string stderr = string.Empty;
            int exitCode = 0;

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();
                stdout = process.StandardOutput.ReadToEnd();
                stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            string combined = string.Empty;
            if (!string.IsNullOrWhiteSpace(stdout)) combined += stdout.TrimEnd();
            if (!string.IsNullOrWhiteSpace(stderr))
            {
                if (!string.IsNullOrWhiteSpace(combined)) combined += Environment.NewLine;
                combined += stderr.TrimEnd();
            }

            WriteLog(logRef, section, exe, exitCode, combined);

            if (exitCode != 0)
                throw new Exception(string.Format(
                    "{0} (exit {1}):\nSTDOUT: {2}\nSTDERR: {3}",
                    errorMsg, exitCode, stdout, stderr));
        }

        // ── GUI runner for 7zg.exe / 7z.exe ──
        private static void RunProcessGui(string exe, string args, string errorMsg, string logRef)
        {
            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                UseShellExecute = true,
                CreateNoWindow = false,
                WindowStyle = ProcessWindowStyle.Minimized
            };

            int exitCode = 0;

            using (var process = new Process { StartInfo = psi })
            {
                process.Start();
                try { process.WaitForInputIdle(5000); } catch { }
                process.WaitForExit();
                exitCode = process.ExitCode;
            }

            string logNote = exitCode == 0
                ? "(GUI mode — progress shown in 7zg window)"
                : string.Format("(GUI mode — exit {0})", exitCode);

            WriteLog(logRef, "7-zip", exe, exitCode, logNote);

            if (exitCode != 0)
                throw new Exception(string.Format(
                    "{0} (exit {1}): Check the 7-Zip window for details.", errorMsg, exitCode));
        }
    }
}