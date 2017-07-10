using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using O365Bot.Services;
using System;
using System.Threading.Tasks;

namespace O365Bot.Dialogs
{
    [Serializable]
    public class GetEventsDialog : IDialog<bool> // the type of returend value.
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);
            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {
            var message = await result as Activity;

            using (var scope = WebApiApplication.Container.BeginLifetimeScope())
            {
                // Resolve IEventService by passing IDialog context for constructor.
                IEventService service = scope.Resolve<IEventService>(new TypedParameter(typeof(IDialogContext), context));
                var events = await service.GetEvents();
                foreach (var @event in events)
                {
                    await context.PostAsync($"{@event.Start.DateTime}-{@event.End.DateTime}: {@event.Subject}");
                }
            }

            // Complete the child dialog
            context.Done(true);
        }
    }
}