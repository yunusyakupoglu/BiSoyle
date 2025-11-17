namespace BiSoyle.Tenant.Service;

// REQUEST/RESPONSE MODELS
public class CreateLicenseRequest
{
    public int TenantId { get; set; }
    public string? LicenseKey { get; set; }
    public string? Package { get; set; }
    public int MaxInstallations { get; set; } = 1;
    public string? DeviceFingerprint { get; set; }
    public double ToleranceThreshold { get; set; } = 1.0;
    public DateTime? ExpiresAt { get; set; }
}

public class ActivateLicenseRequest
{
    public string LicenseJson { get; set; } = "";
    public string Signature { get; set; } = "";
    public string DeviceFingerprint { get; set; } = "";
    public string? TpmPublicKey { get; set; }
    public string? DeviceName { get; set; }
    public string? IpAddress { get; set; }
}

public class ChallengeRequest
{
    public int LicenseId { get; set; }
    public int ActivationId { get; set; }
}

public class VerifyChallengeRequest
{
    public int LicenseId { get; set; }
    public int ActivationId { get; set; }
    public string Nonce { get; set; } = "";
    public string TpmSignature { get; set; } = "";
}

public class LicenseData
{
    public string LicenseKey { get; set; } = "";
    public int TenantId { get; set; }
    public string TenantKey { get; set; } = "";
    public string FirmaAdi { get; set; } = "";
    public string Package { get; set; } = "";
    public int MaxInstallations { get; set; }
    public string? DeviceFingerprint { get; set; }
    public double ToleranceThreshold { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
}

public class CreateTenantRequest
{
    public string FirmaAdi { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telefon { get; set; }
    public string? Adres { get; set; }
    public string? VergiNo { get; set; }
}

public class UpdateTenantRequest
{
    public string FirmaAdi { get; set; } = "";
    public string Email { get; set; } = "";
    public string? Telefon { get; set; }
    public string? Adres { get; set; }
    public string? VergiNo { get; set; }
}

public class CreateSubscriptionRequest
{
    public int TenantId { get; set; }
    public int PlanId { get; set; }
    public DateTime BaslangicTarihi { get; set; } = DateTime.UtcNow;
    public DateTime? BitisTarihi { get; set; }
    public string? PaymentReference { get; set; }
    public decimal? OdenenTutar { get; set; }
    public int? TaksitSayisi { get; set; }
    public string? ParaBirimi { get; set; }
}

public class CreateSubscriptionPlanRequest
{
    public string PlanAdi { get; set; } = "";
    public int MaxKullaniciSayisi { get; set; }
    public int MaxBayiSayisi { get; set; }
    public decimal AylikUcret { get; set; }
}

public class UpdateSubscriptionPlanRequest
{
    public string PlanAdi { get; set; } = "";
    public int MaxKullaniciSayisi { get; set; }
    public int MaxBayiSayisi { get; set; }
    public decimal AylikUcret { get; set; }
}

public class ValidateLicenseRequest
{
    public string LicenseKey { get; set; } = "";
}

public class FetchLicenseRequest
{
    public string LicenseKey { get; set; } = "";
}

public class VirtualPosPaymentRequest
{
    public int TenantId { get; set; }
    public int PlanId { get; set; }
    public decimal Tutar { get; set; }
    public string ParaBirimi { get; set; } = "TRY";
    public int TaksitSayisi { get; set; } = 1;
    public string KartNumarasi { get; set; } = "";
    public string SonKullanmaAy { get; set; } = "";
    public string SonKullanmaYil { get; set; } = "";
    public string Cvv { get; set; } = "";
    public string KartSahibi { get; set; } = "";
    public bool UcDSecure { get; set; } = true;
}

public class VirtualPosPaymentResponse
{
    public string Reference { get; set; } = "";
    public string Status { get; set; } = "success";
    public string? BankName { get; set; }
    public decimal ChargedAmount { get; set; }
    public decimal CommissionAmount { get; set; }
    public decimal CommissionRate { get; set; }
    public decimal TotalAmount => ChargedAmount + CommissionAmount;
    public int Installment { get; set; }
    public string Currency { get; set; } = "TRY";
    public string MaskedCard { get; set; } = "";
    public bool ThreeDSecure { get; set; } = true;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = "Ödeme başarılı";
}

public class UpdatePaymentSettingRequest
{
  public string Iban { get; set; } = "";
  public string? BankName { get; set; }
  public string? AccountHolder { get; set; }
}

public class UpdateMailSettingsRequest
{
  public string Host { get; set; } = "";
  public int Port { get; set; } = 587;
  public string Security { get; set; } = "STARTTLS";
  public string Username { get; set; } = "";
  public string? Password { get; set; }
  public string FromEmail { get; set; } = "";
  public string? FromName { get; set; }
  public string? ReplyTo { get; set; }
}

public class UserInfo
{
    public int? Id { get; set; }
    public string? Username { get; set; }
    public string? Email { get; set; }
}






