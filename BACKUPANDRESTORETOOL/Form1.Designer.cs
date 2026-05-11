
namespace BACKUPANDRESTORETOOL
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.button2 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.checkedListBox1 = new System.Windows.Forms.CheckedListBox();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.checkedListBox2 = new System.Windows.Forms.CheckedListBox();
            this.button6 = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.comboBox1 = new System.Windows.Forms.ComboBox();
            this.textBox6 = new System.Windows.Forms.TextBox();
            this.button4 = new System.Windows.Forms.Button();
            this.textBox5 = new System.Windows.Forms.TextBox();
            this.textBox4 = new System.Windows.Forms.TextBox();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            this.labelStatus = new System.Windows.Forms.Label();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Location = new System.Drawing.Point(12, 12);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(587, 446);
            this.tabControl1.SizeMode = System.Windows.Forms.TabSizeMode.FillToRight;
            this.tabControl1.TabIndex = 0;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.button2);
            this.tabPage1.Controls.Add(this.button3);
            this.tabPage1.Controls.Add(this.button1);
            this.tabPage1.Controls.Add(this.textBox3);
            this.tabPage1.Controls.Add(this.textBox2);
            this.tabPage1.Controls.Add(this.checkedListBox1);
            this.tabPage1.Controls.Add(this.textBox1);
            this.tabPage1.Location = new System.Drawing.Point(4, 25);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(579, 417);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Backup";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(415, 350);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(138, 39);
            this.button2.TabIndex = 2;
            this.button2.Text = "Cancel";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // button3
            // 
            this.button3.Location = new System.Drawing.Point(499, 298);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(40, 29);
            this.button3.TabIndex = 4;
            this.button3.Text = "...";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Location = new System.Drawing.Point(271, 350);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(138, 39);
            this.button1.TabIndex = 1;
            this.button1.Text = "Export";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(17, 301);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(473, 22);
            this.textBox3.TabIndex = 3;
            this.textBox3.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(17, 273);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(130, 22);
            this.textBox2.TabIndex = 2;
            this.textBox2.Text = "Export Path";
            this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
            // 
            // checkedListBox1
            // 
            this.checkedListBox1.FormattingEnabled = true;
            this.checkedListBox1.Location = new System.Drawing.Point(17, 59);
            this.checkedListBox1.Name = "checkedListBox1";
            this.checkedListBox1.Size = new System.Drawing.Size(519, 208);
            this.checkedListBox1.TabIndex = 1;
            this.checkedListBox1.SelectedIndexChanged += new System.EventHandler(this.checkedListBox1_SelectedIndexChanged);
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(16, 21);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(130, 22);
            this.textBox1.TabIndex = 0;
            this.textBox1.Text = "Database to Export";
            this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.checkedListBox2);
            this.tabPage2.Controls.Add(this.button6);
            this.tabPage2.Controls.Add(this.button5);
            this.tabPage2.Controls.Add(this.comboBox1);
            this.tabPage2.Controls.Add(this.textBox6);
            this.tabPage2.Controls.Add(this.button4);
            this.tabPage2.Controls.Add(this.textBox5);
            this.tabPage2.Controls.Add(this.textBox4);
            this.tabPage2.Location = new System.Drawing.Point(4, 25);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(579, 417);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Restore";
            this.tabPage2.UseVisualStyleBackColor = true;
            this.tabPage2.Click += new System.EventHandler(this.tabPage2_Click);
            // 
            // checkedListBox2
            // 
            this.checkedListBox2.FormattingEnabled = true;
            this.checkedListBox2.Location = new System.Drawing.Point(19, 94);
            this.checkedListBox2.Name = "checkedListBox2";
            this.checkedListBox2.Size = new System.Drawing.Size(527, 140);
            this.checkedListBox2.TabIndex = 7;
            this.checkedListBox2.SelectedIndexChanged += new System.EventHandler(this.checkedListBox2_SelectedIndexChanged);
            // 
            // button6
            // 
            this.button6.Location = new System.Drawing.Point(408, 345);
            this.button6.Name = "button6";
            this.button6.Size = new System.Drawing.Size(138, 39);
            this.button6.TabIndex = 6;
            this.button6.Text = "Cancel";
            this.button6.UseVisualStyleBackColor = true;
            this.button6.Click += new System.EventHandler(this.button6_Click_1);
            // 
            // button5
            // 
            this.button5.BackColor = System.Drawing.Color.DeepSkyBlue;
            this.button5.ForeColor = System.Drawing.Color.White;
            this.button5.Location = new System.Drawing.Point(254, 345);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(138, 39);
            this.button5.TabIndex = 5;
            this.button5.Text = "Import";
            this.button5.UseVisualStyleBackColor = false;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // comboBox1
            // 
            this.comboBox1.FormattingEnabled = true;
            this.comboBox1.Location = new System.Drawing.Point(18, 296);
            this.comboBox1.Name = "comboBox1";
            this.comboBox1.Size = new System.Drawing.Size(528, 24);
            this.comboBox1.TabIndex = 4;
            this.comboBox1.Text = "- Select Schema -";
            this.comboBox1.SelectedIndexChanged += new System.EventHandler(this.comboBox1_SelectedIndexChanged);
            // 
            // textBox6
            // 
            this.textBox6.Location = new System.Drawing.Point(18, 250);
            this.textBox6.Name = "textBox6";
            this.textBox6.Size = new System.Drawing.Size(140, 22);
            this.textBox6.TabIndex = 3;
            this.textBox6.Text = "Target Schema";
            this.textBox6.TextChanged += new System.EventHandler(this.textBox6_TextChanged);
            // 
            // button4
            // 
            this.button4.Location = new System.Drawing.Point(510, 49);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(38, 33);
            this.button4.TabIndex = 2;
            this.button4.Text = "...";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click_1);
            // 
            // textBox5
            // 
            this.textBox5.Location = new System.Drawing.Point(18, 55);
            this.textBox5.Name = "textBox5";
            this.textBox5.Size = new System.Drawing.Size(473, 22);
            this.textBox5.TabIndex = 1;
            this.textBox5.TextChanged += new System.EventHandler(this.textBox5_TextChanged);
            // 
            // textBox4
            // 
            this.textBox4.Location = new System.Drawing.Point(18, 18);
            this.textBox4.Name = "textBox4";
            this.textBox4.Size = new System.Drawing.Size(140, 22);
            this.textBox4.TabIndex = 0;
            this.textBox4.Text = "Select Database File";
            this.textBox4.TextChanged += new System.EventHandler(this.textBox4_TextChanged);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(16, 464);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(579, 25);
            this.progressBar1.TabIndex = 1;
            this.progressBar1.Click += new System.EventHandler(this.progressBar1_Click);
            // 
            // labelStatus
            // 
            this.labelStatus.AutoSize = true;
            this.labelStatus.Location = new System.Drawing.Point(135, 502);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(20, 17);
            this.labelStatus.TabIndex = 2;
            this.labelStatus.Text = "...";
            this.labelStatus.Click += new System.EventHandler(this.label1_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(619, 528);
            this.Controls.Add(this.labelStatus);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.tabControl1);
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Backup and Restore Tool";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.CheckedListBox checkedListBox1;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.TextBox textBox4;
        private System.Windows.Forms.TextBox textBox5;
        private System.Windows.Forms.Button button6;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.ComboBox comboBox1;
        private System.Windows.Forms.TextBox textBox6;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.CheckedListBox checkedListBox2;
        private System.Windows.Forms.ProgressBar progressBar1;
        private System.Windows.Forms.Label labelStatus;
    }
}

