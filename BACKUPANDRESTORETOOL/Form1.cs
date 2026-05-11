using System;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Windows.Forms;
using BACKUPANDRESTORETOOL.Controller;
using BACKUPANDRESTORETOOL.Services;

namespace BACKUPANDRESTORETOOL
{
    public partial class Form1 : Form
    {
        private readonly BackupRestoreController _controller;
        private HashSet<string> _allowedDbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
     

        public Form1()
        {
            InitializeComponent();
            _controller = new BackupRestoreController();

            progressBar1.Visible = false;
            progressBar1.Style = System.Windows.Forms.ProgressBarStyle.Continuous;
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;

            textBox1.ReadOnly = true;
            textBox2.ReadOnly = true;
            textBox4.ReadOnly = true;
            textBox6.ReadOnly = true;
            textBox3.ReadOnly = true;
            textBox5.ReadOnly = true;

            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;

            checkedListBox2.ItemCheck += checkedListBox2_ItemCheck;
            checkedListBox1.ItemCheck += checkedListBox1_ItemCheck;

            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string envError = _controller.ValidateEnvironment();
            if (envError != null)
            {
                MessageBox.Show(envError, "Setup Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                button1.Enabled = false;
                button5.Enabled = false;
            }

            textBox3.Text = @"D:\backup";
            textBox5.Text = string.Empty;

            LoadDatabases();
            LoadTargetSchemas();
        }

        private void LoadDatabases()
        {
            try
            {
                checkedListBox1.Items.Clear();

                var allDatabases = _controller.GetAllDatabases();

                var allowedDbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var varName in new[] { "dbname", "dbname2", "dbname3" })
                {
                    string val = Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Machine)
                              ?? Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.User)
                              ?? Environment.GetEnvironmentVariable(varName, EnvironmentVariableTarget.Process);

                    if (!string.IsNullOrWhiteSpace(val))
                    {
                        foreach (var db in val.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                        {
                            string trimmed = db.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed))
                                allowedDbs.Add(trimmed);
                        }
                    }
                }

                _allowedDbs = allowedDbs;

                foreach (var db in allDatabases)
                    checkedListBox1.Items.Add(db);

                for (int i = 0; i < checkedListBox1.Items.Count; i++)
                {
                    if (!_allowedDbs.Contains(checkedListBox1.Items[i].ToString()))
                        checkedListBox1.SetItemCheckState(i, CheckState.Unchecked);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load databases:\n\n" + ex.Message,
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkedListBox1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            string itemName = checkedListBox1.Items[e.Index].ToString();
            if (!_allowedDbs.Contains(itemName))
                e.NewValue = CheckState.Unchecked;
        }

        private void LoadTargetSchemas()
        {
            try
            {
                string currentText = comboBox1.Text;

                comboBox1.Items.Clear();
                comboBox1.Items.Add("- Select Schema -");

                var allDbs = _controller.GetAllDatabases();
                foreach (var db in allDbs)
                    comboBox1.Items.Add(db);

                if (!string.IsNullOrWhiteSpace(currentText))
                    comboBox1.Text = currentText;
                else
                    comboBox1.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load schemas:\n\n" + ex.Message,
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void checkedListBox2_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            if (e.NewValue == CheckState.Checked)
            {
                for (int i = 0; i < checkedListBox2.Items.Count; i++)
                {
                    if (i != e.Index)
                        checkedListBox2.SetItemChecked(i, false);
                }
            }
        }

        // ── BACKUP TAB ──

        private void button3_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Export Folder";
                if (dlg.ShowDialog() == DialogResult.OK)
                    textBox3.Text = dlg.SelectedPath;
            }
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (checkedListBox1.CheckedItems.Count == 0)
            {
                MessageBox.Show("Please check at least one database to export.",
                    "No Database Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(textBox3.Text) || !Directory.Exists(textBox3.Text))
            {
                MessageBox.Show("Please select a valid export folder.",
                    "Invalid Export Path", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string encryptedPassword = ConfigurationManager.AppSettings["EncrpytedPassword"];
            if (string.IsNullOrWhiteSpace(encryptedPassword))
            {
                MessageBox.Show(
                    "No encrypted password found in App.config.\n\nPlease add 'EncrpytedPassword' key under <appSettings>.",
                    "No Password", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var crypto = new ACryptoServiceProvider();
            string plainPassword = crypto.Decrypt(encryptedPassword, "pullasciiencrypt");

            string branch = Environment.GetEnvironmentVariable("branch", EnvironmentVariableTarget.Machine)
                            ?? Environment.GetEnvironmentVariable("branch")
                            ?? "backup";

            foreach (char c in Path.GetInvalidFileNameChars())
                branch = branch.Replace(c.ToString(), "_");

            var selectedDbs = new List<string>();
            foreach (var item in checkedListBox1.CheckedItems)
            {
                int idx = checkedListBox1.Items.IndexOf(item);
                if (checkedListBox1.GetItemCheckState(idx) == CheckState.Checked)
                    selectedDbs.Add(item.ToString());
            }

            string fileName = string.Format("{0}_{1}.7z",
                branch, DateTime.Now.ToString("yyyy_MM_dd"));

            string destPath = Path.Combine(textBox3.Text, fileName);

            var confirm = MessageBox.Show(
                string.Format("Starting backup of {0} database(s).\n\nOutput file:\n{1}\n\nContinue?",
                    selectedDbs.Count, destPath),
                "Confirm Backup", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes) return;

            SetUiBusy(true);
            try
            {
                await Task.Run(() => _controller.BackupMultiple(selectedDbs, destPath, plainPassword));

                MessageBox.Show(
                    string.Format("Export completed!\n\n{0} database(s) backed up to:\n{1}",
                        selectedDbs.Count, destPath),
                    "Export Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetUiBusy(false); }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ── RESTORE TAB ──

        private void button4_Click_1(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Database Backup File";
                dlg.Filter = "Backup Files (*.zip;*.7z)|*.zip;*.7z|All Files (*.*)|*.*";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                textBox5.Text = dlg.FileName;

                try
                {
                    var dbsInZip = _controller.GetDatabasesFromZip(dlg.FileName);
                    checkedListBox2.Items.Clear();
                    foreach (var db in dbsInZip)
                        checkedListBox2.Items.Add(db);

                    if (dbsInZip.Count == 0)
                        MessageBox.Show("No .sql files found inside this backup.",
                            "No Databases Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to read backup file:\n\n" + ex.Message,
                        "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox5.Text))
            {
                MessageBox.Show("Please select a database backup file.",
                    "No File Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!File.Exists(textBox5.Text))
            {
                MessageBox.Show("The selected backup file does not exist.",
                    "File Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (checkedListBox2.CheckedItems.Count == 0)
            {
                MessageBox.Show("Please check at least one database to restore.",
                    "No Database Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string targetDatabase = comboBox1.Text?.Trim();

            if (string.IsNullOrWhiteSpace(targetDatabase) || targetDatabase == "- Select Schema -")
            {
                MessageBox.Show("Please select or type a target schema name.",
                    "No Schema Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(targetDatabase, @"^[a-zA-Z0-9_]+$"))
            {
                MessageBox.Show("Schema name can only contain letters, numbers, and underscores.",
                    "Invalid Schema Name", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var confirm = MessageBox.Show(
                string.Format("WARNING: This will OVERWRITE the '{0}' database.\n\n" +
                    "Are you sure you want to continue?", targetDatabase),
                "Confirm Import", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            SetUiBusy(true);
            progressBar1.Visible = true;
            try
            {
                string sourceFile = textBox5.Text;

                foreach (var item in checkedListBox2.CheckedItems)
                {
                    string dbToRestore = item.ToString();

                    // Stage 1 - Extracting
                    labelStatus.Text = string.Format("Extracting {0}...", dbToRestore);
                    labelStatus.ForeColor = System.Drawing.Color.DarkOrange;
                    await Task.Run(() =>
                        _controller.ExtractOnly(sourceFile, dbToRestore));

                    // Stage 2 - Restoring
                    // PALITAN NG:
                    progressBar1.Value = 0;
                    labelStatus.Text = string.Format("Restoring {0} into {1}... ~0%", dbToRestore, targetDatabase);
                    labelStatus.ForeColor = System.Drawing.Color.DarkBlue;

                    var restoreProgress = new Progress<int>(percent =>
                    {
                        // Hanggang 95% lang — hindi mag-100% hanggang hindi talaga tapos
                        int displayPercent = Math.Min(percent, 95);
                        progressBar1.Value = displayPercent;
                        labelStatus.Text = string.Format(
                            "Restoring {0} into {1}... ~{2}%",
                            dbToRestore, targetDatabase, displayPercent);
                    });

                    await Task.Run(() =>
                        _controller.RestoreOnly(sourceFile, targetDatabase, dbToRestore, restoreProgress));

                    // Tapos na talaga — set to 100
                    progressBar1.Value = 100;
                    labelStatus.Text = string.Format("Restoring {0} into {1}... 100%", dbToRestore, targetDatabase);
                }

                labelStatus.Text = "Restore completed successfully!";
                labelStatus.ForeColor = System.Drawing.Color.Green;

                MessageBox.Show(
                    string.Format("Import completed successfully!\n\nRestored into: {0}", targetDatabase),
                    "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);

                LoadDatabases();
                LoadTargetSchemas();
                comboBox1.Text = targetDatabase;
            }
            catch (Exception ex)
            {
                labelStatus.Text = "Restore failed!";
                labelStatus.ForeColor = System.Drawing.Color.Red;
                MessageBox.Show(ex.Message, "Import Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                progressBar1.Visible = false;
                labelStatus.Text = "";
                SetUiBusy(false);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ── UNUSED DESIGNER EVENTS ───
        private void tabPage1_Click(object sender, EventArgs e) { }
        private void tabPage1_Click_1(object sender, EventArgs e) { }
        private void textBox1_TextChanged(object sender, EventArgs e) { }
        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e) { }
        private void textBox2_TextChanged(object sender, EventArgs e) { }
        private void textBox3_TextChanged(object sender, EventArgs e) { }
        private void textBox4_TextChanged(object sender, EventArgs e) { }
        private void textBox5_TextChanged(object sender, EventArgs e) { }
        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e) { }

        // ── HELPERS ───

        private void SetUiBusy(bool busy)
        {
            button1.Enabled = !busy;
            button2.Enabled = !busy;
            button3.Enabled = !busy;
            button4.Enabled = !busy;
            button5.Enabled = !busy;
            button6.Enabled = !busy;
            tabControl1.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
            Text = busy
                ? "Backup and Restore Tool — Please wait..."
                : "Backup and Restore Tool";
        }

        private void checkedListBox2_SelectedIndexChanged(object sender, EventArgs e) { }

        private void button6_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tabPage2_Click(object sender, EventArgs e) { }

        private void textBox6_TextChanged(object sender, EventArgs e) { }

        private void progressBar1_Click(object sender, EventArgs e) { }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }
}