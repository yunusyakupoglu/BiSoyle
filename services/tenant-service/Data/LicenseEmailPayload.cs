namespace BiSoyle.Tenant.Service.Data;

public class LicenseEmailPayload
{
    public string TenantName { get; set; } = "";
    public string PlanName { get; set; } = "";
    public string LicenseKey { get; set; } = "";
    public int MaxUsers { get; set; }
    public int MaxDevices { get; set; }
    public string ToEmail { get; set; } = "";
    public string? ToName { get; set; }
    public string PortalUrl { get; set; } = "";
    public string SupportEmail { get; set; } = "";
    public string SupportPhone { get; set; } = "";
    public string? AdminEmail { get; set; }
    public string? AdminUsername { get; set; }
    public string? AdminPassword { get; set; }
    public string? PaymentReference { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public int? Installment { get; set; }
    public DateTime SubscriptionDate { get; set; } = DateTime.UtcNow;
}







