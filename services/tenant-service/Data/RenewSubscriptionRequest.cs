namespace BiSoyle.Tenant.Service.Data
{
	public class RenewSubscriptionRequest
	{
		public int TenantId { get; set; }
		public string PaymentReference { get; set; } = string.Empty;
	}
}










