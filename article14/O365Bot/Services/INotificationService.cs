using System.Threading.Tasks;

namespace O365Bot.Services
{
    public interface INotificationService
    {
        Task<string> SubscribeEventChange();
        Task RenewSubscribeEventChange(string subscriptionId);
    }
}