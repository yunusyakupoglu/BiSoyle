namespace BiSoyle.Tenant.Service.Data
{
	public class GatewaySettingsPayload
	{
		public string Provider { get; set; } = "simulator"; // simulator | paytr | iyzico

		// PayTR
		public string? PaytrMerchantId { get; set; }
		public string? PaytrMerchantKey { get; set; }
		public string? PaytrMerchantSalt { get; set; }

		// Iyzico
		public string? IyzicoApiKey { get; set; }
		public string? IyzicoSecretKey { get; set; }
		public string? IyzicoBaseUrl { get; set; }
	}
}










