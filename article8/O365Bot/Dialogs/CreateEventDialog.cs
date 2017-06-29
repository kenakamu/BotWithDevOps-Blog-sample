using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;
using O365Bot.Services;
using System;
using System.Globalization;
using System.Threading.Tasks;

namespace O365Bot.Dialogs
{
    [Serializable]
    public class CreateEventDialog : IDialog<bool>
    {
        private string subject;
        private string detail;
        private DateTime start;
        private bool isAllDay;
        private double hours;

        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync("Creating an event.");
            // Ask for text input
            PromptDialog.Text(context, ResumeAfterTitle, "What is the title?");
        }

        private async Task ResumeAfterTitle(IDialogContext context, IAwaitable<string> result)
        {
            subject = await result;
            // Ask for text input
            PromptDialog.Text(context, ResumeAfterDetail, "What is the detail?");
        }

        private async Task ResumeAfterDetail(IDialogContext context, IAwaitable<string> result)
        {
            detail = await result;
            // As DialogPrompt cannot ask for datetime, ask for text input instead.
            PromptDialog.Text(context, ResumeAfterStard, "When do you start? Use dd/MM/yyyy HH:mm format.");
        }

        private async Task ResumeAfterStard(IDialogContext context, IAwaitable<string> result)
        {
            // Verify the input, and retry if failed.
            if (!DateTime.TryParseExact(await result, "dd/MM/yyyy HH:mm", CultureInfo.CurrentCulture, DateTimeStyles.None, out start))
            {
                PromptDialog.Text(context, ResumeAfterStard, "Wrong format. Use dd/MM/yyyy HH:mm format.");
            }
            // Ask for confirmation. If input validation fails, retry up to 3 times.
            PromptDialog.Confirm(context, ResumeAfterIsAllDay, "Is this all day event?", "Please select the choice.");
        }

        private async Task ResumeAfterIsAllDay(IDialogContext context, IAwaitable<bool> result)
        {
            isAllDay = await result;
            if (isAllDay)
                await CreateEvent(context);
            else
                // Ask for number.
                PromptDialog.Number(context, ResumeAfterHours, "How many hours?", "Please answer by number");
        }

        private async Task ResumeAfterHours(IDialogContext context, IAwaitable<long> result)
        {
            hours = await result;
            await CreateEvent(context);
        }

        private async Task CreateEvent(IDialogContext context)
        {
            using (var scope = WebApiApplication.Container.BeginLifetimeScope())
            {
                IEventService service = scope.Resolve<IEventService>(new TypedParameter(typeof(IDialogContext), context));
                // We can get TimeZone by using https://graph.microsoft.com/beta/me/mailboxSettings, but just hard-coding here for test purpose.
                Event @event = new Event()
                {
                    Subject = subject,
                    Start = new DateTimeTimeZone() { DateTime = start.ToString(), TimeZone = "Tokyo Standard Time" },
                    IsAllDay = isAllDay,
                    End = isAllDay ? null : new DateTimeTimeZone() { DateTime = start.AddHours(hours).ToString(), TimeZone = "Tokyo Standard Time" },
                    Body = new ItemBody() { Content = detail, ContentType = BodyType.Text }
                };
                await service.CreateEvent(@event);
                await context.PostAsync("The event is created.");
            }

            // Complete the child dialog.
            context.Done(true);
        }
    }
}