using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Luis;
using Microsoft.Bot.Builder.Luis.Models;
using Microsoft.Bot.Builder.Tests;
using Moq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Tests
{
    public abstract class LuisTestBase : DialogTestBase
    {
        public static IntentRecommendation[] IntentsFor<D>(Expression<Func<D, Task>> expression, double? score)
        {
            var body = (MethodCallExpression)expression.Body;
            var attributes = body.Method.GetCustomAttributes<LuisIntentAttribute>();
            var intents = attributes
                .Select(attribute => new IntentRecommendation(attribute.IntentName, score))
                .ToArray();
            return intents;
        }

        public static EntityRecommendation EntityFor(string type, string entity, IDictionary<string, object> resolution = null)
        {
            return new EntityRecommendation(type: type) { Entity = entity, Resolution = resolution };
        }

        public static EntityRecommendation EntityForDate(string type, DateTime date)
        {
            return EntityFor(type,
                date.ToString("d", DateTimeFormatInfo.InvariantInfo),
                new Dictionary<string, object>()
                {
                    { "resolution_type", "builtin.datetime.date" },
                    { "date", date.ToString("yyyy-MM-dd", DateTimeFormatInfo.InvariantInfo) }
                });
        }

        public static EntityRecommendation EntityForTime(string type, DateTime time)
        {
            return EntityFor(type,
                time.ToString("t", DateTimeFormatInfo.InvariantInfo),
                new Dictionary<string, object>()
                {
                    { "resolution_type", "builtin.datetime.time" },
                    { "time", time.ToString("THH:mm:ss", DateTimeFormatInfo.InvariantInfo) }
                });
        }

        public static void SetupLuis<D>(
            Mock<ILuisService> luis,
            Expression<Func<D, Task>> expression,
            double? score,
            params EntityRecommendation[] entities
            )
        {
            luis
                .Setup(l => l.QueryAsync(It.IsAny<Uri>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LuisResult()
                {
                    Intents = IntentsFor(expression, score),
                    Entities = entities
                });
        }

        public static void SetupLuis<D>(
            Mock<ILuisService> luis,
            string utterance,
            Expression<Func<D, Task>> expression,
            double? score,
            params EntityRecommendation[] entities
            )
        {
            var uri = new UriBuilder() { Query = utterance }.Uri;
            luis
                .Setup(l => l.BuildUri(It.Is<LuisRequest>(r => r.Query == utterance)))
                .Returns(uri);

            luis.Setup(l => l.ModifyRequest(It.IsAny<LuisRequest>()))
                .Returns<LuisRequest>(r => r);

            luis
                .Setup(l => l.QueryAsync(uri, It.IsAny<CancellationToken>()))
                .Returns<Uri, CancellationToken>(async (_, token) =>
                {
                    return new LuisResult()
                    {
                        Intents = IntentsFor(expression, score),
                        Entities = entities
                    };
                });
        }
    }
}