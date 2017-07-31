using AuthBot;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Graph;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;

namespace O365Bot.Services
{
    public class GraphService : IEventService, INotificationService
    {
        IDialogContext context;
        public GraphService(IDialogContext context)
        {
            this.context = context;
        }

        public async Task CreateEvent(Event @event)
        {
            var client = await GetClient();

            try
            {
                var events = await client.Me.Events.Request().AddAsync(@event);
            }
            catch (Exception ex)
            {
            }
        }

        public async Task<Event> GetEvent(string id)
        {
            var client = await GetClient();

            var @event = await client.Me.Events[id].Request().GetAsync();

            return @event;
        }

        public async Task<List<Event>> GetEvents()
        {
            var events = new List<Event>();
            var client = await GetClient();

            try
            {
                var calendarView = await client.Me.CalendarView.Request(new List<Option>()
                {
                    new QueryOption("startdatetime", DateTime.Now.ToString("yyyy/MM/ddTHH:mm:ssZ")),
                    new QueryOption("enddatetime", DateTime.Now.AddDays(7).ToString("yyyy/MM/ddTHH:mm:ssZ"))
                }).GetAsync();

                events = calendarView.CurrentPage.ToList();
            }
            catch (Exception ex)
            {
            }

            return events;
        }

        public async Task<string> SubscribeEventChange()
        {
            var client = await GetClient();
            var url = HttpContext.Current.Request.Url;
            if (url.Host == "localhost")
                return "";

            var webHookUrl = $"{url.Scheme}://{url.Host}:{url.Port}/api/Notifications";

            var res = await client.Subscriptions.Request().AddAsync(
            new Subscription()
            {
                ChangeType = "updated, deleted",
                NotificationUrl = webHookUrl,
                ExpirationDateTime = DateTime.Now.AddDays(1),
                Resource = $"me/events",
                ClientState = "event update or delete"
            });

            return res.Id;
        }

        public async Task RenewSubscribeEventChange(string subscriptionId)
        {
            var client = await GetClient();
            var subscription = new Subscription()
            {
                ExpirationDateTime = DateTime.Now.AddDays(1),
            };

            var res = await client.Subscriptions[subscriptionId].Request().UpdateAsync(subscription);
        }

        private async Task<GraphServiceClient> GetClient()
        {
            GraphServiceClient client = new GraphServiceClient(new DelegateAuthenticationProvider(AuthProvider));
            return client;
        }

        private async Task AuthProvider(HttpRequestMessage request)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue(
            "bearer", await context.GetAccessToken(ConfigurationManager.AppSettings["ActiveDirectory.ResourceId"]));
        }
    }
}