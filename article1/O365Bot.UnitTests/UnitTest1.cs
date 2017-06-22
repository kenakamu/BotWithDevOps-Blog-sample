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

namespace O365Bot.UnitTests
{
    [TestClass]
    public class SampleDialogTest : DialogTestBase
    {
        [TestMethod]
        public async Task ShouldReturnCount()
        {
            // Instantiate dialog to test
            IDialog<object> rootDialog = new RootDialog();
            
            // Create in-memory bot environment
            Func<IDialog<object>> MakeRoot = () => rootDialog;
            using (new FiberTestBase.ResolveMoqAssembly(rootDialog))
            using (var container = Build(Options.MockConnectorFactory | Options.ScopedQueue, rootDialog))
            {
                // Create a message to send to bot
                var toBot = DialogTestBase.MakeTestMessage();
                toBot.From.Id = Guid.NewGuid().ToString();
                toBot.Text = "hi!";
            
                // Send message and check the answer.
                IMessageActivity toUser = await GetResponse(container, MakeRoot, toBot);

                // Verify the result
                Assert.IsTrue(toUser.Text.Equals("You sent hi! which was 3 characters"));
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
    }
}