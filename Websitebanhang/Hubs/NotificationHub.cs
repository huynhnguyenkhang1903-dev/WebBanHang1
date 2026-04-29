using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace Websitebanhang.Hubs
{
    public class NotificationHub : Hub
    {
        // Hub này không cần thiết kế logic phức tạp vì chủ yếu server sẽ push thông báo tới client
        // Client sẽ nhận thông báo thông qua các hàm như "ReceiveNotification"
    }
}
