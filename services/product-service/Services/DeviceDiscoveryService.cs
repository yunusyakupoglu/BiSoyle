using System.Drawing.Printing;
using NAudio.CoreAudioApi;

namespace BiSoyle.Product.Service.Services;

public class DeviceDiscoveryService
{
    public List<DiscoveredDevice> DiscoverAllDevices()
    {
        var devices = new List<DiscoveredDevice>();
        
        try
        {
            // Yazıcıları tespit et
            devices.AddRange(DiscoverPrinters());
            
            // Mikrofonları tespit et
            devices.AddRange(DiscoverMicrophones());
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Device discovery error: {ex.Message}");
        }
        
        return devices;
    }
    
    private List<DiscoveredDevice> DiscoverPrinters()
    {
        var printers = new List<DiscoveredDevice>();
        
        try
        {
            foreach (string printer in PrinterSettings.InstalledPrinters)
            {
                var printerSettings = new PrinterSettings { PrinterName = printer };
                
                printers.Add(new DiscoveredDevice
                {
                    CihazAdi = printer,
                    CihazTipi = "yazici",
                    Marka = ExtractBrand(printer),
                    Model = printer,
                    BaglantiTipi = DetectConnectionType(printer),
                    Durum = "aktif"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Printer discovery error: {ex.Message}");
        }
        
        return printers;
    }
    
    private List<DiscoveredDevice> DiscoverMicrophones()
    {
        var microphones = new List<DiscoveredDevice>();
        
        try
        {
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);
            
            foreach (var device in devices)
            {
                microphones.Add(new DiscoveredDevice
                {
                    CihazAdi = device.FriendlyName,
                    CihazTipi = "mikrofon",
                    Marka = ExtractBrand(device.FriendlyName),
                    Model = device.FriendlyName,
                    BaglantiTipi = DetectMicrophoneConnectionType(device.FriendlyName),
                    Durum = "aktif"
                });
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Microphone discovery error: {ex.Message}");
        }
        
        return microphones;
    }
    
    private string ExtractBrand(string deviceName)
    {
        // Common printer/mic brands
        var brands = new[] { "HP", "Canon", "Epson", "Brother", "Samsung", "Dell", "Logitech", "Blue", "Rode", "Audio-Technica", "Microsoft", "Realtek", "Creative" };
        
        foreach (var brand in brands)
        {
            if (deviceName.Contains(brand, StringComparison.OrdinalIgnoreCase))
            {
                return brand;
            }
        }
        
        return "Generic";
    }
    
    private string DetectConnectionType(string deviceName)
    {
        if (deviceName.Contains("USB", StringComparison.OrdinalIgnoreCase))
            return "usb";
        if (deviceName.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase) || deviceName.Contains("BT", StringComparison.OrdinalIgnoreCase))
            return "bluetooth";
        if (deviceName.Contains("WiFi", StringComparison.OrdinalIgnoreCase) || deviceName.Contains("Network", StringComparison.OrdinalIgnoreCase))
            return "wifi";
        
        return "usb"; // Default
    }
    
    private string DetectMicrophoneConnectionType(string deviceName)
    {
        if (deviceName.Contains("USB", StringComparison.OrdinalIgnoreCase))
            return "usb";
        if (deviceName.Contains("Bluetooth", StringComparison.OrdinalIgnoreCase))
            return "bluetooth";
        
        return "usb"; // Most microphones are USB
    }
    
    /// <summary>
    /// Windows'ta Bluetooth cihaz bağlantısını test eder
    /// </summary>
    public DeviceTestResult TestBluetoothDevice(string deviceName)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new DeviceTestResult
            {
                Success = false,
                Message = "Bluetooth test sadece Windows'ta çalışır. Docker container içinde test edilemez.",
                IsDocker = true
            };
        }
        
        try
        {
            // PowerShell ile Bluetooth cihazlarını kontrol et
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-Command \"Get-PnpDevice | Where-Object {{ $_.FriendlyName -like '*{deviceName}*' -or $_.FriendlyName -like '*Bluetooth*' }} | Select-Object -First 1 | ConvertTo-Json\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            
            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process == null)
            {
                return new DeviceTestResult
                {
                    Success = false,
                    Message = "PowerShell çalıştırılamadı"
                };
            }
            
            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();
            
            if (!string.IsNullOrWhiteSpace(output) && !output.Contains("\"\":\"\""))
            {
                // Cihaz bulundu
                return new DeviceTestResult
                {
                    Success = true,
                    Message = $"✅ Bluetooth cihaz '{deviceName}' Windows'ta tanınıyor ve aktif görünüyor.",
                    Details = output
                };
            }
            else
            {
                // Cihaz bulunamadı veya bağlı değil
                return new DeviceTestResult
                {
                    Success = false,
                    Message = $"⚠️ Bluetooth cihaz '{deviceName}' Windows'ta bulunamadı veya bağlı değil. Lütfen cihazın açık ve eşleşmiş olduğundan emin olun.",
                    Details = error
                };
            }
        }
        catch (Exception ex)
        {
            return new DeviceTestResult
            {
                Success = false,
                Message = $"Test hatası: {ex.Message}",
                Details = ex.StackTrace ?? ""
            };
        }
    }
}

public class DeviceTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
    public string Details { get; set; } = "";
    public bool IsDocker { get; set; } = false;
}

public class DiscoveredDevice
{
    public string CihazAdi { get; set; } = "";
    public string CihazTipi { get; set; } = "";
    public string Marka { get; set; } = "";
    public string Model { get; set; } = "";
    public string BaglantiTipi { get; set; } = "";
    public string Durum { get; set; } = "";
}

