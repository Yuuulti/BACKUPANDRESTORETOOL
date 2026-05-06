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

        public Form1()
        {
            InitializeComponent();
            _controller = new BackupRestoreController();

            // Make label textboxes read-only
            textBox1.ReadOnly = true; // "Database to Export"
            textBox2.ReadOnly = true; // "Export Path"
            textBox4.ReadOnly = true; // "Select Database File"
            textBox6.ReadOnly = true; // "Target Schema"

            // Path fields filled by browse dialogs
            textBox3.ReadOnly = true;
            textBox5.ReadOnly = true;

            // FIX 1: Make comboBox1 read-only (selection only, no typing)
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;

            // FIX 2: Allow only one checked item at a time in checkedListBox2
            checkedListBox2.ItemCheck += checkedListBox2_ItemCheck;

            this.Load += Form1_Load;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // ── Validate environment FIRST before anything else ──
            string envError = _controller.ValidateEnvironment();
            if (envError != null)
            {
                MessageBox.Show(envError, "Setup Required",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);

                // Disable backup/restore buttons so user can't proceed
                button1.Enabled = false; // Export
                button5.Enabled = false; // Import
            }

            // Set the default export path
            textBox3.Text = @"D:\backup";
            textBox5.Text = string.Empty;
            LoadDatabases();
        }
        private void LoadDatabases()
        {
            try
            {
                checkedListBox1.Items.Clear();
                comboBox1.Items.Clear();
                comboBox1.Items.Add("- Select Schema -");
                comboBox1.SelectedIndex = 0;

                var databases = _controller.GetDatabases();
                foreach (var db in databases)
                {
                    checkedListBox1.Items.Add(db);
                    comboBox1.Items.Add(db);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Failed to load databases:\n\n" + ex.Message,
                    "Connection Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // FIX 2: Only allow one checked item at a time in checkedListBox2
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

        // button3 — Browse export folder
        private void button3_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select Export Folder";
                if (dlg.ShowDialog() == DialogResult.OK)
                    textBox3.Text = dlg.SelectedPath;
            }
        }

        // button1 — Export
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

            // ── Safe branch name — remove invalid filename characters ──
            string branch = Environment.GetEnvironmentVariable("branch", EnvironmentVariableTarget.Machine)
                            ?? Environment.GetEnvironmentVariable("branch")
                            ?? "backup";  // ← safe default instead of "please config environment variable"

            // ── Strip any invalid filename characters from branch ──
            foreach (char c in Path.GetInvalidFileNameChars())
                branch = branch.Replace(c.ToString(), "_");

            var selectedDbs = new List<string>();
            foreach (var item in checkedListBox1.CheckedItems)
                selectedDbs.Add(item.ToString());

            string fileName = string.Format("{0}_{1}.7z",
                branch, DateTime.Now.ToString("yyyy_MM_dd"));

            string destPath = Path.Combine(textBox3.Text, fileName);

            // ── Show exactly what will be created ──
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

        // button2 — Cancel Backup (FIX 3: close the form like X button)
        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ── RESTORE TAB ──

        // button4 — Browse backup file + populate checkedListBox2 with databases inside zip
        private void button4_Click_1(object sender, EventArgs e)
        {
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "Select Database Backup File";
                dlg.Filter = "Backup Files (*.zip;*.7z)|*.zip;*.7z|All Files (*.*)|*.*";
                if (dlg.ShowDialog() != DialogResult.OK) return;

                textBox5.Text = dlg.FileName;

                // ★ Read databases inside the zip and show in checkedListBox2
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

        // button5 — Import checked databases from zip into target schema
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

            // ★ Check at least one database selected from zip
            if (checkedListBox2.CheckedItems.Count == 0)
            {
                MessageBox.Show("Please check at least one database to restore.",
                    "No Database Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (comboBox1.SelectedIndex <= 0)
            {
                MessageBox.Show("Please select a target schema to restore into.",
                    "No Schema Selected", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string targetDatabase = comboBox1.SelectedItem.ToString();

            var confirm = MessageBox.Show(
                string.Format("WARNING: This will OVERWRITE the '{0}' database.\n\n" +
                    "Are you sure you want to continue?", targetDatabase),
                "Confirm Import", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (confirm != DialogResult.Yes) return;

            SetUiBusy(true);
            try
            {
                string sourceFile = textBox5.Text;

                // ★ Restore each checked database from the zip into the target schema
                foreach (var item in checkedListBox2.CheckedItems)
                {
                    string dbToRestore = item.ToString();
                    await Task.Run(() =>
                        _controller.RestoreSpecific(sourceFile, targetDatabase, dbToRestore));
                }

                MessageBox.Show(
                    string.Format("Import completed successfully!\n\nRestored into: {0}", targetDatabase),
                    "Import Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Import Failed",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally { SetUiBusy(false); }
        }

        // button6 — Cancel Restore (FIX 3: close the form like X button)
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

        private void checkedListBox2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button6_Click_1(object sender, EventArgs e)
        {
            this.Close();
        }

        private void tabPage2_Click(object sender, EventArgs e)
        {

        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }
    }
}