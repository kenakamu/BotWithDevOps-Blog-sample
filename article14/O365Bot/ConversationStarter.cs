using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Connector;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace O365Bot
{
    public class ConversationStarter
    {
        /// <summary>
        /// Insert the dialog on current conversation.
        /// </summary>
        public static async Task Resume(Activity message, IDialog<object> dialog)
        {
            var client = new ConnectorClient(new Uri(message.ServiceUrl));

            using (var scope = DialogModule.BeginLifetimeScope(Conversation.Container, message))
            {
                var botData = scope.Resolve<IBotData>();
                await botData.LoadAsync(CancellationToken.None);
                var task = scope.Resolve<IDialogTask>();
                
                task.Call(dialog.Void<object, IMessageActivity>(), null);
                await task.PollAsync(CancellationToken.None);
                await botData.FlushAsync(CancellationToken.None);
            }
        }
    }
}