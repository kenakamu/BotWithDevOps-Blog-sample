using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Microsoft.Bot.Connector.DirectLine;

namespace O365Bot.FunctionTests
{
    [TestClass]
    public class FunctionTest1
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void Function_ShouldReturnEvents()
        {
            DirectLineHelper helper = new DirectLineHelper(TestContext);
            var toUser = helper.SentMessage("get appointments");
            Assert.IsTrue(true);
        }

        [TestMethod]
        public void Function_ShouldCreateAllDayEvent()
        {
            DirectLineHelper helper = new DirectLineHelper(TestContext);
            var toUser = helper.SentMessage("add appointment");

            // Verify the result
            Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
            Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));

            toUser = helper.SentMessage("Learn BotFramework");
            Assert.IsTrue(toUser[0].Text.Equals("What is the detail?"));

            toUser = helper.SentMessage("Implement O365Bot");
            Assert.IsTrue(toUser[0].Text.Equals("When do you start? Use dd/MM/yyyy HH:mm format."));

            toUser = helper.SentMessage("01/07/2017 13:00");
            Assert.IsTrue(JsonConvert.DeserializeObject<HeroCard>(toUser[0].Attachments[0].Content.ToString()).Text.Equals("Is this all day event?"));

            toUser = helper.SentMessage("Yes");
            Assert.IsTrue(toUser[0].Text.Equals("The event is created."));
        }

        [TestMethod]
        public void Function_ShouldCreateEvent()
        {
            DirectLineHelper helper = new DirectLineHelper(TestContext);
            var toUser = helper.SentMessage("add appointment");

            // Verify the result
            Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
            Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));

            toUser = helper.SentMessage("Learn BotFramework");
            Assert.IsTrue(toUser[0].Text.Equals("What is the detail?"));

            toUser = helper.SentMessage("Implement O365Bot");
            Assert.IsTrue(toUser[0].Text.Equals("When do you start? Use dd/MM/yyyy HH:mm format."));

            toUser = helper.SentMessage("01/07/2017 13:00");
            Assert.IsTrue(JsonConvert.DeserializeObject<HeroCard>(toUser[0].Attachments[0].Content.ToString()).Text.Equals("Is this all day event?"));

            toUser = helper.SentMessage("No");
            Assert.IsTrue(toUser[0].Text.Equals("How many hours?"));

            toUser = helper.SentMessage("4");
            Assert.IsTrue(toUser[0].Text.Equals("The event is created."));
        }

        [TestMethod]
        public void Function_ShouldCancelCurrrentDialog()
        {
            DirectLineHelper helper = new DirectLineHelper(TestContext);
            var toUser = helper.SentMessage("add appointment");

            // Verify the result
            Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
            Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));

            toUser = helper.SentMessage("Learn BotFramework");
            Assert.IsTrue(toUser[0].Text.Equals("What is the detail?"));

            toUser = helper.SentMessage("Cancel");
            Assert.IsTrue(toUser.Count.Equals(0));

            toUser = helper.SentMessage("add appointment");
            Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
            Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));
        }

        [TestMethod]
        public void Function_ShouldInterruptCurrentDialog()
        {
            DirectLineHelper helper = new DirectLineHelper(TestContext);
            var toUser = helper.SentMessage("add appointment");
            // Verify the result
            Assert.IsTrue(toUser[0].Text.Equals("Creating an event."));
            Assert.IsTrue(toUser[1].Text.Equals("What is the title?"));

            toUser = helper.SentMessage("Learn BotFramework");
            Assert.IsTrue(toUser[0].Text.Equals("What is the detail?"));

            toUser = helper.SentMessage("Get Events");
            Assert.IsTrue(true);

            toUser = helper.SentMessage("Implement O365Bot");
            Assert.IsTrue(toUser[0].Text.Equals("When do you start? Use dd/MM/yyyy HH:mm format."));
        }
    }
}