using Microsoft.Bot.Connector;
using Microsoft.Graph;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using O365Bot.Dialogs;
using O365Bot.Services;
using System;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using cg = System.Collections.Generic;
namespace O365Bot
{
    public class NotificationsController : ApiController
    {
        public async Task<HttpResponseMessage> Post(object obj)
        {
            var response = Request.CreateResponse(HttpStatusCode.OK);

            // Verify the webhook subscription.
            if (Request.RequestUri.Query.Contains("validationToken"))
            {
                response.Content = new StringContent(Request.RequestUri.Query.Split('=')[1], Encoding.UTF8, "text/plain");
            }
            else
            {
                var subscriptions = JsonConvert.DeserializeObject<cg.List<Subscription>>(JToken.Parse(obj.ToString())["value"].ToString());
                try
                {
                    foreach (var subscription in subscriptions)
                    {
                        if (CacheService.caches.ContainsKey(subscription.AdditionalData["subscriptionId"].ToString()))
                        {
                            // Get ConversationReference by using SubscriptionId.
                            var conversationReference = CacheService.caches[subscription.AdditionalData["subscriptionId"].ToString()] as ConversationReference;
                            // Get the event id.
                            var id = ((dynamic)subscription.AdditionalData["resourceData"]).id.ToString();

                            // Get local id and set it.
                            var activity = conversationReference.GetPostToBotMessage();
                            var locale = CacheService.caches[activity.From.Id].ToString();
                            Thread.CurrentThread.CurrentCulture = new CultureInfo(locale);
                            Thread.CurrentThread.CurrentUICulture = new CultureInfo(locale);

                            // Interrupt current conversation.
                            await ConversationStarter.Resume(
                                activity,
                                new NotifyEventChageDialog(id));
                        }
                        var resp = new HttpResponseMessage(HttpStatusCode.OK);
                        return resp;
                    }
                }
                catch (Exception ex)
                {
                    return Request.CreateErrorResponse(HttpStatusCode.BadRequest, ex);
                }

            }
            return response;
        }
    }
}