using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BiSoyle.Tenant.Service.Data;

/// <summary>
/// Lisans bilgilerini içeren entity.
/// Device binding ve activation tracking için kullanılır.
/// </summary>
public class License
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int TenantId { get; set; }
    
    /// <summary>
    /// Lisans anahtarı (unique string identifier)
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string LicenseKey { get; set; } = "";
    
    /// <summary>
    /// Lisans JSON içeriği (base64 encoded)
    /// </summary>
    [Required]
    public string LicenseJson { get; set; } = "";
    
    /// <summary>
    /// RSA imzası (base64 encoded)
    /// </summary>
    [Required]
    public string Signature { get; set; } = "";
    
    /// <summary>
    /// Paket bilgisi (örn: "Professional", "Enterprise")
    /// </summary>
    [MaxLength(100)]
    public string Package { get; set; } = "";
    
    /// <summary>
    /// Maksimum kurulum sayısı
    /// </summary>
    [Required]
    public int MaxInstallations { get; set; } = 1;
    
    /// <summary>
    /// Lisansa bağlı cihaz fingerprint'i (opsiyonel - lisans oluşturulurken belirlenebilir)
    /// </summary>
    [MaxLength(256)]
    public string? DeviceFingerprint { get; set; }
    
    /// <summary>
    /// Tolerance threshold (0.0-1.0) - Device fingerprint eşleşmesi için tolerans
    /// Örn: 0.8 = %80 eşleşme yeterli
    /// </summary>
    public double ToleranceThreshold { get; set; } = 1.0; // Varsayılan: tam eşleşme
    
    /// <summary>
    /// Lisans aktif mi?
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Oluşturulma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Son güncellenme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public Tenant Tenant { get; set; } = null!;
    public List<LicenseActivation> Activations { get; set; } = new();
}

/// <summary>
/// Lisans aktivasyon kayıtları.
/// Her cihaz kurulumu için bir activation kaydı oluşturulur.
/// </summary>
public class LicenseActivation
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int LicenseId { get; set; }
    
    /// <summary>
    /// Cihaz fingerprint'i (SHA-256 hash)
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string DeviceFingerprint { get; set; } = "";
    
    /// <summary>
    /// TPM Public Key (opsiyonel, base64 encoded PEM/DER format)
    /// </summary>
    public string? TpmPublicKey { get; set; }
    
    /// <summary>
    /// Aktivasyon durumu
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Aktivasyon tarihi
    /// </summary>
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Son kullanım tarihi (heartbeat)
    /// </summary>
    public DateTime? LastSeenAt { get; set; }
    
    /// <summary>
    /// Cihaz adı/bilgisi (opsiyonel)
    /// </summary>
    [MaxLength(200)]
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// IP adresi (aktivasyon sırasında)
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }
    
    // Navigation property
    public License License { get; set; } = null!;
}

/// <summary>
/// Device rebind (yeniden bağlama) istekleri.
/// Cihaz değişikliği veya tolerance mismatch durumlarında kullanılır.
/// </summary>
public class RebindRequest
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public int LicenseId { get; set; }
    
    /// <summary>
    /// Eski device fingerprint
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string OldDeviceFingerprint { get; set; } = "";
    
    /// <summary>
    /// Yeni device fingerprint
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string NewDeviceFingerprint { get; set; } = "";
    
    /// <summary>
    /// İstek durumu (Pending, Approved, Rejected)
    /// </summary>
    [MaxLength(50)]
    public string Status { get; set; } = "Pending";
    
    /// <summary>
    /// İstek açıklaması
    /// </summary>
    [MaxLength(500)]
    public string? Reason { get; set; }
    
    /// <summary>
    /// İstek tarihi
    /// </summary>
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Onay/Red tarihi
    /// </summary>
    public DateTime? ProcessedAt { get; set; }
    
    /// <summary>
    /// Onaylayan kullanıcı ID (opsiyonel)
    /// </summary>
    public int? ProcessedByUserId { get; set; }
    
    // Navigation property
    public License License { get; set; } = null!;
}






