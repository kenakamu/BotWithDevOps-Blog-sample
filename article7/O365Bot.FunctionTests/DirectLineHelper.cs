using Microsoft.Bot.Connector.DirectLine;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace O365Bot.FunctionTests
{
    public class DirectLineHelper
    {
        private TestContext testContext;

        private string conversationId;
        private string userId;
        private string watermark;

        private DirectLineClient client;

        public DirectLineHelper(TestContext testContext)
        {
            client = new DirectLineClient(testContext.Properties["DirectLineSecret"].ToString());
            userId = testContext.Properties["UserId"].ToString();
            conversationId = client.Conversations.StartConversation().ConversationId;
            watermark = null;
        }

        public List<Activity> SentMessage(string text)
        {
            Activity activity = new Activity()
            {
                Type = ActivityTypes.Message,
                From = new ChannelAccount(userId, userId),
                Text = text
            };
            client.Conversations.PostActivity(conversationId, activity);
            var reply = client.Conversations.GetActivities(conversationId, watermark);

            watermark = reply.Watermark;
            return reply.Activities.Where(x => x.From.Id != userId).ToList();
        }
    }
}