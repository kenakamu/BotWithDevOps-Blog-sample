using Microsoft.Bot.Connector.DirectLine;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace O365Bot.AuthConsole
{
    class Program
    {
        static void Main(string[] args)
        {
            var userId = ConfigurationManager.AppSettings["UserId"];
            var directLineSecret = ConfigurationManager.AppSettings["DirectLineSecret"];
            DirectLineClient client = new DirectLineClient(directLineSecret);
            var conversation = client.Conversations.StartConversation();
            Activity activity = new Activity()
            {
                Type = ActivityTypes.Message,
                From = new ChannelAccount(userId, userId),
                Text = "Hi"
            };
            client.Conversations.PostActivity(conversation.ConversationId, activity);
            var reply = client.Conversations.GetActivities(conversation.ConversationId, null);
            var authReply = reply.Activities.Where(x => x.From.Id != userId).First();
            if (authReply.Attachments != null &&
                authReply.Attachments.First().ContentType != "application/vnd.microsoft.card.signin")
                return;

            Console.WriteLine((authReply.Attachments.First().Content as dynamic).buttons[0].value);
            Console.WriteLine("Copy the address, past to browser and authenticate, then past the displayed code back");
            var code = Console.ReadLine();

            activity = new Activity()
            {
                Type = ActivityTypes.Message,
                From = new ChannelAccount(userId, userId),
                Text = code
            };

            client.Conversations.PostActivity(conversation.ConversationId, activity);
            return;
        }
    }
}