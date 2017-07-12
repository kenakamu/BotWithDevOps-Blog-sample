﻿using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.FormFlow;
using Microsoft.Graph;
using O365Bot.Models;
using O365Bot.Services;
using System;
using System.Threading.Tasks;

namespace O365Bot.Dialogs
{
    [Serializable]
    public class CreateEventDialog : IDialog<bool> // このダイアログが完了時に返す型
    {
        public async Task StartAsync(IDialogContext context)
        {
            // Create a from
            var outlookEventFormDialog = FormDialog.FromForm(this.BuildOutlookEventForm, FormOptions.PromptInStart);
            context.Call(outlookEventFormDialog, this.ResumeAfterDialog);
        }

        private async Task ResumeAfterDialog(IDialogContext context, IAwaitable<OutlookEvent> result)
        {
            await context.PostAsync("The event is created.");

            // Complete the child dialog.
            context.Done(true);
        }

        private IForm<OutlookEvent> BuildOutlookEventForm()
        {
            OnCompletionAsyncDelegate<OutlookEvent> processOutlookEventCreate = async (context, state) =>
            {
                using (var scope = WebApiApplication.Container.BeginLifetimeScope())
                {
                    IEventService service = scope.Resolve<IEventService>(new TypedParameter(typeof(IDialogContext), context));
                    Event @event = new Event()
                    {
                        Subject = state.Subject,
                        Start = new DateTimeTimeZone() { DateTime = state.Start.ToString(), TimeZone = "Tokyo Standard Time" },
                        IsAllDay = state.IsAllDay,
                        End = state.IsAllDay ? null : new DateTimeTimeZone() { DateTime = state.Start.AddHours(state.Hours).ToString(), TimeZone = "Tokyo Standard Time" },
                        Body = new ItemBody() { Content = state.Description, ContentType = BodyType.Text }
                    };
                    await service.CreateEvent(@event);
                }
            };

            return new FormBuilder<OutlookEvent>()
                .Message("Creating an event.")
                .Field(nameof(OutlookEvent.Subject), prompt: "What is the title?", validate: async (state, value) =>
                {
                    var subject = (string)value;
                    var result = new ValidateResult() { IsValid = true, Value = subject };
                    if (subject.Contains("FormFlow"))
                    {
                        result.IsValid = false;
                        result.Feedback = "You cannot include FormFlow as subject.";
                    }
                    return result;

                })
                .Field(nameof(OutlookEvent.Description), prompt: "What is the detail?")
                .Field(nameof(OutlookEvent.Start), prompt: "When do you start? Use dd/MM/yyyy HH:mm format.")
                .Field(nameof(OutlookEvent.IsAllDay), prompt: "Is this all day event?{||}")
                .Field(nameof(OutlookEvent.Hours), prompt: "How many hours?", active: (state) =>
                {
                    // If this is all day event, then do not display hours field.
                    if (state.IsAllDay)
                        return false;
                    else
                        return true;
                })                
                .OnCompletion(processOutlookEventCreate)
                .Build();
        }
    }
}