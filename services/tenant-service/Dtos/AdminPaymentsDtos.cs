namespace BiSoyle.Tenant.Service.Dtos
{
	public record PaymentSummaryDto(
		int totalTenants,
		int activeTenants,
		int overdueTenants,
		int deactivatedTenants,
		decimal paidThisMonth,
		int paymentsThisMonth
	);

	public record MonthlyPaymentPoint(string month, decimal total, int count);
}










