namespace TaskbarMonitor
{
    partial class OptionForm
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.listTheme = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.editHistorySize = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.listCounters = new System.Windows.Forms.ListBox();
            this.panel3 = new System.Windows.Forms.Panel();
            this.listShowTitle = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.listShowCurrentValue = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.listGraphType = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.editHistorySize)).BeginInit();
            this.panel2.SuspendLayout();
            this.panel3.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.listTheme);
            this.panel1.Controls.Add(this.label4);
            this.panel1.Controls.Add(this.editHistorySize);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(800, 71);
            this.panel1.TabIndex = 0;
            // 
            // listTheme
            // 
            this.listTheme.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listTheme.FormattingEnabled = true;
            this.listTheme.Items.AddRange(new object[] {
            "default"});
            this.listTheme.Location = new System.Drawing.Point(649, 22);
            this.listTheme.Name = "listTheme";
            this.listTheme.Size = new System.Drawing.Size(121, 24);
            this.listTheme.TabIndex = 4;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(567, 25);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(56, 17);
            this.label4.TabIndex = 3;
            this.label4.Text = "Theme:";
            // 
            // editHistorySize
            // 
            this.editHistorySize.Location = new System.Drawing.Point(119, 25);
            this.editHistorySize.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.editHistorySize.Name = "editHistorySize";
            this.editHistorySize.Size = new System.Drawing.Size(100, 22);
            this.editHistorySize.TabIndex = 2;
            this.editHistorySize.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
            this.editHistorySize.ValueChanged += new System.EventHandler(this.EditHistorySize_ValueChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 25);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(87, 17);
            this.label1.TabIndex = 0;
            this.label1.Text = "History Size:";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.label2);
            this.panel2.Controls.Add(this.listCounters);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Left;
            this.panel2.Location = new System.Drawing.Point(0, 71);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(239, 379);
            this.panel2.TabIndex = 1;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 23);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(69, 17);
            this.label2.TabIndex = 1;
            this.label2.Text = "Counters:";
            // 
            // listCounters
            // 
            this.listCounters.FormattingEnabled = true;
            this.listCounters.ItemHeight = 16;
            this.listCounters.Location = new System.Drawing.Point(12, 46);
            this.listCounters.Name = "listCounters";
            this.listCounters.Size = new System.Drawing.Size(207, 292);
            this.listCounters.TabIndex = 0;
            this.listCounters.SelectedIndexChanged += new System.EventHandler(this.ListCounters_SelectedIndexChanged);
            // 
            // panel3
            // 
            this.panel3.Controls.Add(this.listGraphType);
            this.panel3.Controls.Add(this.label6);
            this.panel3.Controls.Add(this.listShowCurrentValue);
            this.panel3.Controls.Add(this.label5);
            this.panel3.Controls.Add(this.listShowTitle);
            this.panel3.Controls.Add(this.label3);
            this.panel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel3.Location = new System.Drawing.Point(239, 71);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(561, 379);
            this.panel3.TabIndex = 2;
            // 
            // listShowTitle
            // 
            this.listShowTitle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listShowTitle.FormattingEnabled = true;
            this.listShowTitle.Location = new System.Drawing.Point(159, 95);
            this.listShowTitle.Name = "listShowTitle";
            this.listShowTitle.Size = new System.Drawing.Size(121, 24);
            this.listShowTitle.TabIndex = 1;
            this.listShowTitle.SelectedIndexChanged += new System.EventHandler(this.ListShowTitle_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(25, 98);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(72, 17);
            this.label3.TabIndex = 0;
            this.label3.Text = "Show title:";
            // 
            // listShowCurrentValue
            // 
            this.listShowCurrentValue.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listShowCurrentValue.FormattingEnabled = true;
            this.listShowCurrentValue.Location = new System.Drawing.Point(159, 137);
            this.listShowCurrentValue.Name = "listShowCurrentValue";
            this.listShowCurrentValue.Size = new System.Drawing.Size(121, 24);
            this.listShowCurrentValue.TabIndex = 3;
            this.listShowCurrentValue.SelectedIndexChanged += new System.EventHandler(this.listShowCurrentValue_SelectedIndexChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(25, 140);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(133, 17);
            this.label5.TabIndex = 2;
            this.label5.Text = "Show current value:";
            // 
            // listGraphType
            // 
            this.listGraphType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.listGraphType.FormattingEnabled = true;
            this.listGraphType.Location = new System.Drawing.Point(159, 43);
            this.listGraphType.Name = "listGraphType";
            this.listGraphType.Size = new System.Drawing.Size(121, 24);
            this.listGraphType.TabIndex = 5;
            this.listGraphType.SelectedIndexChanged += new System.EventHandler(this.listGraphType_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(25, 46);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(83, 17);
            this.label6.TabIndex = 4;
            this.label6.Text = "Graph type:";
            // 
            // OptionForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "OptionForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "takbar-monitor: settings";
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.editHistorySize)).EndInit();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.panel3.ResumeLayout(false);
            this.panel3.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.NumericUpDown editHistorySize;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ListBox listCounters;
        private System.Windows.Forms.ComboBox listShowTitle;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox listTheme;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ComboBox listShowCurrentValue;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox listGraphType;
        private System.Windows.Forms.Label label6;
    }
}