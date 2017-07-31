using AuthBot;
using AuthBot.Dialogs;
using Autofac;
using Microsoft.Bot.Builder.ConnectorEx;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Connector;
using O365Bot.Services;
using System;
using System.Configuration;
using System.Threading;
using System.Threading.Tasks;

namespace O365Bot.Dialogs
{
    [LuisModel("LUIS APP ID", "LUIS SUBSCIPTION KEY")]
    [Serializable]
    public class LuisRootDialog : LuisDialog<object>
    {
        LuisResult luisResult;

        [LuisIntent("Calendar.Find")]
        public async Task GetEvents(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            this.luisResult = result;
            var message = await activity;

            // Check authentication
            if (string.IsNullOrEmpty(await context.GetAccessToken(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"])))
            {
                // Store luisDialog
                luisResult = result;
                await Authenticate(context, message);
            }
            else
            {
                await SubscribeEventChange(context, message);
                await context.Forward(new GetEventsDialog(), ResumeAfterDialog, message, CancellationToken.None);
            }
        }

        [LuisIntent("Calendar.Add")]
        public async Task CreateEvent(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            var message = await activity;
            // Check authentication
            if (string.IsNullOrEmpty(await context.GetAccessToken(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"])))
            {
                // Store luisDialog
                luisResult = result;
                await Authenticate(context, message);
            }
            else
            {
                await SubscribeEventChange(context, message);
                context.Call(new CreateEventDialog(result), ResumeAfterDialog);
            }
        }

        [LuisIntent("None")]
        public async Task NoneHandler(IDialogContext context, IAwaitable<IMessageActivity> activity, LuisResult result)
        {
            await context.PostAsync("Cannot understand");
        }

        private async Task Authenticate(IDialogContext context, IMessageActivity message)
        {
            // Store the original message.
            context.PrivateConversationData.SetValue<Activity>("OriginalMessage", message as Activity);
            // Run authentication dialog.
            await context.Forward(new AzureAuthDialog(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"]), this.ResumeAfterAuth, message, CancellationToken.None);
        }


        private async Task SubscribeEventChange(IDialogContext context, IMessageActivity message)
        {
            if (message.ChannelId != "emulator" && message.ChannelId != "directline")
            {
                using (var scope = WebApiApplication.Container.BeginLifetimeScope())
                {
                    var service = scope.Resolve<INotificationService>(new TypedParameter(typeof(IDialogContext), context));

                    // Subscribe to Office 365 event change
                    var subscriptionId = context.UserData.GetValueOrDefault<string>("SubscriptionId", "");
                    if (string.IsNullOrEmpty(subscriptionId))
                    {
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
        }

        private async Task ResumeAfterDialog(IDialogContext context, IAwaitable<bool> result)
        {
            // Get the dialog result
            var dialogResult = await result;
            context.Done(true);
        }

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            // Restore the original message.
            var message = context.PrivateConversationData.GetValue<Activity>("OriginalMessage");
            await SubscribeEventChange(context, message);
            switch (luisResult.TopScoringIntent.Intent)
            {
                case "Calendar.Find":
                    await context.Forward(new GetEventsDialog(), ResumeAfterDialog, message, CancellationToken.None);
                    break;
                case "Calendar.Add":
                    context.Call(new CreateEventDialog(luisResult), ResumeAfterDialog);
                    break;
                case "None":
                    await context.PostAsync("Cannot understand");
                    break;
            }
        }
    }
}