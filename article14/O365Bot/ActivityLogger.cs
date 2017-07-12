using Microsoft.Bot.Builder.History;
using Microsoft.Bot.Connector;
using System.Diagnostics;
using System.Threading.Tasks;

namespace O365Bot.Services
{
    public class ActivityLogger : IActivityLogger
    {
        public async Task LogAsync(IActivity activity)
        {
            Debug.WriteLine($"From:{activity.From.Id} - To:{activity.Recipient.Id} - Message:{activity.AsMessageActivity()?.Text}");
        }
    }
}