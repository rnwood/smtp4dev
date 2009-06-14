namespace smtp4dev
{
    partial class MainForm
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.trayIcon = new System.Windows.Forms.NotifyIcon(this.components);
            this.trayIconContextMenuStrip = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.showMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.viewLastMessageMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.deleteAllMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.listenForConnectionsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.optionsMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.exitMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.messageGrid = new System.Windows.Forms.DataGridView();
            this.Recieved = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ToAddressesNice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.deleteAllButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.viewButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.optionsButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.stopListeningButton = new System.Windows.Forms.Button();
            this.startListeningButton = new System.Windows.Forms.Button();
            this.saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.fromAddressDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.subjectDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.bindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.button1 = new System.Windows.Forms.Button();
            this.trayIconContextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.messageGrid)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).BeginInit();
            this.SuspendLayout();
            // 
            // trayIcon
            // 
            this.trayIcon.ContextMenuStrip = this.trayIconContextMenuStrip;
            this.trayIcon.Text = "smtp4dev";
            this.trayIcon.Visible = true;
            this.trayIcon.BalloonTipClicked += new System.EventHandler(this.trayIcon_BalloonTipClicked);
            this.trayIcon.DoubleClick += new System.EventHandler(this.trayIcon_DoubleClick);
            // 
            // trayIconContextMenuStrip
            // 
            this.trayIconContextMenuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.showMenuItem,
            this.viewLastMessageMenuItem,
            this.deleteAllMenuItem,
            this.listenForConnectionsToolStripMenuItem,
            this.optionsMenuItem,
            this.exitMenuItem});
            this.trayIconContextMenuStrip.Name = "contextMenuStrip";
            this.trayIconContextMenuStrip.Size = new System.Drawing.Size(191, 136);
            // 
            // showMenuItem
            // 
            this.showMenuItem.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.showMenuItem.Name = "showMenuItem";
            this.showMenuItem.Size = new System.Drawing.Size(190, 22);
            this.showMenuItem.Text = "View Messages";
            this.showMenuItem.Click += new System.EventHandler(this.trayIcon_DoubleClick);
            // 
            // viewLastMessageMenuItem
            // 
            this.viewLastMessageMenuItem.Enabled = false;
            this.viewLastMessageMenuItem.Name = "viewLastMessageMenuItem";
            this.viewLastMessageMenuItem.Size = new System.Drawing.Size(190, 22);
            this.viewLastMessageMenuItem.Text = "View Last Message";
            this.viewLastMessageMenuItem.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // deleteAllMenuItem
            // 
            this.deleteAllMenuItem.Enabled = false;
            this.deleteAllMenuItem.Name = "deleteAllMenuItem";
            this.deleteAllMenuItem.Size = new System.Drawing.Size(190, 22);
            this.deleteAllMenuItem.Text = "Delete All Messages";
            this.deleteAllMenuItem.Click += new System.EventHandler(this.clearAllEmailsToolStripMenuItem_Click);
            // 
            // listenForConnectionsToolStripMenuItem
            // 
            this.listenForConnectionsToolStripMenuItem.Name = "listenForConnectionsToolStripMenuItem";
            this.listenForConnectionsToolStripMenuItem.Size = new System.Drawing.Size(190, 22);
            this.listenForConnectionsToolStripMenuItem.Text = "Listen for connections";
            this.listenForConnectionsToolStripMenuItem.Click += new System.EventHandler(this.listenForConnectionsToolStripMenuItem_Click);
            // 
            // optionsMenuItem
            // 
            this.optionsMenuItem.Name = "optionsMenuItem";
            this.optionsMenuItem.Size = new System.Drawing.Size(190, 22);
            this.optionsMenuItem.Text = "Options";
            this.optionsMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(190, 22);
            this.exitMenuItem.Text = "Exit";
            this.exitMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // messageGrid
            // 
            this.messageGrid.AllowUserToDeleteRows = false;
            this.messageGrid.AllowUserToResizeRows = false;
            this.messageGrid.AutoGenerateColumns = false;
            this.messageGrid.BackgroundColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle1.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle1.BackColor = System.Drawing.SystemColors.Control;
            dataGridViewCellStyle1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle1.ForeColor = System.Drawing.SystemColors.WindowText;
            dataGridViewCellStyle1.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle1.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle1.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.messageGrid.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle1;
            this.messageGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.messageGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Recieved,
            this.fromAddressDataGridViewTextBoxColumn,
            this.ToAddressesNice,
            this.subjectDataGridViewTextBoxColumn});
            this.messageGrid.DataSource = this.bindingSource;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle2.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.SystemColors.ControlText;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.messageGrid.DefaultCellStyle = dataGridViewCellStyle2;
            this.messageGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.messageGrid.GridColor = System.Drawing.SystemColors.ControlLight;
            this.messageGrid.Location = new System.Drawing.Point(0, 50);
            this.messageGrid.MultiSelect = false;
            this.messageGrid.Name = "messageGrid";
            this.messageGrid.ReadOnly = true;
            this.messageGrid.RowHeadersVisible = false;
            this.messageGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.messageGrid.Size = new System.Drawing.Size(514, 235);
            this.messageGrid.TabIndex = 1;
            this.messageGrid.VirtualMode = true;
            this.messageGrid.DoubleClick += new System.EventHandler(this.messageGrid_DoubleClick);
            this.messageGrid.CellValueNeeded += new System.Windows.Forms.DataGridViewCellValueEventHandler(this.messageGrid_CellValueNeeded);
            this.messageGrid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.messageGrid_CellFormatting);
            this.messageGrid.SelectionChanged += new System.EventHandler(this.messageGrid_SelectionChanged);
            // 
            // Recieved
            // 
            this.Recieved.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Recieved.DataPropertyName = "Recieved";
            this.Recieved.HeaderText = "Recieved";
            this.Recieved.Name = "Recieved";
            this.Recieved.ReadOnly = true;
            this.Recieved.Width = 78;
            // 
            // ToAddressesNice
            // 
            this.ToAddressesNice.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ToAddressesNice.HeaderText = "To";
            this.ToAddressesNice.Name = "ToAddressesNice";
            this.ToAddressesNice.ReadOnly = true;
            // 
            // deleteAllButton
            // 
            this.deleteAllButton.AutoSize = true;
            this.deleteAllButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.deleteAllButton.Enabled = false;
            this.deleteAllButton.Location = new System.Drawing.Point(209, 3);
            this.deleteAllButton.Name = "deleteAllButton";
            this.deleteAllButton.Size = new System.Drawing.Size(62, 23);
            this.deleteAllButton.TabIndex = 2;
            this.deleteAllButton.Text = "Delete All";
            this.deleteAllButton.UseVisualStyleBackColor = true;
            this.deleteAllButton.Click += new System.EventHandler(this.deleteAllButton_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.button1);
            this.flowLayoutPanel1.Controls.Add(this.viewButton);
            this.flowLayoutPanel1.Controls.Add(this.saveButton);
            this.flowLayoutPanel1.Controls.Add(this.deleteButton);
            this.flowLayoutPanel1.Controls.Add(this.deleteAllButton);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(0, 285);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(514, 29);
            this.flowLayoutPanel1.TabIndex = 3;
            // 
            // viewButton
            // 
            this.viewButton.AutoSize = true;
            this.viewButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.viewButton.Enabled = false;
            this.viewButton.Location = new System.Drawing.Point(388, 3);
            this.viewButton.Name = "viewButton";
            this.viewButton.Size = new System.Drawing.Size(40, 23);
            this.viewButton.TabIndex = 3;
            this.viewButton.Text = "View";
            this.viewButton.UseVisualStyleBackColor = true;
            this.viewButton.Click += new System.EventHandler(this.viewButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.AutoSize = true;
            this.saveButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.saveButton.Enabled = false;
            this.saveButton.Location = new System.Drawing.Point(331, 3);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(51, 23);
            this.saveButton.TabIndex = 4;
            this.saveButton.Text = "Save...";
            this.saveButton.UseVisualStyleBackColor = true;
            this.saveButton.Click += new System.EventHandler(this.saveButton_Click);
            // 
            // deleteButton
            // 
            this.deleteButton.AutoSize = true;
            this.deleteButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.deleteButton.Enabled = false;
            this.deleteButton.Location = new System.Drawing.Point(277, 3);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(48, 23);
            this.deleteButton.TabIndex = 5;
            this.deleteButton.Text = "Delete";
            this.deleteButton.UseVisualStyleBackColor = true;
            this.deleteButton.Click += new System.EventHandler(this.deleteButton_Click);
            // 
            // optionsButton
            // 
            this.optionsButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.optionsButton.AutoSize = true;
            this.optionsButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.optionsButton.Location = new System.Drawing.Point(289, 3);
            this.optionsButton.Name = "optionsButton";
            this.optionsButton.Size = new System.Drawing.Size(53, 23);
            this.optionsButton.TabIndex = 4;
            this.optionsButton.Text = "Options";
            this.optionsButton.UseVisualStyleBackColor = true;
            this.optionsButton.Click += new System.EventHandler(this.optionsButton_Click);
            // 
            // flowLayoutPanel2
            // 
            this.flowLayoutPanel2.AutoSize = true;
            this.flowLayoutPanel2.BackColor = System.Drawing.SystemColors.Control;
            this.flowLayoutPanel2.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.flowLayoutPanel2.Controls.Add(this.pictureBox2);
            this.flowLayoutPanel2.Controls.Add(this.pictureBox3);
            this.flowLayoutPanel2.Controls.Add(this.statusLabel);
            this.flowLayoutPanel2.Controls.Add(this.stopListeningButton);
            this.flowLayoutPanel2.Controls.Add(this.startListeningButton);
            this.flowLayoutPanel2.Controls.Add(this.optionsButton);
            this.flowLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel2.Location = new System.Drawing.Point(0, 314);
            this.flowLayoutPanel2.Name = "flowLayoutPanel2";
            this.flowLayoutPanel2.Size = new System.Drawing.Size(514, 33);
            this.flowLayoutPanel2.TabIndex = 4;
            // 
            // pictureBox2
            // 
            this.pictureBox2.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pictureBox2.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox2.Image")));
            this.pictureBox2.Location = new System.Drawing.Point(3, 6);
            this.pictureBox2.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.pictureBox2.Name = "pictureBox2";
            this.pictureBox2.Size = new System.Drawing.Size(16, 16);
            this.pictureBox2.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox2.TabIndex = 8;
            this.pictureBox2.TabStop = false;
            // 
            // pictureBox3
            // 
            this.pictureBox3.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.pictureBox3.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox3.Image")));
            this.pictureBox3.Location = new System.Drawing.Point(22, 6);
            this.pictureBox3.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.pictureBox3.Name = "pictureBox3";
            this.pictureBox3.Size = new System.Drawing.Size(16, 16);
            this.pictureBox3.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox3.TabIndex = 9;
            this.pictureBox3.TabStop = false;
            // 
            // statusLabel
            // 
            this.statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.statusLabel.AutoEllipsis = true;
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(38, 8);
            this.statusLabel.Margin = new System.Windows.Forms.Padding(0, 0, 3, 0);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(65, 13);
            this.statusLabel.TabIndex = 5;
            this.statusLabel.Text = "Not listening";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // stopListeningButton
            // 
            this.stopListeningButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.stopListeningButton.AutoSize = true;
            this.stopListeningButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.stopListeningButton.Location = new System.Drawing.Point(109, 3);
            this.stopListeningButton.Name = "stopListeningButton";
            this.stopListeningButton.Size = new System.Drawing.Size(84, 23);
            this.stopListeningButton.TabIndex = 6;
            this.stopListeningButton.Text = "Stop Listening";
            this.stopListeningButton.UseVisualStyleBackColor = true;
            this.stopListeningButton.Visible = false;
            this.stopListeningButton.Click += new System.EventHandler(this.stopListeningButton_Click);
            // 
            // startListeningButton
            // 
            this.startListeningButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.startListeningButton.AutoSize = true;
            this.startListeningButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.startListeningButton.Location = new System.Drawing.Point(199, 3);
            this.startListeningButton.Name = "startListeningButton";
            this.startListeningButton.Size = new System.Drawing.Size(84, 23);
            this.startListeningButton.TabIndex = 7;
            this.startListeningButton.Text = "Start Listening";
            this.startListeningButton.UseVisualStyleBackColor = true;
            this.startListeningButton.Click += new System.EventHandler(this.startListeningButton_Click);
            // 
            // saveFileDialog
            // 
            this.saveFileDialog.Filter = "Email message|*.eml";
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.White;
            this.panel1.Controls.Add(this.label2);
            this.panel1.Controls.Add(this.label1);
            this.panel1.Controls.Add(this.pictureBox1);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Font = new System.Drawing.Font("Arial", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(514, 50);
            this.panel1.TabIndex = 4;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.BackColor = System.Drawing.Color.Transparent;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(43, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(98, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "rob@rnwood.co.uk";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(43, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 18);
            this.label1.TabIndex = 0;
            this.label1.Text = "smtp4dev";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(1, 4);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(45, 47);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 2;
            this.pictureBox1.TabStop = false;
            // 
            // fromAddressDataGridViewTextBoxColumn
            // 
            this.fromAddressDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.fromAddressDataGridViewTextBoxColumn.DataPropertyName = "FromAddress";
            this.fromAddressDataGridViewTextBoxColumn.HeaderText = "From";
            this.fromAddressDataGridViewTextBoxColumn.Name = "fromAddressDataGridViewTextBoxColumn";
            this.fromAddressDataGridViewTextBoxColumn.ReadOnly = true;
            this.fromAddressDataGridViewTextBoxColumn.Width = 55;
            // 
            // subjectDataGridViewTextBoxColumn
            // 
            this.subjectDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.subjectDataGridViewTextBoxColumn.DataPropertyName = "Subject";
            this.subjectDataGridViewTextBoxColumn.HeaderText = "Subject";
            this.subjectDataGridViewTextBoxColumn.Name = "subjectDataGridViewTextBoxColumn";
            this.subjectDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // bindingSource
            // 
            this.bindingSource.DataSource = typeof(smtp4dev.Email);
            // 
            // button1
            // 
            this.button1.AutoSize = true;
            this.button1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.button1.Enabled = false;
            this.button1.Location = new System.Drawing.Point(434, 3);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(77, 23);
            this.button1.TabIndex = 6;
            this.button1.Text = "View Source";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // MainForm
            // 
            this.AcceptButton = this.viewButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(514, 347);
            this.Controls.Add(this.messageGrid);
            this.Controls.Add(this.flowLayoutPanel1);
            this.Controls.Add(this.flowLayoutPanel2);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "smtp4dev";
            this.SizeChanged += new System.EventHandler(this.MainForm_SizeChanged);
            this.VisibleChanged += new System.EventHandler(this.MainForm_VisibleChanged);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.trayIconContextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.messageGrid)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.bindingSource)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayIconContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.DataGridView messageGrid;
        private System.Windows.Forms.BindingSource bindingSource;
        private System.Windows.Forms.Button deleteAllButton;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel1;
        private System.Windows.Forms.Button viewButton;
        private System.Windows.Forms.ToolStripMenuItem deleteAllMenuItem;
        private System.Windows.Forms.ToolStripMenuItem viewLastMessageMenuItem;
        private System.Windows.Forms.ToolStripMenuItem showMenuItem;
        private System.Windows.Forms.ToolStripMenuItem optionsMenuItem;
        private System.Windows.Forms.Button optionsButton;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel2;
        private System.Windows.Forms.Button saveButton;
        private System.Windows.Forms.SaveFileDialog saveFileDialog;
        private System.Windows.Forms.Button deleteButton;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.PictureBox pictureBox1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Button stopListeningButton;
        private System.Windows.Forms.Button startListeningButton;
        private System.Windows.Forms.PictureBox pictureBox2;
        private System.Windows.Forms.PictureBox pictureBox3;
        private System.Windows.Forms.ToolStripMenuItem listenForConnectionsToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn Recieved;
        private System.Windows.Forms.DataGridViewTextBoxColumn fromAddressDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ToAddressesNice;
        private System.Windows.Forms.DataGridViewTextBoxColumn subjectDataGridViewTextBoxColumn;
        private System.Windows.Forms.Button button1;
    }
}

