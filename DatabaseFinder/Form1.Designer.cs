namespace DatabaseFinder;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();

        this.dgvDatabases = new System.Windows.Forms.DataGridView();
        this.btnRefresh = new System.Windows.Forms.Button();
        this.lblStatus = new System.Windows.Forms.Label();
        this.btnCopy = new System.Windows.Forms.Button();
        this.lblTitle = new System.Windows.Forms.Label();
        this.btnSettings = new System.Windows.Forms.Button();
        this.btnProfiles = new System.Windows.Forms.Button();
        this.btnTest = new System.Windows.Forms.Button();
        this.btnRemote = new System.Windows.Forms.Button();
        this.btnCopyFiles = new System.Windows.Forms.Button();
        this.btnQuery = new System.Windows.Forms.Button();
        this.cmbScanMode = new System.Windows.Forms.ComboBox();
        this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
        this.trayMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
        this.miShow = new System.Windows.Forms.ToolStripMenuItem();
        this.miRefresh = new System.Windows.Forms.ToolStripMenuItem();
        this.miExit = new System.Windows.Forms.ToolStripMenuItem();

        ((System.ComponentModel.ISupportInitialize)(this.dgvDatabases)).BeginInit();
        this.trayMenu.SuspendLayout();
        this.SuspendLayout();

        // dgvDatabases
        this.dgvDatabases.AllowUserToAddRows = false;
        this.dgvDatabases.AllowUserToDeleteRows = false;
        this.dgvDatabases.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
        this.dgvDatabases.AutoGenerateColumns = false;
        this.dgvDatabases.BackgroundColor = System.Drawing.Color.White;
        this.dgvDatabases.Location = new System.Drawing.Point(12, 90);
        this.dgvDatabases.Name = "dgvDatabases";
        this.dgvDatabases.ReadOnly = true;
        this.dgvDatabases.RowHeadersVisible = false;
        this.dgvDatabases.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.dgvDatabases.Size = new System.Drawing.Size(770, 300);
        this.dgvDatabases.TabIndex = 0;
        this.dgvDatabases.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDatabases_CellDoubleClick);

        // Columns
        this.colCheck = new System.Windows.Forms.DataGridViewCheckBoxColumn();
        this.colCheck.HeaderText = "انتخاب";
        this.colCheck.Name = "colCheck";
        this.colCheck.DataPropertyName = "Selected";
        this.colCheck.Width = 45;

        this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colType.HeaderText = "نام دیتابیس";
        this.colType.Name = "colType";
        this.colType.DataPropertyName = "DisplayName";
        this.colType.ReadOnly = true;
        this.colType.Width = 100;

        this.colVersion = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colVersion.HeaderText = "نسخه";
        this.colVersion.Name = "colVersion";
        this.colVersion.DataPropertyName = "Version";
        this.colVersion.ReadOnly = true;
        this.colVersion.Width = 100;

        this.colPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colPort.HeaderText = "پورت";
        this.colPort.Name = "colPort";
        this.colPort.DataPropertyName = "Port";
        this.colPort.ReadOnly = true;
        this.colPort.Width = 60;

        this.colService = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colService.HeaderText = "سرویس";
        this.colService.Name = "colService";
        this.colService.DataPropertyName = "ServiceName";
        this.colService.ReadOnly = true;
        this.colService.Width = 150;

        this.colProcess = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colProcess.HeaderText = "پروسس / PID";
        this.colProcess.Name = "colProcess";
        this.colProcess.DataPropertyName = "ProcessDisplay";
        this.colProcess.ReadOnly = true;
        this.colProcess.Width = 130;

        this.colHow = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colHow.HeaderText = "تشخیص";
        this.colHow.Name = "colHow";
        this.colHow.DataPropertyName = "DetectionMethod";
        this.colHow.ReadOnly = true;
        this.colHow.Width = 80;

        this.colLocation = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colLocation.HeaderText = "مسیر / آدرس";
        this.colLocation.Name = "colLocation";
        this.colLocation.DataPropertyName = "Location";
        this.colLocation.ReadOnly = true;
        this.colLocation.Width = 220;

        this.colSizeInfo = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colSizeInfo.HeaderText = "اندازه / تاریخ";
        this.colSizeInfo.Name = "colSizeInfo";
        this.colSizeInfo.DataPropertyName = "SizeInfo";
        this.colSizeInfo.ReadOnly = true;
        this.colSizeInfo.Width = 130;

        this.dgvDatabases.Columns.Add(this.colCheck);
        this.dgvDatabases.Columns.Add(this.colType);
        this.dgvDatabases.Columns.Add(this.colVersion);
        this.dgvDatabases.Columns.Add(this.colPort);
        this.dgvDatabases.Columns.Add(this.colService);
        this.dgvDatabases.Columns.Add(this.colProcess);
        this.dgvDatabases.Columns.Add(this.colHow);
        this.dgvDatabases.Columns.Add(this.colLocation);
        this.dgvDatabases.Columns.Add(this.colSizeInfo);

        // cmbScanMode
        this.cmbScanMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbScanMode.Font = new System.Drawing.Font("Segoe UI", 9F);
        this.cmbScanMode.Location = new System.Drawing.Point(300, 12);
        this.cmbScanMode.Name = "cmbScanMode";
        this.cmbScanMode.Size = new System.Drawing.Size(260, 26);
        this.cmbScanMode.TabIndex = 2;
        this.cmbScanMode.Items.AddRange(new object[] {
            "فقط آنلاین (در حال اجرا)",
            "فقط آفلاین (اسکن هارد)",
            "آنلاین + آفلاین"});
        this.cmbScanMode.SelectedIndex = 0;

        // lblTitle
        this.lblTitle.AutoSize = true;
        this.lblTitle.BackColor = System.Drawing.Color.Transparent;
        this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 13F, System.Drawing.FontStyle.Bold);
        this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(33, 150, 243);
        this.lblTitle.Location = new System.Drawing.Point(10, 9);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Text = "Database Finder v1.4";

        this.Controls.Add(this.cmbScanMode);
        this.Controls.Add(this.lblTitle);

        // btnRefresh
        this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
        this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnRefresh.ForeColor = System.Drawing.Color.White;
        this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnRefresh.Location = new System.Drawing.Point(674, 12);
        this.btnRefresh.Name = "btnRefresh";
        this.btnRefresh.Size = new System.Drawing.Size(108, 28);
        this.btnRefresh.Text = "تشخیص مجدد";
        this.btnRefresh.UseVisualStyleBackColor = false;
        this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

        // btnCopy
        this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnCopy.BackColor = System.Drawing.Color.FromArgb(76, 175, 80);
        this.btnCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCopy.ForeColor = System.Drawing.Color.White;
        this.btnCopy.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnCopy.Location = new System.Drawing.Point(574, 12);
        this.btnCopy.Name = "btnCopy";
        this.btnCopy.Size = new System.Drawing.Size(94, 28);
        this.btnCopy.Text = "کپی لیست";
        this.btnCopy.UseVisualStyleBackColor = false;
        this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);

        // btnSettings
        this.btnSettings.BackColor = System.Drawing.Color.FromArgb(255, 152, 0);
        this.btnSettings.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnSettings.ForeColor = System.Drawing.Color.White;
        this.btnSettings.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnSettings.Location = new System.Drawing.Point(12, 45);
        this.btnSettings.Name = "btnSettings";
        this.btnSettings.Size = new System.Drawing.Size(110, 30);
        this.btnSettings.Text = "تنظیمات";
        this.btnSettings.UseVisualStyleBackColor = false;
        this.btnSettings.Click += new System.EventHandler(this.btnSettings_Click);

        // btnProfiles
        this.btnProfiles.BackColor = System.Drawing.Color.FromArgb(156, 39, 176);
        this.btnProfiles.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnProfiles.ForeColor = System.Drawing.Color.White;
        this.btnProfiles.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnProfiles.Location = new System.Drawing.Point(130, 45);
        this.btnProfiles.Name = "btnProfiles";
        this.btnProfiles.Size = new System.Drawing.Size(110, 30);
        this.btnProfiles.Text = "پروفایل‌ها";
        this.btnProfiles.UseVisualStyleBackColor = false;
        this.btnProfiles.Click += new System.EventHandler(this.btnProfiles_Click);

        // btnTest
        this.btnTest.BackColor = System.Drawing.Color.FromArgb(0, 188, 212);
        this.btnTest.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnTest.ForeColor = System.Drawing.Color.White;
        this.btnTest.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnTest.Location = new System.Drawing.Point(248, 45);
        this.btnTest.Name = "btnTest";
        this.btnTest.Size = new System.Drawing.Size(110, 30);
        this.btnTest.Text = "تست اتصال";
        this.btnTest.UseVisualStyleBackColor = false;
        this.btnTest.Click += new System.EventHandler(this.btnTest_Click);

        // btnQuery
        this.btnQuery.BackColor = System.Drawing.Color.FromArgb(0, 150, 136);
        this.btnQuery.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnQuery.ForeColor = System.Drawing.Color.White;
        this.btnQuery.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnQuery.Location = new System.Drawing.Point(366, 45);
        this.btnQuery.Name = "btnQuery";
        this.btnQuery.Size = new System.Drawing.Size(110, 30);
        this.btnQuery.Text = "اجرای کوئری";
        this.btnQuery.UseVisualStyleBackColor = false;
        this.btnQuery.Click += new System.EventHandler(this.btnQuery_Click);

        // btnRemote
        this.btnRemote.BackColor = System.Drawing.Color.FromArgb(63, 81, 181);
        this.btnRemote.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnRemote.ForeColor = System.Drawing.Color.White;
        this.btnRemote.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnRemote.Location = new System.Drawing.Point(484, 45);
        this.btnRemote.Name = "btnRemote";
        this.btnRemote.Size = new System.Drawing.Size(130, 30);
        this.btnRemote.Text = "اسکن راه دور";
        this.btnRemote.UseVisualStyleBackColor = false;
        this.btnRemote.Click += new System.EventHandler(this.btnRemote_Click);

        // btnCopyFiles
        this.btnCopyFiles.BackColor = System.Drawing.Color.FromArgb(139, 195, 74);
        this.btnCopyFiles.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCopyFiles.ForeColor = System.Drawing.Color.White;
        this.btnCopyFiles.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
        this.btnCopyFiles.Location = new System.Drawing.Point(622, 45);
        this.btnCopyFiles.Name = "btnCopyFiles";
        this.btnCopyFiles.Size = new System.Drawing.Size(160, 30);
        this.btnCopyFiles.Text = "کپی فایل‌های دیتابیس";
        this.btnCopyFiles.UseVisualStyleBackColor = false;
        this.btnCopyFiles.Click += new System.EventHandler(this.btnCopyFiles_Click);

        // lblStatus
        this.lblStatus.AutoSize = true;
        this.lblStatus.Location = new System.Drawing.Point(12, 398);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(0, 0);
        this.lblStatus.ForeColor = System.Drawing.Color.Gray;

        // notifyIcon
        this.notifyIcon.Icon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        this.notifyIcon.Text = "Database Finder";
        this.notifyIcon.ContextMenuStrip = trayMenu;
        this.notifyIcon.Visible = false;
        this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);

        // trayMenu
        this.trayMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.miShow, this.miRefresh, this.miExit});
        this.trayMenu.Name = "trayMenu";

        this.miShow.Text = "نمایش پنجره";
        this.miShow.Click += new System.EventHandler(this.miShow_Click);

        this.miRefresh.Text = "تشخیص مجدد";
        this.miRefresh.Click += new System.EventHandler(this.miRefresh_Click);

        this.miExit.Text = "خروج";
        this.miExit.Click += new System.EventHandler(this.miExit_Click);

        // Form1
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 420);
        this.Controls.Add(this.btnTest);
        this.Controls.Add(this.btnQuery);
        this.Controls.Add(this.btnCopyFiles);
        this.Controls.Add(this.btnRemote);
        this.Controls.Add(this.btnProfiles);
        this.Controls.Add(this.btnSettings);
        this.Controls.Add(this.btnCopy);
        this.Controls.Add(this.btnRefresh);
        this.Controls.Add(this.lblStatus);
        this.Controls.Add(this.dgvDatabases);

        this.Font = new System.Drawing.Font("Segoe UI", 10F);
        this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
        this.RightToLeftLayout = true;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "Database Finder - جستجوی دیتابیس‌های در حال اجرا";
        this.Resize += new System.EventHandler(this.Form1_Resize);
        this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
        this.Load += new System.EventHandler(this.Form1_Load);

        ((System.ComponentModel.ISupportInitialize)(this.dgvDatabases)).EndInit();
        this.trayMenu.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.DataGridView dgvDatabases;
    private System.Windows.Forms.Button btnRefresh;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Button btnCopy;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.Button btnSettings;
    private System.Windows.Forms.Button btnProfiles;
    private System.Windows.Forms.Button btnTest;
    private System.Windows.Forms.Button btnQuery;
    private System.Windows.Forms.Button btnRemote;
    private System.Windows.Forms.Button btnCopyFiles;
    private System.Windows.Forms.ComboBox cmbScanMode;
    private System.Windows.Forms.NotifyIcon notifyIcon;
    private System.Windows.Forms.ContextMenuStrip trayMenu;
    private System.Windows.Forms.ToolStripMenuItem miShow;
    private System.Windows.Forms.ToolStripMenuItem miRefresh;
    private System.Windows.Forms.ToolStripMenuItem miExit;
    private System.Windows.Forms.DataGridViewTextBoxColumn colType;
    private System.Windows.Forms.DataGridViewCheckBoxColumn colCheck;
    private System.Windows.Forms.DataGridViewTextBoxColumn colVersion;
    private System.Windows.Forms.DataGridViewTextBoxColumn colPort;
    private System.Windows.Forms.DataGridViewTextBoxColumn colService;
    private System.Windows.Forms.DataGridViewTextBoxColumn colProcess;
    private System.Windows.Forms.DataGridViewTextBoxColumn colHow;
    private System.Windows.Forms.DataGridViewTextBoxColumn colLocation;
    private System.Windows.Forms.DataGridViewTextBoxColumn colSizeInfo;
}
