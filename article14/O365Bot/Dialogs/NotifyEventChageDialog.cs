using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using O365Bot.Services;
using System;
using System.Threading.Tasks;

namespace O365Bot.Dialogs
{
    [Serializable]
    public class NotifyEventChageDialog : IDialog<object> 
    {
        private string id;
        public NotifyEventChageDialog(string id)
        {
            this.id = id;
        }

        public async Task StartAsync(IDialogContext context)
        {
            PromptDialog.Choice(context, this.AfterSelectOption, new string[] { "Check the detail", "Go back to current conversation." }, "One of your events has been updated.");
        }

        private async Task AfterSelectOption(IDialogContext context, IAwaitable<string> result)
        {
            var answer = await result;

            if (answer == "Check the detail")
            {
                await context.PostAsync("Check the detail");
                using (var scope = WebApiApplication.Container.BeginLifetimeScope())
                {
                    IEventService service = scope.Resolve<IEventService>(new TypedParameter(typeof(IDialogContext), context));
                    var @event = await service.GetEvent(id);
                    await context.PostAsync($"{@event.Start.DateTime}-{@event.End.DateTime}: {@event.Subject}@{@event.Location.DisplayName}-{@event.Body.Content}");
                }
            }

            await context.PostAsync("Going back to the original conversation.");
            context.Done(String.Empty);
        }
    }
}