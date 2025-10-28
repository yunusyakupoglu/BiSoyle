using Microsoft.AspNetCore.SignalR;

namespace BiSoyle.Receipt.Service.Hubs;

public class ReceiptHub : Hub
{
    public async Task SendReceiptUpdate(string islemKodu, string status)
    {
        await Clients.All.SendAsync("receiptUpdated", islemKodu, status);
    }

    public async Task SendReceiptComplete(string islemKodu, string pdfPath)
    {
        await Clients.All.SendAsync("receiptComplete", islemKodu, pdfPath);
    }

    public override async Task OnConnectedAsync()
    {
        await Clients.Caller.SendAsync("connected", Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        await base.OnDisconnectedAsync(exception);
    }
}





