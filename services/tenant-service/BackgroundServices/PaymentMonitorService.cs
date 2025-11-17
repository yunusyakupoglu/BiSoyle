using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using BiSoyle.Tenant.Service.Data;

namespace BiSoyle.Tenant.Service.BackgroundServices
{
	public class PaymentMonitorService : BackgroundService
	{
		private readonly IServiceProvider _serviceProvider;
		private readonly ILogger<PaymentMonitorService> _logger;

		public PaymentMonitorService(IServiceProvider serviceProvider, ILogger<PaymentMonitorService> logger)
		{
			_serviceProvider = serviceProvider;
			_logger = logger;
		}

		protected override async Task ExecuteAsync(CancellationToken stoppingToken)
		{
			while (!stoppingToken.IsCancellationRequested)
			{
				try
				{
					using var scope = _serviceProvider.CreateScope();
					var db = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

					var now = DateTime.UtcNow;
					var tenants = await db.Tenants.ToListAsync(stoppingToken);

					foreach (var tenant in tenants)
					{
						var lastSuccessPayment = await db.SubscriptionPayments
							.Where(p => p.TenantId == tenant.Id && p.Durum == "Basarili")
							.OrderByDescending(p => p.OnayTarihi ?? p.OlusturmaTarihi)
							.FirstOrDefaultAsync(stoppingToken);

						var referenceDate = lastSuccessPayment?.OnayTarihi ?? tenant.OlusturmaTarihi;
						var dueDate = referenceDate.AddDays(30);
						var graceUntil = dueDate.AddDays(7);

						if (now > graceUntil && tenant.Aktif)
						{
							tenant.Aktif = false;
							_logger.LogInformation("Tenant {TenantId} deactivated due to overdue payment (grace exceeded).", tenant.Id);
						}
					}

					await db.SaveChangesAsync(stoppingToken);
				}
				catch (Exception ex)
				{
					_logger.LogError(ex, "PaymentMonitorService error");
				}

				var nextRun = DateTime.UtcNow.Date.AddDays(1).AddHours(3);
				var delay = nextRun - DateTime.UtcNow;
				if (delay < TimeSpan.FromMinutes(1))
				{
					delay = TimeSpan.FromHours(24);
				}
				try
				{
					await Task.Delay(delay, stoppingToken);
				}
				catch (TaskCanceledException) { }
			}
		}
	}
}










