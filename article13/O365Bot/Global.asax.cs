using Autofac;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Internals.Fibers;
using O365Bot.Services;
using System.Configuration;
using System.Web.Http;

namespace O365Bot
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        public static IContainer Container;

        protected void Application_Start()
        {
            this.RegisterBotModules();
            GlobalConfiguration.Configure(WebApiConfig.Register);

            AuthBot.Models.AuthSettings.Mode = ConfigurationManager.AppSettings["ActiveDirectory.Mode"];
            AuthBot.Models.AuthSettings.EndpointUrl = ConfigurationManager.AppSettings["ActiveDirectory.EndpointUrl"];
            AuthBot.Models.AuthSettings.Tenant = ConfigurationManager.AppSettings["ActiveDirectory.Tenant"];
            AuthBot.Models.AuthSettings.RedirectUrl = ConfigurationManager.AppSettings["ActiveDirectory.RedirectUrl"];
            AuthBot.Models.AuthSettings.ClientId = ConfigurationManager.AppSettings["ActiveDirectory.ClientId"];
            AuthBot.Models.AuthSettings.ClientSecret = ConfigurationManager.AppSettings["ActiveDirectory.ClientSecret"];

            var builder = new ContainerBuilder();
            builder.RegisterType<GraphService>().As<IEventService>();
            Container = builder.Build();
        }

        private void RegisterBotModules()
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new ReflectionSurrogateModule());
            builder.RegisterModule<GlobalMessageHandlers>();
            builder.Update(Conversation.Container);
        }
    }
}