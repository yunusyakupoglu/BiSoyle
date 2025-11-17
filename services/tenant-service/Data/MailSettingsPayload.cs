namespace BiSoyle.Tenant.Service.Data;

public class MailSettingsPayload
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















