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

namespace O365Bot.Services
{
    public class GraphService : IEventService
    {
        IDialogContext context;
        public GraphService(IDialogContext context)
        {
            this.context = context;
        }
        
        /// <summary>
        /// Get events for next 7 days.
        /// </summary>
        /// <returns></returns>
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