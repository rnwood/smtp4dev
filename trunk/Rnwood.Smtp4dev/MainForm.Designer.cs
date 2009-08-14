namespace Rnwood.Smtp4dev
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
            this.subjectDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.ToAddressesNice = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.messageBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.deleteAllButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel1 = new System.Windows.Forms.FlowLayoutPanel();
            this.viewButton = new System.Windows.Forms.Button();
            this.inspectMessageButton = new System.Windows.Forms.Button();
            this.saveButton = new System.Windows.Forms.Button();
            this.deleteButton = new System.Windows.Forms.Button();
            this.optionsButton = new System.Windows.Forms.Button();
            this.flowLayoutPanel2 = new System.Windows.Forms.FlowLayoutPanel();
            this.pictureBox2 = new System.Windows.Forms.PictureBox();
            this.pictureBox3 = new System.Windows.Forms.PictureBox();
            this.statusLabel = new System.Windows.Forms.Label();
            this.stopListeningButton = new System.Windows.Forms.Button();
            this.startListeningButton = new System.Windows.Forms.Button();
            this.saveMessageFileDialog = new System.Windows.Forms.SaveFileDialog();
            this.panel1 = new System.Windows.Forms.Panel();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.sessionsGrid = new System.Windows.Forms.DataGridView();
            this.StartDate = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.clientDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.NumberOfMessages = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.sessionBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.flowLayoutPanel3 = new System.Windows.Forms.FlowLayoutPanel();
            this.viewSessionButton = new System.Windows.Forms.Button();
            this.trayIconContextMenuStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.messageGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.messageBindingSource)).BeginInit();
            this.flowLayoutPanel1.SuspendLayout();
            this.flowLayoutPanel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).BeginInit();
            this.panel1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            this.tabPage2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sessionsGrid)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.sessionBindingSource)).BeginInit();
            this.flowLayoutPanel3.SuspendLayout();
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
            this.trayIconContextMenuStrip.Size = new System.Drawing.Size(189, 136);
            // 
            // showMenuItem
            // 
            this.showMenuItem.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.showMenuItem.Name = "showMenuItem";
            this.showMenuItem.Size = new System.Drawing.Size(188, 22);
            this.showMenuItem.Text = "View Messages";
            this.showMenuItem.Click += new System.EventHandler(this.trayIcon_DoubleClick);
            // 
            // viewLastMessageMenuItem
            // 
            this.viewLastMessageMenuItem.Enabled = false;
            this.viewLastMessageMenuItem.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.viewLastMessageMenuItem.Name = "viewLastMessageMenuItem";
            this.viewLastMessageMenuItem.Size = new System.Drawing.Size(188, 22);
            this.viewLastMessageMenuItem.Text = "View Last Message";
            this.viewLastMessageMenuItem.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // deleteAllMenuItem
            // 
            this.deleteAllMenuItem.Enabled = false;
            this.deleteAllMenuItem.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.deleteAllMenuItem.Name = "deleteAllMenuItem";
            this.deleteAllMenuItem.Size = new System.Drawing.Size(188, 22);
            this.deleteAllMenuItem.Text = "Delete All Messages";
            this.deleteAllMenuItem.Click += new System.EventHandler(this.clearAllEmailsToolStripMenuItem_Click);
            // 
            // listenForConnectionsToolStripMenuItem
            // 
            this.listenForConnectionsToolStripMenuItem.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listenForConnectionsToolStripMenuItem.Name = "listenForConnectionsToolStripMenuItem";
            this.listenForConnectionsToolStripMenuItem.Size = new System.Drawing.Size(188, 22);
            this.listenForConnectionsToolStripMenuItem.Text = "Listen for connections";
            this.listenForConnectionsToolStripMenuItem.Click += new System.EventHandler(this.listenForConnectionsToolStripMenuItem_Click);
            // 
            // optionsMenuItem
            // 
            this.optionsMenuItem.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.optionsMenuItem.Name = "optionsMenuItem";
            this.optionsMenuItem.Size = new System.Drawing.Size(188, 22);
            this.optionsMenuItem.Text = "Options";
            this.optionsMenuItem.Click += new System.EventHandler(this.optionsToolStripMenuItem_Click);
            // 
            // exitMenuItem
            // 
            this.exitMenuItem.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.exitMenuItem.Name = "exitMenuItem";
            this.exitMenuItem.Size = new System.Drawing.Size(188, 22);
            this.exitMenuItem.Text = "Exit";
            this.exitMenuItem.Click += new System.EventHandler(this.exitToolStripMenuItem_Click);
            // 
            // messageGrid
            // 
            this.messageGrid.AllowUserToDeleteRows = false;
            this.messageGrid.AllowUserToResizeRows = false;
            this.messageGrid.AutoGenerateColumns = false;
            this.messageGrid.BackgroundColor = System.Drawing.SystemColors.Window;
            this.messageGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.messageGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.Recieved,
            this.subjectDataGridViewTextBoxColumn,
            this.ToAddressesNice});
            this.messageGrid.DataSource = this.messageBindingSource;
            this.messageGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.messageGrid.GridColor = System.Drawing.SystemColors.ControlLight;
            this.messageGrid.Location = new System.Drawing.Point(3, 3);
            this.messageGrid.Name = "messageGrid";
            this.messageGrid.ReadOnly = true;
            this.messageGrid.RowHeadersVisible = false;
            this.messageGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.messageGrid.Size = new System.Drawing.Size(500, 203);
            this.messageGrid.TabIndex = 1;
            this.messageGrid.VirtualMode = true;
            this.messageGrid.DoubleClick += new System.EventHandler(this.messageGrid_DoubleClick);
            this.messageGrid.CellFormatting += new System.Windows.Forms.DataGridViewCellFormattingEventHandler(this.messageGrid_CellFormatting);
            this.messageGrid.SelectionChanged += new System.EventHandler(this.messageGrid_SelectionChanged);
            // 
            // Recieved
            // 
            this.Recieved.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.Recieved.DataPropertyName = "ReceivedDate";
            this.Recieved.HeaderText = "Recieved";
            this.Recieved.Name = "Recieved";
            this.Recieved.ReadOnly = true;
            this.Recieved.Width = 77;
            // 
            // subjectDataGridViewTextBoxColumn
            // 
            this.subjectDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.subjectDataGridViewTextBoxColumn.DataPropertyName = "Subject";
            this.subjectDataGridViewTextBoxColumn.HeaderText = "Subject";
            this.subjectDataGridViewTextBoxColumn.Name = "subjectDataGridViewTextBoxColumn";
            this.subjectDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // ToAddressesNice
            // 
            this.ToAddressesNice.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.ToAddressesNice.DataPropertyName = "To";
            this.ToAddressesNice.HeaderText = "To";
            this.ToAddressesNice.Name = "ToAddressesNice";
            this.ToAddressesNice.ReadOnly = true;
            // 
            // messageBindingSource
            // 
            this.messageBindingSource.DataSource = typeof(Rnwood.Smtp4dev.MessageViewModel);
            // 
            // deleteAllButton
            // 
            this.deleteAllButton.AutoSize = true;
            this.deleteAllButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.deleteAllButton.Enabled = false;
            this.deleteAllButton.Location = new System.Drawing.Point(212, 3);
            this.deleteAllButton.Name = "deleteAllButton";
            this.deleteAllButton.Size = new System.Drawing.Size(66, 23);
            this.deleteAllButton.TabIndex = 2;
            this.deleteAllButton.Text = "Delete All";
            this.deleteAllButton.UseVisualStyleBackColor = true;
            this.deleteAllButton.Click += new System.EventHandler(this.deleteAllButton_Click);
            // 
            // flowLayoutPanel1
            // 
            this.flowLayoutPanel1.AutoSize = true;
            this.flowLayoutPanel1.Controls.Add(this.viewButton);
            this.flowLayoutPanel1.Controls.Add(this.inspectMessageButton);
            this.flowLayoutPanel1.Controls.Add(this.saveButton);
            this.flowLayoutPanel1.Controls.Add(this.deleteButton);
            this.flowLayoutPanel1.Controls.Add(this.deleteAllButton);
            this.flowLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel1.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel1.Location = new System.Drawing.Point(3, 206);
            this.flowLayoutPanel1.Name = "flowLayoutPanel1";
            this.flowLayoutPanel1.Size = new System.Drawing.Size(500, 29);
            this.flowLayoutPanel1.TabIndex = 3;
            // 
            // viewButton
            // 
            this.viewButton.AutoSize = true;
            this.viewButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.viewButton.Enabled = false;
            this.viewButton.Location = new System.Drawing.Point(455, 3);
            this.viewButton.Name = "viewButton";
            this.viewButton.Size = new System.Drawing.Size(42, 23);
            this.viewButton.TabIndex = 3;
            this.viewButton.Text = "View";
            this.viewButton.UseVisualStyleBackColor = true;
            this.viewButton.Click += new System.EventHandler(this.viewButton_Click);
            // 
            // inspectMessageButton
            // 
            this.inspectMessageButton.AutoSize = true;
            this.inspectMessageButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.inspectMessageButton.Enabled = false;
            this.inspectMessageButton.Location = new System.Drawing.Point(395, 3);
            this.inspectMessageButton.Name = "inspectMessageButton";
            this.inspectMessageButton.Size = new System.Drawing.Size(54, 23);
            this.inspectMessageButton.TabIndex = 7;
            this.inspectMessageButton.Text = "Inspect";
            this.inspectMessageButton.UseVisualStyleBackColor = true;
            this.inspectMessageButton.Click += new System.EventHandler(this.inspectButton_Click);
            // 
            // saveButton
            // 
            this.saveButton.AutoSize = true;
            this.saveButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.saveButton.Enabled = false;
            this.saveButton.Location = new System.Drawing.Point(340, 3);
            this.saveButton.Name = "saveButton";
            this.saveButton.Size = new System.Drawing.Size(49, 23);
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
            this.deleteButton.Location = new System.Drawing.Point(284, 3);
            this.deleteButton.Name = "deleteButton";
            this.deleteButton.Size = new System.Drawing.Size(50, 23);
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
            this.optionsButton.Location = new System.Drawing.Point(312, 3);
            this.optionsButton.Name = "optionsButton";
            this.optionsButton.Size = new System.Drawing.Size(59, 23);
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
            this.statusLabel.Size = new System.Drawing.Size(74, 13);
            this.statusLabel.TabIndex = 5;
            this.statusLabel.Text = "Not listening";
            this.statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // stopListeningButton
            // 
            this.stopListeningButton.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.stopListeningButton.AutoSize = true;
            this.stopListeningButton.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.stopListeningButton.Location = new System.Drawing.Point(118, 3);
            this.stopListeningButton.Name = "stopListeningButton";
            this.stopListeningButton.Size = new System.Drawing.Size(91, 23);
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
            this.startListeningButton.Location = new System.Drawing.Point(215, 3);
            this.startListeningButton.Name = "startListeningButton";
            this.startListeningButton.Size = new System.Drawing.Size(91, 23);
            this.startListeningButton.TabIndex = 7;
            this.startListeningButton.Text = "Start Listening";
            this.startListeningButton.UseVisualStyleBackColor = true;
            this.startListeningButton.Click += new System.EventHandler(this.startListeningButton_Click);
            // 
            // saveMessageFileDialog
            // 
            this.saveMessageFileDialog.Filter = "Email message|*.eml";
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
            this.label2.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.ForeColor = System.Drawing.Color.Black;
            this.label2.Location = new System.Drawing.Point(43, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(108, 13);
            this.label2.TabIndex = 1;
            this.label2.Text = "rob@rnwood.co.uk";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Segoe UI", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.Black;
            this.label1.Location = new System.Drawing.Point(43, 8);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(79, 20);
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
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 50);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(514, 264);
            this.tabControl1.TabIndex = 5;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.messageGrid);
            this.tabPage1.Controls.Add(this.flowLayoutPanel1);
            this.tabPage1.Location = new System.Drawing.Point(4, 22);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(506, 238);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Messages";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.sessionsGrid);
            this.tabPage2.Controls.Add(this.flowLayoutPanel3);
            this.tabPage2.Location = new System.Drawing.Point(4, 22);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(506, 238);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Sessions";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // sessionsGrid
            // 
            this.sessionsGrid.AllowUserToAddRows = false;
            this.sessionsGrid.AllowUserToDeleteRows = false;
            this.sessionsGrid.AutoGenerateColumns = false;
            this.sessionsGrid.BackgroundColor = System.Drawing.SystemColors.Window;
            this.sessionsGrid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.sessionsGrid.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.StartDate,
            this.clientDataGridViewTextBoxColumn,
            this.NumberOfMessages});
            this.sessionsGrid.DataSource = this.sessionBindingSource;
            this.sessionsGrid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sessionsGrid.GridColor = System.Drawing.SystemColors.ControlLight;
            this.sessionsGrid.Location = new System.Drawing.Point(3, 3);
            this.sessionsGrid.MultiSelect = false;
            this.sessionsGrid.Name = "sessionsGrid";
            this.sessionsGrid.ReadOnly = true;
            this.sessionsGrid.RowHeadersVisible = false;
            this.sessionsGrid.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.sessionsGrid.Size = new System.Drawing.Size(500, 203);
            this.sessionsGrid.TabIndex = 0;
            this.sessionsGrid.SelectionChanged += new System.EventHandler(this.sessionsGrid_SelectionChanged);
            // 
            // StartDate
            // 
            this.StartDate.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.StartDate.DataPropertyName = "StartDate";
            this.StartDate.HeaderText = "Date/Time";
            this.StartDate.Name = "StartDate";
            this.StartDate.ReadOnly = true;
            this.StartDate.Width = 83;
            // 
            // clientDataGridViewTextBoxColumn
            // 
            this.clientDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.clientDataGridViewTextBoxColumn.DataPropertyName = "Client";
            this.clientDataGridViewTextBoxColumn.HeaderText = "Client";
            this.clientDataGridViewTextBoxColumn.Name = "clientDataGridViewTextBoxColumn";
            this.clientDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // NumberOfMessages
            // 
            this.NumberOfMessages.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.NumberOfMessages.DataPropertyName = "NumberOfMessages";
            this.NumberOfMessages.HeaderText = "No of Msgs";
            this.NumberOfMessages.Name = "NumberOfMessages";
            this.NumberOfMessages.ReadOnly = true;
            this.NumberOfMessages.Width = 91;
            // 
            // sessionBindingSource
            // 
            this.sessionBindingSource.DataSource = typeof(Rnwood.Smtp4dev.SessionViewModel);
            // 
            // flowLayoutPanel3
            // 
            this.flowLayoutPanel3.AutoSize = true;
            this.flowLayoutPanel3.Controls.Add(this.viewSessionButton);
            this.flowLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.flowLayoutPanel3.FlowDirection = System.Windows.Forms.FlowDirection.RightToLeft;
            this.flowLayoutPanel3.Location = new System.Drawing.Point(3, 206);
            this.flowLayoutPanel3.Name = "flowLayoutPanel3";
            this.flowLayoutPanel3.Size = new System.Drawing.Size(500, 29);
            this.flowLayoutPanel3.TabIndex = 1;
            // 
            // viewSessionButton
            // 
            this.viewSessionButton.Enabled = false;
            this.viewSessionButton.Location = new System.Drawing.Point(422, 3);
            this.viewSessionButton.Name = "viewSessionButton";
            this.viewSessionButton.Size = new System.Drawing.Size(75, 23);
            this.viewSessionButton.TabIndex = 0;
            this.viewSessionButton.Text = "View Log";
            this.viewSessionButton.UseVisualStyleBackColor = true;
            this.viewSessionButton.Click += new System.EventHandler(this.button1_Click_1);
            // 
            // MainForm
            // 
            this.AcceptButton = this.viewButton;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(514, 347);
            this.Controls.Add(this.tabControl1);
            this.Controls.Add(this.flowLayoutPanel2);
            this.Controls.Add(this.panel1);
            this.Font = new System.Drawing.Font("Segoe UI", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "smtp4dev";
            this.SizeChanged += new System.EventHandler(this.MainForm_SizeChanged);
            this.VisibleChanged += new System.EventHandler(this.MainForm_VisibleChanged);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.trayIconContextMenuStrip.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.messageGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.messageBindingSource)).EndInit();
            this.flowLayoutPanel1.ResumeLayout(false);
            this.flowLayoutPanel1.PerformLayout();
            this.flowLayoutPanel2.ResumeLayout(false);
            this.flowLayoutPanel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox2)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox3)).EndInit();
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            this.tabPage2.ResumeLayout(false);
            this.tabPage2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.sessionsGrid)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.sessionBindingSource)).EndInit();
            this.flowLayoutPanel3.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.NotifyIcon trayIcon;
        private System.Windows.Forms.ContextMenuStrip trayIconContextMenuStrip;
        private System.Windows.Forms.ToolStripMenuItem exitMenuItem;
        private System.Windows.Forms.DataGridView messageGrid;
        private System.Windows.Forms.BindingSource messageBindingSource;
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
        private System.Windows.Forms.SaveFileDialog saveMessageFileDialog;
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
        private System.Windows.Forms.DataGridViewTextBoxColumn fromAddressDataGridViewTextBoxColumn;
        private System.Windows.Forms.Button inspectMessageButton;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.DataGridView sessionsGrid;
        private System.Windows.Forms.BindingSource sessionBindingSource;
        private System.Windows.Forms.DataGridViewTextBoxColumn StartDate;
        private System.Windows.Forms.DataGridViewTextBoxColumn clientDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn NumberOfMessages;
        private System.Windows.Forms.FlowLayoutPanel flowLayoutPanel3;
        private System.Windows.Forms.Button viewSessionButton;
        private System.Windows.Forms.DataGridViewTextBoxColumn Recieved;
        private System.Windows.Forms.DataGridViewTextBoxColumn subjectDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn ToAddressesNice;
    }
}

