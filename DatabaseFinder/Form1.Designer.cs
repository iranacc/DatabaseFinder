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
        DataGridViewColumn SQLCol = new DataGridViewColumn();
        this.components = new System.ComponentModel.Container();

        this.dgvDatabases = new System.Windows.Forms.DataGridView();
        this.btnRefresh = new System.Windows.Forms.Button();
        this.lblStatus = new System.Windows.Forms.Label();
        this.btnCopy = new System.Windows.Forms.Button();
        this.lblTitle = new System.Windows.Forms.Label();

        ((System.ComponentModel.ISupportInitialize)(this.dgvDatabases)).BeginInit();
        this.SuspendLayout();

        // dgvDatabases
        this.dgvDatabases.AllowUserToAddRows = false;
        this.dgvDatabases.AllowUserToDeleteRows = false;
        this.dgvDatabases.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
        this.dgvDatabases.AutoGenerateColumns = false;
        this.dgvDatabases.BackgroundColor = System.Drawing.Color.White;
        this.dgvDatabases.Location = new System.Drawing.Point(12, 50);
        this.dgvDatabases.Name = "dgvDatabases";
        this.dgvDatabases.ReadOnly = true;
        this.dgvDatabases.RowHeadersVisible = false;
        this.dgvDatabases.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
        this.dgvDatabases.Size = new System.Drawing.Size(776, 360);
        this.dgvDatabases.TabIndex = 0;
        this.dgvDatabases.CellDoubleClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDatabases_CellDoubleClick);

        // Columns
        this.colType = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colType.HeaderText = "نام دیتابیس";
        this.colType.Name = "colType";
        this.colType.DataPropertyName = "TypeDisplayName";
        this.colType.ReadOnly = true;
        this.colType.Width = 120;

        this.colPort = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colPort.HeaderText = "پورت";
        this.colPort.Name = "colPort";
        this.colPort.DataPropertyName = "Port";
        this.colPort.ReadOnly = true;
        this.colPort.Width = 70;

        this.colService = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colService.HeaderText = "سرویس";
        this.colService.Name = "colService";
        this.colService.DataPropertyName = "ServiceName";
        this.colService.ReadOnly = true;
        this.colService.Width = 170;

        this.colProcess = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colProcess.HeaderText = "پروسس / PID";
        this.colProcess.Name = "colProcess";
        this.colProcess.DataPropertyName = "ProcessDisplay";
        this.colProcess.ReadOnly = true;
        this.colProcess.Width = 150;

        this.colHow = new System.Windows.Forms.DataGridViewTextBoxColumn();
        this.colHow.HeaderText = "نحوه تشخیص";
        this.colHow.Name = "colHow";
        this.colHow.DataPropertyName = "DetectionMethod";
        this.colHow.ReadOnly = true;
        this.colHow.Width = 100;

        this.dgvDatabases.Columns.Add(this.colType);
        this.dgvDatabases.Columns.Add(this.colPort);
        this.dgvDatabases.Columns.Add(this.colService);
        this.dgvDatabases.Columns.Add(this.colProcess);
        this.dgvDatabases.Columns.Add(this.colHow);

        // lblTitle
        this.lblTitle.AutoSize = true;
        this.lblTitle.BackColor = System.Drawing.Color.Transparent;
        this.lblTitle.Font = new System.Drawing.Font("Segoe UI", 15F, System.Drawing.FontStyle.Bold);
        this.lblTitle.ForeColor = System.Drawing.Color.FromArgb(33, 150, 243);
        this.lblTitle.Location = new System.Drawing.Point(10, 9);
        this.lblTitle.Name = "lblTitle";
        this.lblTitle.Size = new System.Drawing.Size(200, 20);
        this.lblTitle.TabIndex = 1;
        this.lblTitle.Text = "Database Finder";

        // lblStatus
        this.lblStatus.AutoSize = true;
        this.lblStatus.Location = new System.Drawing.Point(12, 420);
        this.lblStatus.Name = "lblStatus";
        this.lblStatus.Size = new System.Drawing.Size(0, 0);
        this.lblStatus.TabIndex = 2;

        // btnRefresh
        this.btnRefresh.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnRefresh.BackColor = System.Drawing.Color.FromArgb(33, 150, 243);
        this.btnRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnRefresh.ForeColor = System.Drawing.Color.White;
        this.btnRefresh.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnRefresh.Location = new System.Drawing.Point(714, 12);
        this.btnRefresh.Name = "btnRefresh";
        this.btnRefresh.Size = new System.Drawing.Size(75, 30);
        this.btnRefresh.TabIndex = 3;
        this.btnRefresh.Text = "تشخیص مجدد";
        this.btnRefresh.UseVisualStyleBackColor = false;
        this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);

        // btnCopy
        this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnCopy.BackColor = System.Drawing.Color.FromArgb(76, 175, 80);
        this.btnCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
        this.btnCopy.ForeColor = System.Drawing.Color.White;
        this.btnCopy.Font = new System.Drawing.Font("Segoe UI", 10F, System.Drawing.FontStyle.Bold);
        this.btnCopy.Location = new System.Drawing.Point(600, 12);
        this.btnCopy.Name = "btnCopy";
        this.btnCopy.Size = new System.Drawing.Size(108, 30);
        this.btnCopy.TabIndex = 4;
        this.btnCopy.Text = "کپی لیست";
        this.btnCopy.UseVisualStyleBackColor = false;
        this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);

        // Form1
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(800, 445);
        this.Controls.Add(this.btnCopy);
        this.Controls.Add(this.btnRefresh);
        this.Controls.Add(this.lblStatus);
        this.Controls.Add(this.lblTitle);
        this.Controls.Add(this.dgvDatabases);

        this.Font = new System.Drawing.Font("Segoe UI", 10F);
        this.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
        this.RightToLeftLayout = true;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Text = "جستجوی دیتابیس‌های در حال اجرا";
        this.Load += new System.EventHandler(this.Form1_Load);

        ((System.ComponentModel.ISupportInitialize)(this.dgvDatabases)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    private System.Windows.Forms.DataGridView dgvDatabases;
    private System.Windows.Forms.Button btnRefresh;
    private System.Windows.Forms.Label lblStatus;
    private System.Windows.Forms.Button btnCopy;
    private System.Windows.Forms.Label lblTitle;
    private System.Windows.Forms.DataGridViewTextBoxColumn colType;
    private System.Windows.Forms.DataGridViewTextBoxColumn colPort;
    private System.Windows.Forms.DataGridViewTextBoxColumn colService;
    private System.Windows.Forms.DataGridViewTextBoxColumn colProcess;
    private System.Windows.Forms.DataGridViewTextBoxColumn colHow;
}