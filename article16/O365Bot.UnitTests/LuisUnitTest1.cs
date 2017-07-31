using Autofac;
using Microsoft.Bot.Builder.Base;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.Tests;
using Microsoft.Bot.Connector;
using Microsoft.Graph;
using Microsoft.QualityTools.Testing.Fakes;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json.Linq;
using O365Bot.Dialogs;
using O365Bot.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace O365Bot.UnitTests
{
    [TestClass]
    public class SampleLuisTest : LuisTestBase
    { 
        [TestMethod]
        public async Task ShouldReturnEvents()
        {
            // Instantiate ShimsContext to use Fakes 
            using (ShimsContext.Create())
            {
                // Return "dummyToken" when calling GetAccessToken method 
                AuthBot.Fakes.ShimContextExtensions.GetAccessTokenIBotContextString =
                    async (a, e) => { return "dummyToken"; };


                // Mock the LUIS service
                var luis1 = new Mock<ILuisService>();
                // Mock other services
                var mockEventService = new Mock<IEventService>();
                mockEventService.Setup(x => x.GetEvents()).ReturnsAsync(new List<Event>()
                {
                    new Event
                    {
                        Subject = "dummy event",
                        Start = new DateTimeTimeZone()
                        {
                            DateTime = "2017-05-31 12:00",
                            TimeZone = "Standard Tokyo Time"
                        },
                        End = new DateTimeTimeZone()
                        {
                            DateTime = "2017-05-31 13:00",
                            TimeZone = "Standard Tokyo Time"
                        }
                    }
                });
                var subscriptionId = Guid.NewGuid().ToString();
                var mockNotificationService = new Mock<INotificationService>();
                mockNotificationService.Setup(x => x.SubscribeEventChange()).ReturnsAsync(subscriptionId);
                mockNotificationService.Setup(x => x.RenewSubscribeEventChange(It.IsAny<string>())).Returns(Task.FromResult(true));

                var builder = new ContainerBuilder();
                builder.RegisterInstance(mockEventService.Object).As<IEventService>();
                builder.RegisterInstance(mockNotificationService.Object).As<INotificationService>();
                WebApiApplication.Container = builder.Build();

                /// Instantiate dialog to test
                LuisRootDialog rootDialog = new LuisRootDialog();

                // Create in-memory bot environment
                Func<IDialog<object>> MakeRoot = () => rootDialog;
                using (new FiberTestBase.ResolveMoqAssembly(luis1.Object))
                using (var container = Build(Options.ResolveDialogFromContainer, luis1.Object))
                {
                    var dialogBuilder = new ContainerBuilder();
                    dialogBuilder
                        .RegisterInstance(rootDialog)
                        .As<IDialog<object>>();
                    dialogBuilder.Update(container);

                    // Register global message handler
                    RegisterBotModules(container);

                    // Specify "Calendar.Find" intent as LUIS result
                    SetupLuis<LuisRootDialog>(luis1, d => d.GetEvents(null, null, null), 1.0, new EntityRecommendation(type: "Calendar.Find"));

                    // Create a message to send to bot
                    var toBot = DialogTestBase.MakeTestMessage();
                    toBot.From.Id = Guid.NewGuid().ToString();
                    toBot.Text = "get events";

                    // Send message and check the answer.
                    IMessageActivity toUser = await GetResponse(container, MakeRoot, toBot);

                    // Verify the result
                    Assert.IsTrue(toUser.Text.Equals("2017-05-31 12:00-2017-05-31 13:00: dummy event"));
                }
            }
        }

        [TestMethod]
        public async Task ShouldCreateAllDayEvent()
        {
            // Instantiate ShimsContext to use Fakes 
            using (ShimsContext.Create())
            {
                // Return "dummyToken" when calling GetAccessToken method 
                AuthBot.Fakes.ShimContextExtensions.GetAccessTokenIBotContextString =
                    async (a, e) => { return "dummyToken"; };


                // Mock the LUIS service
                var luis1 = new Mock<ILuisService>();
                // Mock other services
                var mockEventService = new Mock<IEventService>();
                mockEventService.Setup(x => x.CreateEvent(It.IsAny<Event>())).Returns(Task.FromResult(true));
                var subscriptionId = Guid.NewGuid().ToString();
                var mockNotificationService = new Mock<INotificationService>();
                mockNotificationService.Setup(x => x.SubscribeEventChange()).ReturnsAsync(subscriptionId);
                mockNotificationService.Setup(x => x.RenewSubscribeEventChange(It.IsAny<string>())).Returns(Task.FromResult(true));

                var builder = new ContainerBuilder();
                builder.RegisterInstance(mockEventService.Object).As<IEventService>();
                builder.RegisterInstance(mockNotificationService.Object).As<INotificationService>();
                WebApiApplication.Container = builder.Build();

                /// Instantiate dialog to test
                LuisRootDialog rootDialog = new LuisRootDialog();

                // Create in-memory bot environment
                Func<IDialog<object>> MakeRoot = () => rootDialog;
                using (new FiberTestBase.ResolveMoqAssembly(luis1.Object))
                using (var container = Build(Options.ResolveDialogFromContainer, luis1.Object))
                {
                    var dialogBuilder = new ContainerBuilder();
                    dialogBuilder
                        .RegisterInstance(rootDialog)
                        .As<IDialog<object>>();
                    dialogBuilder.Update(container);

                    // Register global message handler
                    RegisterBotModules(container);

                    // create datetimeV2 resolution
                    Dictionary<string, object> resolution = new Dictionary<string, object>();
                    JArray values = new JArray();
                    Dictionary<string, object> resolutionData = new Dictionary<string, object>();
                    resolutionData.Add("type", "datetime");
                    resolutionData.Add("value", DateTime.Now.AddDays(1));
                    values.Add(JToken.FromObject(resolutionData));
                    resolution.Add("values", values);

                    // Specify "Calendar.Find" intent as LUIS result
                    SetupLuis<LuisRootDialog>(luis1, d => d.GetEvents(null, null, null), 1.0, 
                        new EntityRecommendation(type: "Calendar.Subject", entity: "dummy subject"),
                        new EntityRecommendation(type: "builtin.datetimeV2.datetime", resolution: resolution));

                    // Create a message to send to bot
                    var toBot = DialogTestBase.MakeTestMessage();
                    toBot.From.Id = Guid.NewGuid().ToString();
                    toBot.Text = "eat dinner with my wife at outback stakehouse at shinagawa at 7 pm next Wednesday";

                    // Send message and check the answer.
                    var toUser = await GetResponses(container, MakeRoot, toBot);

                    // Verify the result
                    Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
                    Assert.IsTrue((toUser[1].Attachments[0].Content as HeroCard).Text.Equals("Is this all day event?"));

                    toBot.Text = "Yes";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("The event is created."));
                }
            }
        }

        [TestMethod]
        public async Task ShouldCreateEvent()
        {
            // Instantiate ShimsContext to use Fakes 
            using (ShimsContext.Create())
            {
                // Return "dummyToken" when calling GetAccessToken method 
                AuthBot.Fakes.ShimContextExtensions.GetAccessTokenIBotContextString =
                    async (a, e) => { return "dummyToken"; };


                // Mock the LUIS service
                var luis1 = new Mock<ILuisService>();
                // Mock other services
                var mockEventService = new Mock<IEventService>();
                mockEventService.Setup(x => x.CreateEvent(It.IsAny<Event>())).Returns(Task.FromResult(true));
                var subscriptionId = Guid.NewGuid().ToString();
                var mockNotificationService = new Mock<INotificationService>();
                mockNotificationService.Setup(x => x.SubscribeEventChange()).ReturnsAsync(subscriptionId);
                mockNotificationService.Setup(x => x.RenewSubscribeEventChange(It.IsAny<string>())).Returns(Task.FromResult(true));

                var builder = new ContainerBuilder();
                builder.RegisterInstance(mockEventService.Object).As<IEventService>();
                builder.RegisterInstance(mockNotificationService.Object).As<INotificationService>();
                WebApiApplication.Container = builder.Build();

                /// Instantiate dialog to test
                LuisRootDialog rootDialog = new LuisRootDialog();

                // Create in-memory bot environment
                Func<IDialog<object>> MakeRoot = () => rootDialog;
                using (new FiberTestBase.ResolveMoqAssembly(luis1.Object))
                using (var container = Build(Options.ResolveDialogFromContainer, luis1.Object))
                {
                    var dialogBuilder = new ContainerBuilder();
                    dialogBuilder
                        .RegisterInstance(rootDialog)
                        .As<IDialog<object>>();
                    dialogBuilder.Update(container);

                    // Register global message handler
                    RegisterBotModules(container);

                    // create datetimeV2 resolution
                    Dictionary<string, object> resolution = new Dictionary<string, object>();
                    JArray values = new JArray();
                    Dictionary<string, object> resolutionData = new Dictionary<string, object>();
                    resolutionData.Add("type", "datetime");
                    resolutionData.Add("value", DateTime.Now.AddDays(1));
                    values.Add(JToken.FromObject(resolutionData));
                    resolution.Add("values", values);

                    // Specify "Calendar.Find" intent as LUIS result
                    SetupLuis<LuisRootDialog>(luis1, d => d.GetEvents(null, null, null), 1.0,
                        new EntityRecommendation(type: "Calendar.Subject", entity: "dummy subject"),
                        new EntityRecommendation(type: "builtin.datetimeV2.datetime", resolution: resolution));

                    // Create a message to send to bot
                    var toBot = DialogTestBase.MakeTestMessage();
                    toBot.From.Id = Guid.NewGuid().ToString();
                    toBot.Text = "eat dinner with my wife at outback stakehouse at shinagawa at 7 pm next Wednesday";

                    // Send message and check the answer.
                    var toUser = await GetResponses(container, MakeRoot, toBot);

                    // Verify the result
                    Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
                    Assert.IsTrue((toUser[1].Attachments[0].Content as HeroCard).Text.Equals("Is this all day event?"));

                    toBot.Text = "No";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("How many hours?"));

                    toBot.Text = "3";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("The event is created."));
                }
            }
        }

        /// <summary>
        /// Send a message to the bot and get repsponse.
        /// </summary>
        public async Task<IMessageActivity> GetResponse(IContainer container, Func<IDialog<object>> makeRoot, IMessageActivity toBot)
        {
            using (var scope = DialogModule.BeginLifetimeScope(container, toBot))
            {
                DialogModule_MakeRoot.Register(scope, makeRoot);

                // act: sending the message
                using (new LocalizedScope(toBot.Locale))
                {
                    var task = scope.Resolve<IPostToBot>();
                    await task.PostAsync(toBot, CancellationToken.None);
                }
                //await Conversation.SendAsync(toBot, makeRoot, CancellationToken.None);
                return scope.Resolve<Queue<IMessageActivity>>().Dequeue();
            }
        }

        /// <summary>
        /// Send a message to the bot and get all repsponses.
        /// </summary>
        public async Task<List<IMessageActivity>> GetResponses(IContainer container, Func<IDialog<object>> makeRoot, IMessageActivity toBot)
        {
            using (var scope = DialogModule.BeginLifetimeScope(container, toBot))
            {
                var results = new List<IMessageActivity>();
                DialogModule_MakeRoot.Register(scope, makeRoot);

                // act: sending the message
                using (new LocalizedScope(toBot.Locale))
                {
                    var task = scope.Resolve<IPostToBot>();
                    await task.PostAsync(toBot, CancellationToken.None);
                }
                //await Conversation.SendAsync(toBot, makeRoot, CancellationToken.None);
                var queue = scope.Resolve<Queue<IMessageActivity>>();
                while (queue.Count != 0)
                {
                    results.Add(queue.Dequeue());
                }

                return results;
            }
        }

        /// <summary>
        /// Register Global Message
        /// </summary>
        private void RegisterBotModules(IContainer container)
        {
            var builder = new ContainerBuilder();
            builder.RegisterModule(new ReflectionSurrogateModule());
            builder.RegisterModule<GlobalMessageHandlers>();
            builder.RegisterType<ActivityLogger>().AsImplementedInterfaces().InstancePerDependency();
            builder.Update(container);
        }

        /// <summary>
        /// Resume the conversation
        /// </summary>
        public async Task<List<IMessageActivity>> Resume(IContainer container, IDialog<object> dialog, IMessageActivity toBot)
        {
            using (var scope = DialogModule.BeginLifetimeScope(container, toBot))
            {
                var results = new List<IMessageActivity>();

                var botData = scope.Resolve<IBotData>();
                await botData.LoadAsync(CancellationToken.None);
                var task = scope.Resolve<IDialogTask>();

                // Insert dialog to current event
                task.Call(dialog.Void<object, IMessageActivity>(), null);
                await task.PollAsync(CancellationToken.None);
                await botData.FlushAsync(CancellationToken.None);

                // Get the result
                var queue = scope.Resolve<Queue<IMessageActivity>>();
                while (queue.Count != 0)
                {
                    results.Add(queue.Dequeue());
                }

                return results;
            }
        }
    }
}