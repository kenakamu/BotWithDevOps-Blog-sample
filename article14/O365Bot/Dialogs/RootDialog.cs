using AuthBot;
using AuthBot.Dialogs;
using Autofac;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using O365Bot.Services;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace O365Bot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var message = await result as Activity;

            // Check authentication
            if (string.IsNullOrEmpty(await context.GetAccessToken(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"])))
            {
                // Store the original message.
                context.PrivateConversationData.SetValue<Activity>("OriginalMessage", message as Activity);
                // Run authentication dialog.
                await context.Forward(new AzureAuthDialog(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"]), this.ResumeAfterAuth, message, CancellationToken.None);
            }
            else
            {
                await DoWork(context, message);
            }
        }
        

        private async Task DoWork(IDialogContext context, IMessageActivity message)
        {
            if (message.ChannelId != "emulator")
            {
                using (var scope = WebApiApplication.Container.BeginLifetimeScope())
                {
                    var service = scope.Resolve<INotificationService>(new TypedParameter(typeof(IDialogContext), context));

                    // Subscribe to Office 365 event change
                    var subscriptionId = context.UserData.GetValueOrDefault<string>("SubscriptionId", "");
                    if (string.IsNullOrEmpty(subscriptionId))
                    {
                        // Subscribe to Microsoft Graph Notification and get SubscriptionId
                        subscriptionId = await service.SubscribeEventChange();
                        context.UserData.SetValue("SubscriptionId", subscriptionId);
                    }
                    else
                        await service.RenewSubscribeEventChange(subscriptionId);

                    // Convert current message as ConversationReference.
                    var conversationReference = message.ToConversationReference();

                    // Map the ConversationReference to SubscriptionId of Microsoft Graph Notification.
                    if (CacheService.caches.ContainsKey(subscriptionId))
                        CacheService.caches[subscriptionId] = conversationReference;
                    else
                        CacheService.caches.Add(subscriptionId, conversationReference);
                    // Store locale info as conversation info doesn't store it.
                    if (!CacheService.caches.ContainsKey(message.From.Id))
                        CacheService.caches.Add(message.From.Id, Thread.CurrentThread.CurrentCulture.Name);
                }
            }

            if (message.Text.Contains("get"))
                // Chain to GetEventDialog
                await context.Forward(new GetEventsDialog(), ResumeAfterDialog, message, CancellationToken.None);
            else if (message.Text.Contains("add"))
                // Chain to CreateEventDialog
                context.Call(new CreateEventDialog(), ResumeAfterDialog);
        }

        private async Task ResumeAfterDialog(IDialogContext context, IAwaitable<bool> result)
        {
            // Get the dialog result
            var dialogResult = await result;
            context.Wait(MessageReceivedAsync);
        }

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            // Restore the original message.
            var message = context.PrivateConversationData.GetValue<Activity>("OriginalMessage");
            await DoWork(context, message);
        }
    }
}

