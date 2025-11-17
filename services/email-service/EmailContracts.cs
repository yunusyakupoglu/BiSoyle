internal record LicenseEmailRequest
{
    public MailSettingsDto MailSettings { get; init; } = new();
    public LicenseEmailModel Model { get; init; } = new();
}

internal record MailSettingsDto
{
    public string Host { get; init; } = "";
    public int Port { get; init; } = 587;
    public string Security { get; init; } = "STARTTLS";
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string FromEmail { get; init; } = "";
    public string? FromName { get; init; }
    public string? ReplyTo { get; init; }
}

internal record LicenseEmailModel
{
    public string TenantName { get; init; } = "";
    public string PlanName { get; init; } = "";
    public string LicenseKey { get; init; } = "";
    public int MaxUsers { get; init; }
    public int MaxDevices { get; init; }
    public string ToEmail { get; init; } = "";
    public string? ToName { get; init; }
    public string? AdminEmail { get; init; }
    public string? AdminUsername { get; init; }
    public string? AdminPassword { get; init; }
    public string? PaymentReference { get; init; }
    public decimal? Amount { get; init; }
    public string? Currency { get; init; }
    public int? Installment { get; init; }
    public string? PortalUrl { get; init; }
    public string? SupportEmail { get; init; }
    public string? SupportPhone { get; init; }
    public DateTime SubscriptionDate { get; init; } = DateTime.UtcNow;
}







