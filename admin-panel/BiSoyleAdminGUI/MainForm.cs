using System.Diagnostics;

namespace BiSoyleAdminGUI;

public partial class MainForm : Form
{
    private readonly Dictionary<int, Process?> _serviceProcesses = new();
    private readonly Dictionary<int, bool> _serviceStatus = new();
    private System.Windows.Forms.Timer? _statusTimer;
    
    public MainForm()
    {
        InitializeComponent();
        InitializeServices();
        StartStatusMonitoring();
    }

    private void InitializeServices()
    {
        // Servisleri dictionary'ye ekle
        _serviceProcesses[5000] = null; // Gateway
        _serviceProcesses[5001] = null; // Receipt Service
        _serviceProcesses[5002] = null; // Product Service
        _serviceProcesses[5003] = null; // Transaction Service
        _serviceProcesses[5004] = null; // User Service
        _serviceProcesses[5005] = null; // Tenant Service
        _serviceProcesses[8765] = null; // Voice Service HTTP
        _serviceProcesses[8766] = null; // Voice Service WebSocket
        _serviceProcesses[4200] = null; // Frontend

        // İlk durumu kontrol et
        CheckAllServices();
    }

    private void StartStatusMonitoring()
    {
        _statusTimer = new System.Windows.Forms.Timer();
        _statusTimer.Interval = 5000; // 5 saniyede bir kontrol et
        _statusTimer.Tick += (s, e) => CheckAllServices();
        _statusTimer.Start();
    }

    private void CheckAllServices()
    {
        CheckService(5000, lblGateway, btnStartGateway, btnStopGateway);
        CheckService(5001, lblReceiptService, btnStartReceipt, btnStopReceipt);
        CheckService(5002, lblProductService, btnStartProduct, btnStopProduct);
        CheckService(5003, lblTransactionService, btnStartTransaction, btnStopTransaction);
        CheckService(5004, lblUserService, btnStartUser, btnStopUser);
        CheckService(5005, lblTenantService, btnStartTenant, btnStopTenant);
        CheckService(8765, lblVoiceService, btnStartVoice, btnStopVoice);
        CheckService(4200, lblFrontend, btnStartFrontend, btnStopFrontend);
    }

    private void CheckService(int port, Label statusLabel, Button startBtn, Button stopBtn)
    {
        var isRunning = IsPortListening(port);
        _serviceStatus[port] = isRunning;
        
        if (statusLabel.InvokeRequired)
        {
            statusLabel.Invoke(() =>
            {
                UpdateServiceUI(statusLabel, startBtn, stopBtn, isRunning);
            });
        }
        else
        {
            UpdateServiceUI(statusLabel, startBtn, stopBtn, isRunning);
        }
    }

    private bool IsPortListening(int port)
    {
        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            
            return output.Contains($":{port}") && output.Contains("LISTENING");
        }
        catch
        {
            return false;
        }
    }

    private void UpdateServiceUI(Label statusLabel, Button startBtn, Button stopBtn, bool isRunning)
    {
        if (isRunning)
        {
            statusLabel.Text = "● Running";
            statusLabel.ForeColor = Color.LimeGreen;
            statusLabel.Font = new Font(statusLabel.Font, FontStyle.Bold);
            startBtn.Enabled = false;
            stopBtn.Enabled = true;
        }
        else
        {
            statusLabel.Text = "○ Stopped";
            statusLabel.ForeColor = Color.Red;
            statusLabel.Font = new Font(statusLabel.Font, FontStyle.Regular);
            startBtn.Enabled = true;
            stopBtn.Enabled = false;
        }
    }

    private void StartService(string name, int port, string path, string command)
    {
        try
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoExit -Command \"cd {path}; Write-Host '{name}' -ForegroundColor Green; {command}\"",
                WorkingDirectory = Path.GetDirectoryName(path) ?? Environment.CurrentDirectory,
                CreateNoWindow = false,
                UseShellExecute = true
            };

            var process = Process.Start(processStartInfo);
            _serviceProcesses[port] = process;
            
            LogMessage($"[{DateTime.Now:HH:mm:ss}] {name} başlatılıyor...", Color.Blue);
        }
        catch (Exception ex)
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] Hata: {name} başlatılamadı - {ex.Message}", Color.Red);
        }
    }

    private void StopService(string name, int port)
    {
        try
        {
            // İlgili portu dinleyen process'i bul ve kapat
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netstat",
                    Arguments = "-ano",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };
            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            var lines = output.Split('\n');
            foreach (var line in lines)
            {
                if (line.Contains($":{port}") && line.Contains("LISTENING"))
                {
                    var parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 4)
                    {
                        var pid = parts[parts.Length - 1];
                        if (int.TryParse(pid, out var processId))
                        {
                            try
                            {
                                var proc = Process.GetProcessById(processId);
                                proc.Kill();
                                LogMessage($"[{DateTime.Now:HH:mm:ss}] {name} durduruldu (PID: {processId})", Color.Orange);
                            }
                            catch { }
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogMessage($"[{DateTime.Now:HH:mm:ss}] Hata: {name} durdurulamadı - {ex.Message}", Color.Red);
        }
    }

    private void LogMessage(string message, Color color)
    {
        if (txtLog.InvokeRequired)
        {
            txtLog.Invoke(() =>
            {
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.SelectionLength = 0;
                txtLog.SelectionColor = color;
                txtLog.AppendText(message + Environment.NewLine);
                txtLog.SelectionColor = txtLog.ForeColor;
                txtLog.ScrollToCaret();
            });
        }
        else
        {
            txtLog.SelectionStart = txtLog.TextLength;
            txtLog.SelectionLength = 0;
            txtLog.SelectionColor = color;
            txtLog.AppendText(message + Environment.NewLine);
            txtLog.SelectionColor = txtLog.ForeColor;
            txtLog.ScrollToCaret();
        }
    }

    // Gateway Events
    private void BtnStartGateway_Click(object? sender, EventArgs e)
        => StartService("GATEWAY", 5000, @"gateway", "dotnet run");

    private void BtnStopGateway_Click(object? sender, EventArgs e)
        => StopService("GATEWAY", 5000);

    // Receipt Service Events
    private void BtnStartReceipt_Click(object? sender, EventArgs e)
        => StartService("RECEIPT SERVICE", 5001, @"services\receipt-service", "dotnet run");

    private void BtnStopReceipt_Click(object? sender, EventArgs e)
        => StopService("RECEIPT SERVICE", 5001);

    // Product Service Events
    private void BtnStartProduct_Click(object? sender, EventArgs e)
        => StartService("PRODUCT SERVICE", 5002, @"services\product-service", "dotnet run");

    private void BtnStopProduct_Click(object? sender, EventArgs e)
        => StopService("PRODUCT SERVICE", 5002);

    // Transaction Service Events
    private void BtnStartTransaction_Click(object? sender, EventArgs e)
        => StartService("TRANSACTION SERVICE", 5003, @"services\transaction-service", "dotnet run");

    private void BtnStopTransaction_Click(object? sender, EventArgs e)
        => StopService("TRANSACTION SERVICE", 5003);

    // User Service Events
    private void BtnStartUser_Click(object? sender, EventArgs e)
        => StartService("USER SERVICE", 5004, @"services\user-service", "dotnet run");

    private void BtnStopUser_Click(object? sender, EventArgs e)
        => StopService("USER SERVICE", 5004);

    // Tenant Service Events
    private void BtnStartTenant_Click(object? sender, EventArgs e)
        => StartService("TENANT SERVICE", 5005, @"services\tenant-service", "dotnet run");

    private void BtnStopTenant_Click(object? sender, EventArgs e)
        => StopService("TENANT SERVICE", 5005);

    // Voice Service Events
    private void BtnStartVoice_Click(object? sender, EventArgs e)
        => StartService("VOICE SERVICE", 8765, @"services\voice-service", "python main.py");

    private void BtnStopVoice_Click(object? sender, EventArgs e)
        => StopService("VOICE SERVICE", 8765);

    // Frontend Events
    private void BtnStartFrontend_Click(object? sender, EventArgs e)
        => StartService("FRONTEND", 4200, @"frontend\Admin", "npm start");

    private void BtnStopFrontend_Click(object? sender, EventArgs e)
        => StopService("FRONTEND", 4200);

    // Control Buttons
    private void BtnStartAll_Click(object? sender, EventArgs e)
    {
        LogMessage($"[{DateTime.Now:HH:mm:ss}] Tüm servisler başlatılıyor...", Color.Blue);
        BtnStartGateway_Click(sender, e);
        System.Threading.Thread.Sleep(500);
        BtnStartReceipt_Click(sender, e);
        System.Threading.Thread.Sleep(500);
        BtnStartProduct_Click(sender, e);
        System.Threading.Thread.Sleep(500);
        BtnStartTransaction_Click(sender, e);
        System.Threading.Thread.Sleep(500);
        BtnStartUser_Click(sender, e);
        System.Threading.Thread.Sleep(500);
        BtnStartTenant_Click(sender, e);
        System.Threading.Thread.Sleep(500);
        BtnStartVoice_Click(sender, e);
        System.Threading.Thread.Sleep(1000);
        BtnStartFrontend_Click(sender, e);
        LogMessage($"[{DateTime.Now:HH:mm:ss}] Tüm servisler başlatma komutu verildi!", Color.Green);
    }

    private void BtnStopAll_Click(object? sender, EventArgs e)
    {
        LogMessage($"[{DateTime.Now:HH:mm:ss}] Tüm servisler durduruluyor...", Color.Orange);
        BtnStopGateway_Click(sender, e);
        BtnStopReceipt_Click(sender, e);
        BtnStopProduct_Click(sender, e);
        BtnStopTransaction_Click(sender, e);
        BtnStopUser_Click(sender, e);
        BtnStopTenant_Click(sender, e);
        BtnStopVoice_Click(sender, e);
        BtnStopFrontend_Click(sender, e);
        LogMessage($"[{DateTime.Now:HH:mm:ss}] Tüm servisler durdurma komutu verildi!", Color.Green);
    }

    private void BtnOpenFrontend_Click(object? sender, EventArgs e)
    {
        Process.Start(new ProcessStartInfo("http://localhost:4200") { UseShellExecute = true });
    }

    private void BtnRefresh_Click(object? sender, EventArgs e)
    {
        CheckAllServices();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _statusTimer?.Stop();
        base.OnFormClosing(e);
    }
}

