using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Tests;
using Microsoft.Bot.Connector;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;
using Autofac;
using O365Bot.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Base;
using System.Threading;
using System.Collections.Generic;
using Microsoft.QualityTools.Testing.Fakes;
using O365Bot.Services;
using Moq;
using Microsoft.Graph;
using System.Globalization;
using Microsoft.Bot.Builder.Internals.Fibers;

namespace O365Bot.UnitTests
{
    [TestClass]
    public class SampleDialogTest : DialogTestBase
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
                var builder = new ContainerBuilder();
                builder.RegisterInstance(mockEventService.Object).As<IEventService>();
                WebApiApplication.Container = builder.Build();

                // Instantiate dialog to test
                IDialog<object> rootDialog = new RootDialog();

                // Create in-memory bot environment
                Func<IDialog<object>> MakeRoot = () => rootDialog;
                using (new FiberTestBase.ResolveMoqAssembly(rootDialog))
                using (var container = Build(Options.MockConnectorFactory | Options.ScopedQueue, rootDialog))
                {
                    // Register global message handler
                    RegisterBotModules(container);

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

                // Mock the service and register
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

                // Instantiate dialog to test
                IDialog<object> rootDialog = new RootDialog();

                // Create in-memory bot environment
                Func<IDialog<object>> MakeRoot = () => rootDialog;
                using (new FiberTestBase.ResolveMoqAssembly(rootDialog))
                using (var container = Build(Options.MockConnectorFactory | Options.ScopedQueue, rootDialog))
                {
                    // Register global message handler
                    RegisterBotModules(container);

                    // Create a message to send to bot
                    var toBot = DialogTestBase.MakeTestMessage();
                    // Specify local as US English
                    toBot.Locale = "en-US";
                    toBot.From.Id = Guid.NewGuid().ToString();
                    toBot.Text = "add appointment";

                    // Send message and check the answer.
                    var toUser = await GetResponses(container, MakeRoot, toBot);

                    // Verify the result
                    Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
                    Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));

                    toBot.Text = "Learn BotFramework";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("What is the detail?"));

                    toBot.Text = "Implement O365Bot";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("When do you start? Use dd/MM/yyyy HH:mm format."));

                    toBot.Text = "01/07/2017 13:00";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue((toUser[0].Attachments[0].Content as HeroCard).Text.Equals("Is this all day event?"));

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

                // Mock the service and register
                var mockEventService = new Mock<IEventService>();
                mockEventService.Setup(x => x.CreateEvent(It.IsAny<Event>())).Returns(Task.FromResult(true));
                var mockNotificationService = new Mock<INotificationService>();
                mockNotificationService.Setup(x => x.SubscribeEventChange()).ReturnsAsync(Guid.NewGuid().ToString());
                mockNotificationService.Setup(x => x.RenewSubscribeEventChange(It.IsAny<string>())).Returns(Task.FromResult(true));

                var builder = new ContainerBuilder();
                builder.RegisterInstance(mockEventService.Object).As<IEventService>();
                builder.RegisterInstance(mockNotificationService.Object).As<INotificationService>();
                WebApiApplication.Container = builder.Build();

                // Instantiate dialog to test
                IDialog<object> rootDialog = new RootDialog();

                // Create in-memory bot environment
                Func<IDialog<object>> MakeRoot = () => rootDialog;
                using (new FiberTestBase.ResolveMoqAssembly(rootDialog))
                using (var container = Build(Options.MockConnectorFactory | Options.ScopedQueue, rootDialog))
                {
                    // Register global message handler
                    RegisterBotModules(container);

                    // Create a message to send to bot
                    var toBot = DialogTestBase.MakeTestMessage();
                    // Specify local as US English
                    toBot.Locale = "en-US";
                    toBot.From.Id = Guid.NewGuid().ToString();
                    toBot.Text = "add appointment";

                    // Send message and check the answer.
                    var toUser = await GetResponses(container, MakeRoot, toBot);

                    // Verify the result
                    Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
                    Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));

                    toBot.Text = "Learn BotFramework";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("What is the detail?"));

                    toBot.Text = "Implement O365Bot";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("When do you start? Use dd/MM/yyyy HH:mm format."));

                    toBot.Text = "01/07/2017 13:00";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue((toUser[0].Attachments[0].Content as HeroCard).Text.Equals("Is this all day event?"));

                    toBot.Text = "No";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("How many hours?"));


                    toBot.Text = "4";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("The event is created."));
                }
            }
        }

        [TestMethod]
        public async Task ShouldCancelCurrrentDialog()
        {
            // Instantiate ShimsContext to use Fakes 
            using (ShimsContext.Create())
            {
                // Return "dummyToken" when calling GetAccessToken method 
                AuthBot.Fakes.ShimContextExtensions.GetAccessTokenIBotContextString =
 async (a, e) => { return "dummyToken"; };

                // Mock the service and register
                var mockEventService = new Mock<IEventService>();
                mockEventService.Setup(x => x.CreateEvent(It.IsAny<Event>())).Returns(Task.FromResult(true));
                var mockNotificationService = new Mock<INotificationService>();
                mockNotificationService.Setup(x => x.SubscribeEventChange()).ReturnsAsync(Guid.NewGuid().ToString());
                mockNotificationService.Setup(x => x.RenewSubscribeEventChange(It.IsAny<string>())).Returns(Task.FromResult(true));

                var builder = new ContainerBuilder();
                builder.RegisterInstance(mockEventService.Object).As<IEventService>();
                builder.RegisterInstance(mockNotificationService.Object).As<INotificationService>();
                WebApiApplication.Container = builder.Build();

                // Instantiate dialog to test
                IDialog<object> rootDialog = new RootDialog();

                // Create in-memory bot environment
                Func<IDialog<object>> MakeRoot = () => rootDialog;
                using (new FiberTestBase.ResolveMoqAssembly(rootDialog))
                using (var container = Build(Options.MockConnectorFactory | Options.ScopedQueue, rootDialog))
                {
                    // Register global message handler
                    RegisterBotModules(container);
                                        
                    // Create a message to send to bot
                    var toBot = DialogTestBase.MakeTestMessage();
                    // Specify local as US English
                    toBot.Locale = "en-US";
                    toBot.From.Id = Guid.NewGuid().ToString();
                    toBot.Text = "add appointment";

                    // Send message and check the answer.
                    var toUser = await GetResponses(container, MakeRoot, toBot);

                    // Verify the result
                    Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
                    Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));

                    toBot.Text = "Learn BotFramework";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("What is the detail?"));

                    toBot.Text = "Cancel";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser.Count.Equals(0));

                    toBot.Text = "add appointment";
                    toUser = await GetResponses(container, MakeRoot, toBot);

                    // Verify the result
                    Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
                    Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));
                }
            }
        }

        [TestMethod]
        public async Task ShouldInterruptCurrentDialog()
        {
            // Instantiate ShimsContext to use Fakes 
            using (ShimsContext.Create())
            {
                // Return "dummyToken" when calling GetAccessToken method 
                AuthBot.Fakes.ShimContextExtensions.GetAccessTokenIBotContextString =
 async (a, e) => { return "dummyToken"; };

                // Mock the service and register
                var mockEventService = new Mock<IEventService>();
                mockEventService.Setup(x => x.CreateEvent(It.IsAny<Event>())).Returns(Task.FromResult(true));
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
                var mockNotificationService = new Mock<INotificationService>();
                mockNotificationService.Setup(x => x.SubscribeEventChange()).ReturnsAsync(Guid.NewGuid().ToString());
                mockNotificationService.Setup(x => x.RenewSubscribeEventChange(It.IsAny<string>())).Returns(Task.FromResult(true));
                
                var builder = new ContainerBuilder();
                builder.RegisterInstance(mockEventService.Object).As<IEventService>();
                builder.RegisterInstance(mockNotificationService.Object).As<INotificationService>();
                WebApiApplication.Container = builder.Build();

                // Instantiate dialog to test
                IDialog<object> rootDialog = new RootDialog();

                // Create in-memory bot environment
                Func<IDialog<object>> MakeRoot = () => rootDialog;
                using (new FiberTestBase.ResolveMoqAssembly(rootDialog))
                using (var container = Build(Options.MockConnectorFactory | Options.ScopedQueue, rootDialog))
                {
                    // Register global message handler
                    RegisterBotModules(container);

                    // Create a message to send to bot
                    var toBot = DialogTestBase.MakeTestMessage();
                    // Specify local as US English
                    toBot.Locale = "en-US";
                    toBot.From.Id = Guid.NewGuid().ToString();
                    toBot.Text = "add appointment";

                    // Send message and check the answer.
                    var toUser = await GetResponses(container, MakeRoot, toBot);

                    // Verify the result
                    Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
                    Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));
                    
                    toBot.Text = "Learn BotFramework";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("What is the detail?"));

                    toBot.Text = "Get Events";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("2017-05-31 12:00-2017-05-31 13:00: dummy event"));

                    toBot.Text = "Glbal Message Handler for O365Bot";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("When do you start? Use dd/MM/yyyy HH:mm format."));
                }
            }
        }

        [TestMethod]
        public async Task ShouldReceiveNotifyEventChageDialog()
        {
            // Instantiate ShimsContext to use Fakes 
            using (ShimsContext.Create())
            {
                // Return "dummyToken" when calling GetAccessToken method 
                AuthBot.Fakes.ShimContextExtensions.GetAccessTokenIBotContextString =
                    async (a, e) => { return "dummyToken"; };

                // Mock the service and register
                var mockEventService = new Mock<IEventService>();
                mockEventService.Setup(x => x.CreateEvent(It.IsAny<Event>())).Returns(Task.FromResult(true));
                mockEventService.Setup(x => x.GetEvent(It.IsAny<string>())).ReturnsAsync(new Event()
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
                    },
                    Body = new ItemBody()
                    {
                        Content = "Dummy Body"
                    },
                    Location = new Location()
                    {
                        DisplayName = "Dummy Location"
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

                // Instantiate dialog to test
                IDialog<object> rootDialog = new RootDialog();

                // Create in-memory bot environment
                Func<IDialog<object>> MakeRoot = () => rootDialog;
                using (new FiberTestBase.ResolveMoqAssembly(rootDialog))
                using (var container = Build(Options.MockConnectorFactory | Options.ScopedQueue, rootDialog))
                {
                    // Register global message handler
                    RegisterBotModules(container);

                    // Create a message to send to bot
                    var toBot = DialogTestBase.MakeTestMessage();
                    // Specify local as US English
                    toBot.Locale = "en-US";
                    toBot.From.Id = Guid.NewGuid().ToString();
                    toBot.Text = "add appointment";

                    // Send message and check the answer.
                    var toUser = await GetResponses(container, MakeRoot, toBot);

                    // Verify the result
                    Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
                    Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));

                    toBot.Text = "Learn BotFramework";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("What is the detail?"));

                    // Send Proactive Message
                    // Use current subscriptionid to get ConversationReference。
                    var conversationReference = CacheService.caches[subscriptionId] as ConversationReference;
                    // Get an Event Id
                    var id = Guid.NewGuid().ToString();

                    // Get local and set it
                    var activity = conversationReference.GetPostToBotMessage();
                    var locale = CacheService.caches[activity.From.Id].ToString();
                    Thread.CurrentThread.CurrentCulture = new CultureInfo(locale);
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(locale);
                    toUser = await Resume(container, new NotifyEventChageDialog(id), activity);

                    Assert.IsTrue((toUser[0].Attachments[0].Content as HeroCard).Text.Equals("One of your events has been updated."));

                    toBot.Text = "Check the detail";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("Check the detail"));
                    Assert.IsTrue(toUser[1].Text.Equals("2017-05-31 12:00-2017-05-31 13:00: dummy event@Dummy Location-Dummy Body"));

                    toBot.Text = "Implement O365Bot";
                    toUser = await GetResponses(container, MakeRoot, toBot);
                    Assert.IsTrue(toUser[0].Text.Equals("When do you start? Use dd/MM/yyyy HH:mm format."));
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