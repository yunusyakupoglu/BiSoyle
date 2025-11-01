namespace BiSoyleAdminGUI;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    // Labels
    private Label lblGateway;
    private Label lblReceiptService;
    private Label lblProductService;
    private Label lblTransactionService;
    private Label lblUserService;
    private Label lblTenantService;
    private Label lblVoiceService;
    private Label lblFrontend;

    // Buttons
    private Button btnStartGateway;
    private Button btnStopGateway;
    private Button btnStartReceipt;
    private Button btnStopReceipt;
    private Button btnStartProduct;
    private Button btnStopProduct;
    private Button btnStartTransaction;
    private Button btnStopTransaction;
    private Button btnStartUser;
    private Button btnStopUser;
    private Button btnStartTenant;
    private Button btnStopTenant;
    private Button btnStartVoice;
    private Button btnStopVoice;
    private Button btnStartFrontend;
    private Button btnStopFrontend;

    // Control Buttons
    private Button btnStartAll;
    private Button btnStopAll;
    private Button btnOpenFrontend;
    private Button btnRefresh;

    // Log
    private RichTextBox txtLog;

    // Panels
    private Panel pnlHeader;
    private Panel pnlServices;
    private Panel pnlLog;

    // Header Labels
    private Label lblTitle;
    private Label lblSubtitle;

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
        
        // Form
        this.AutoScaleDimensions = new SizeF(7F, 15F);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.ClientSize = new Size(1000, 700);
        this.Text = "BiSoyle - Service Manager";
        this.StartPosition = FormStartPosition.CenterScreen;
        this.MinimumSize = new Size(800, 600);

        // Header Panel
        pnlHeader = new Panel();
        pnlHeader.Dock = DockStyle.Top;
        pnlHeader.Height = 100;
        pnlHeader.BackColor = Color.FromArgb(0, 120, 215);
        
        lblTitle = new Label();
        lblTitle.Text = "BiSoyle Service Manager";
        lblTitle.Font = new Font("Segoe UI", 20, FontStyle.Bold);
        lblTitle.ForeColor = Color.White;
        lblTitle.AutoSize = true;
        lblTitle.Location = new Point(20, 15);
        
        lblSubtitle = new Label();
        lblSubtitle.Text = "Microservice Yönetim Arayüzü - HostEagle Information Technologies";
        lblSubtitle.Font = new Font("Segoe UI", 10);
        lblSubtitle.ForeColor = Color.FromArgb(200, 230, 255);
        lblSubtitle.AutoSize = true;
        lblSubtitle.Location = new Point(20, 50);
        
        pnlHeader.Controls.Add(lblTitle);
        pnlHeader.Controls.Add(lblSubtitle);
        this.Controls.Add(pnlHeader);

        // Services Panel
        pnlServices = new Panel();
        pnlServices.Dock = DockStyle.Top;
        pnlServices.Height = 350;
        pnlServices.Padding = new Padding(10);
        pnlServices.AutoScroll = true;
        this.Controls.Add(pnlServices);

        // Create service rows
        CreateServiceRow(pnlServices, "API Gateway", "localhost:5000", 10, 
            out lblGateway, out btnStartGateway, out btnStopGateway);
        btnStartGateway.Click += BtnStartGateway_Click;
        btnStopGateway.Click += BtnStopGateway_Click;
        
        CreateServiceRow(pnlServices, "Receipt Service", "localhost:5001", 50,
            out lblReceiptService, out btnStartReceipt, out btnStopReceipt);
        btnStartReceipt.Click += BtnStartReceipt_Click;
        btnStopReceipt.Click += BtnStopReceipt_Click;
        
        CreateServiceRow(pnlServices, "Product Service", "localhost:5002", 90,
            out lblProductService, out btnStartProduct, out btnStopProduct);
        btnStartProduct.Click += BtnStartProduct_Click;
        btnStopProduct.Click += BtnStopProduct_Click;
        
        CreateServiceRow(pnlServices, "Transaction Service", "localhost:5003", 130,
            out lblTransactionService, out btnStartTransaction, out btnStopTransaction);
        btnStartTransaction.Click += BtnStartTransaction_Click;
        btnStopTransaction.Click += BtnStopTransaction_Click;
        
        CreateServiceRow(pnlServices, "User Service", "localhost:5004", 170,
            out lblUserService, out btnStartUser, out btnStopUser);
        btnStartUser.Click += BtnStartUser_Click;
        btnStopUser.Click += BtnStopUser_Click;
        
        CreateServiceRow(pnlServices, "Tenant Service", "localhost:5005", 210,
            out lblTenantService, out btnStartTenant, out btnStopTenant);
        btnStartTenant.Click += BtnStartTenant_Click;
        btnStopTenant.Click += BtnStopTenant_Click;
        
        CreateServiceRow(pnlServices, "Voice Service", "localhost:8765/8766", 250,
            out lblVoiceService, out btnStartVoice, out btnStopVoice);
        btnStartVoice.Click += BtnStartVoice_Click;
        btnStopVoice.Click += BtnStopVoice_Click;
        
        CreateServiceRow(pnlServices, "Frontend (Angular)", "localhost:4200", 290,
            out lblFrontend, out btnStartFrontend, out btnStopFrontend);
        btnStartFrontend.Click += BtnStartFrontend_Click;
        btnStopFrontend.Click += BtnStopFrontend_Click;

        // Control Buttons Panel
        var pnlControls = new Panel();
        pnlControls.Dock = DockStyle.Top;
        pnlControls.Height = 60;
        pnlControls.Padding = new Padding(10);
        pnlControls.BackColor = Color.FromArgb(245, 245, 245);
        this.Controls.Add(pnlControls);

        btnStartAll = new Button();
        btnStartAll.Text = "▶ Start All";
        btnStartAll.Size = new Size(100, 35);
        btnStartAll.Location = new Point(10, 12);
        btnStartAll.BackColor = Color.LimeGreen;
        btnStartAll.ForeColor = Color.White;
        btnStartAll.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        btnStartAll.FlatStyle = FlatStyle.Flat;
        btnStartAll.Click += BtnStartAll_Click;
        pnlControls.Controls.Add(btnStartAll);

        btnStopAll = new Button();
        btnStopAll.Text = "■ Stop All";
        btnStopAll.Size = new Size(100, 35);
        btnStopAll.Location = new Point(120, 12);
        btnStopAll.BackColor = Color.Red;
        btnStopAll.ForeColor = Color.White;
        btnStopAll.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        btnStopAll.FlatStyle = FlatStyle.Flat;
        btnStopAll.Click += BtnStopAll_Click;
        pnlControls.Controls.Add(btnStopAll);

        btnRefresh = new Button();
        btnRefresh.Text = "⟳ Refresh";
        btnRefresh.Size = new Size(100, 35);
        btnRefresh.Location = new Point(230, 12);
        btnRefresh.BackColor = Color.DodgerBlue;
        btnRefresh.ForeColor = Color.White;
        btnRefresh.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        btnRefresh.FlatStyle = FlatStyle.Flat;
        btnRefresh.Click += BtnRefresh_Click;
        pnlControls.Controls.Add(btnRefresh);

        btnOpenFrontend = new Button();
        btnOpenFrontend.Text = "🌐 Open Frontend";
        btnOpenFrontend.Size = new Size(120, 35);
        btnOpenFrontend.Location = new Point(340, 12);
        btnOpenFrontend.BackColor = Color.DarkBlue;
        btnOpenFrontend.ForeColor = Color.White;
        btnOpenFrontend.Font = new Font("Segoe UI", 9, FontStyle.Bold);
        btnOpenFrontend.FlatStyle = FlatStyle.Flat;
        btnOpenFrontend.Click += BtnOpenFrontend_Click;
        pnlControls.Controls.Add(btnOpenFrontend);

        // Log Panel
        pnlLog = new Panel();
        pnlLog.Dock = DockStyle.Fill;
        pnlLog.Padding = new Padding(10);
        this.Controls.Add(pnlLog);

        var lblLogTitle = new Label();
        lblLogTitle.Text = "Service Logs";
        lblLogTitle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
        lblLogTitle.AutoSize = true;
        lblLogTitle.Location = new Point(10, 10);
        pnlLog.Controls.Add(lblLogTitle);

        txtLog = new RichTextBox();
        txtLog.ReadOnly = true;
        txtLog.Font = new Font("Consolas", 9);
        txtLog.Location = new Point(10, 35);
        txtLog.Size = new Size(960, 200);
        txtLog.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        pnlLog.Controls.Add(txtLog);
    }

    private void CreateServiceRow(Control parent, string name, string url, int y, 
        out Label statusLabel, out Button startBtn, out Button stopBtn)
    {
        // Service Name
        var lblName = new Label();
        lblName.Text = name;
        lblName.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        lblName.Location = new Point(10, y);
        lblName.Size = new Size(200, 23);
        parent.Controls.Add(lblName);

        // URL
        var lblUrl = new Label();
        lblUrl.Text = url;
        lblUrl.Font = new Font("Segoe UI", 9);
        lblUrl.ForeColor = Color.Gray;
        lblUrl.Location = new Point(220, y + 2);
        lblUrl.Size = new Size(200, 23);
        parent.Controls.Add(lblUrl);

        // Status Label
        statusLabel = new Label();
        statusLabel.Text = "○ Stopped";
        statusLabel.ForeColor = Color.Red;
        statusLabel.Font = new Font("Segoe UI", 10, FontStyle.Bold);
        statusLabel.Location = new Point(430, y);
        statusLabel.Size = new Size(120, 23);
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        parent.Controls.Add(statusLabel);

        // Start Button
        startBtn = new Button();
        startBtn.Text = "▶";
        startBtn.Size = new Size(35, 30);
        startBtn.Location = new Point(560, y - 3);
        startBtn.BackColor = Color.LimeGreen;
        startBtn.ForeColor = Color.White;
        startBtn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        startBtn.FlatStyle = FlatStyle.Flat;
        startBtn.Tag = name; // Store service name for event handling
        parent.Controls.Add(startBtn);

        // Stop Button
        stopBtn = new Button();
        stopBtn.Text = "■";
        stopBtn.Size = new Size(35, 30);
        stopBtn.Location = new Point(605, y - 3);
        stopBtn.BackColor = Color.Red;
        stopBtn.ForeColor = Color.White;
        stopBtn.Font = new Font("Segoe UI", 12, FontStyle.Bold);
        stopBtn.FlatStyle = FlatStyle.Flat;
        stopBtn.Enabled = false;
        stopBtn.Tag = name; // Store service name for event handling
        parent.Controls.Add(stopBtn);
    }
}

