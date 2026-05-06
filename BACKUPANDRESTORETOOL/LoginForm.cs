using System;
using System.Configuration;
using System.Windows.Forms;
using BACKUPANDRESTORETOOL.Services;

namespace BACKUPANDRESTORETOOL
{
    public partial class LoginForm : Form
    {
        private ACryptoServiceProvider crypto = new ACryptoServiceProvider();
        private const string IV = "pullasciiencrypt"; // same IV used in BackupRestoreController

        public LoginForm()
        {
            InitializeComponent();
            button1.Click += new EventHandler(button1_Click);
            button2.Click += new EventHandler(button2_Click);
            textBox1.PasswordChar = '*';
            this.AcceptButton = button1;
        }

        private void LoginForm_Load(object sender, EventArgs e)
        {
            label2.Text = "";
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            label2.Text = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                label2.ForeColor = System.Drawing.Color.Red;
                label2.Text = "Please enter password!";
                return;
            }

            string encryptedStored = ConfigurationManager.AppSettings["LoginPassword"];
            string decryptedPassword = crypto.Decrypt(encryptedStored, IV);

            if (textBox1.Text == decryptedPassword)
            {
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                {
                    MessageBox.Show("Incorrect password!", "Access Denied",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    textBox1.Clear();
                    textBox1.Focus();
                }

                textBox1.Clear();
                textBox1.Focus();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
    }
}