using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using AuthBot;
using System.Configuration;
using AuthBot.Dialogs;
using System.Threading;
using O365Bot.Services;
using Autofac;

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
                // Run authentication dialog
                await context.Forward(new AzureAuthDialog(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"]), this.ResumeAfterAuth, message, CancellationToken.None);
            }
            else
            {
                using (var scope = WebApiApplication.Container.BeginLifetimeScope())
                {
                    // Resolve IEventService to concrete type. Pass IDialogContext object to constructor
                    IEventService service = scope.Resolve<IEventService>(new TypedParameter(typeof(IDialogContext), context));
                    var events = await service.GetEvents();
                    foreach (var @event in events)
                    {
                        await context.PostAsync($"{@event.Start.DateTime}-{@event.End.DateTime}: {@event.Subject}");
                    }
                }
            }
        }

        private async Task ResumeAfterAuth(IDialogContext context, IAwaitable<string> result)
        {
            context.Wait(MessageReceivedAsync);
        }
    }
}