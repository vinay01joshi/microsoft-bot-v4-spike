// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace SpikeBot
{
    /// <summary>
    /// Represents a bot that processes incoming activities.
    /// For each user interaction, an instance of this class is created and the OnTurnAsync method is called.
    /// This is a Transient lifetime service.  Transient lifetime services are created
    /// each time they're requested. For each Activity received, a new instance of this
    /// class is created. Objects that are expensive to construct, or have a lifetime
    /// beyond the single turn, should be carefully managed.
    /// For example, the <see cref="MemoryStorage"/> object and associated
    /// <see cref="IStatePropertyAccessor{T}"/> object are created with a singleton lifetime.
    /// </summary>
    /// <seealso cref="https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-2.1"/>
    public class EchoWithCounterBot : IBot
    {
        private readonly EchoBotAccessors _accessors;
        private readonly ILogger _logger;
        private IHostingEnvironment _env;
        private const string _genericMessage = @"This is a simple Welcome Bot sample. You can say 'intro' to 
                                         see the introduction card. If you are running this bot in the Bot 
                                         Framework Emulator, press the 'Start Over' button to simulate user joining a bot or a channel";

        public EchoWithCounterBot(EchoBotAccessors accessors, ILoggerFactory loggerFactory, IHostingEnvironment env)
        {
            if (loggerFactory == null)
            {
                throw new System.ArgumentNullException(nameof(loggerFactory));
            }

            _logger = loggerFactory.CreateLogger<EchoWithCounterBot>();
            _logger.LogTrace("EchoBot turn start.");
            _env = env;
            _accessors = accessors ?? throw new System.ArgumentNullException(nameof(accessors));
        }

        public async Task OnTurnAsync(ITurnContext turnContext, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessors.CounterState.GetAsync(turnContext, () => new CounterState());

            if (turnContext.Activity.Type == ActivityTypes.Message)
            {
                if (state.DidBotWelcomedUser == false)
                {
                    state.DidBotWelcomedUser = true;
                    state.TurnCount++;
                    await _accessors.CounterState.SetAsync(turnContext, state);
                    await _accessors.ConversationState.SaveChangesAsync(turnContext);

                    var userName = turnContext.Activity.From.Name;

                    await turnContext.SendActivityAsync($"You are seeing this message because this was your first message ever to this bot.", cancellationToken: cancellationToken);
                    await turnContext.SendActivityAsync($"It is a good practice to welcome the user and provide a personal greeting. For example, welcome {userName}.", cancellationToken: cancellationToken);
                }
                else
                {
                    var text = turnContext.Activity.Text.ToLowerInvariant();
                    switch (text)
                    {
                        case "hello":
                        case "hi":
                            await turnContext.SendActivityAsync($"You said {text}.", cancellationToken: cancellationToken);
                            break;
                        case "intro":
                        case "help":
                        case "media":
                            await turnContext.SendActivityAsync(GetMediaReply(turnContext), cancellationToken);
                            break;
                        case "hero":
                            await turnContext.SendActivityAsync(GetHeroCard(turnContext), cancellationToken);
                            break;
                        case "heropostback":
                            await turnContext.SendActivityAsync(GetHeroPostBack(turnContext), cancellationToken);
                            break;
                        case "adptivecard":
                            await turnContext.SendActivityAsync(GetAdaptiveCard(turnContext), cancellationToken);
                            break;
                        case "crouselcard":
                            await turnContext.SendActivityAsync(GetCrousleCard(turnContext), cancellationToken);
                            break;
                        default:
                            await turnContext.SendActivityAsync(_genericMessage, cancellationToken: cancellationToken);
                            break;
                    }
                }
            }
            else if (turnContext.Activity.Type == ActivityTypes.Event)
            {
                await turnContext.SendActivityAsync("An event activity type");
            }
            else if (turnContext.Activity.Type == ActivityTypes.ConversationUpdate)
            {
                if (turnContext.Activity.MembersAdded.Any())
                {
                    foreach (var member in turnContext.Activity.MembersAdded)
                    {
                        if (member.Id != turnContext.Activity.Recipient.Id)
                        {
                            await turnContext.SendActivityAsync($"Hi there - {member.Name}. Welcome to the 'Welcome User' Bot. This bot will introduce you to welcoming and greeting users.", cancellationToken: cancellationToken);
                            await turnContext.SendActivityAsync($"You are seeing this message because the bot recieved atleast one 'ConversationUpdate' event,indicating you (and possibly others) joined the conversation. If you are using the emulator, pressing the 'Start Over' button to trigger this event again. The specifics of the 'ConversationUpdate' event depends on the channel. You can read more information at https://aka.ms/about-botframewor-welcome-user", cancellationToken: cancellationToken);
                            await turnContext.SendActivityAsync($"It is a good pattern to use this event to send general greeting to user, explaning what your bot can do. In this example, the bot handles 'hello', 'hi', 'help' and 'intro. Try it now, type 'hi'", cancellationToken: cancellationToken);
                        }
                    }
                }
            }
            else
            {
                await turnContext.SendActivityAsync($"{turnContext.Activity.Type} event detected");
            }
        }

        private Activity GetMediaReply(ITurnContext currentContext)
        {
            var reply = currentContext.Activity.CreateReply();
            var attachment = new Attachment
            {
                ContentUrl = "http://localhost:3978/images/vj.jpg",
                ContentType = "image/jpg",
                Name = "VjImage",
            };
            reply.Attachments = new List<Attachment>() { attachment };

            return reply;
        }

        private Activity GetHeroCard(ITurnContext currentContext)
        {
            var reply = currentContext.Activity.CreateReply();

            var card = new HeroCard
            {
                Text = "You can upload an image or select one of the following choices",
                Buttons = new List<CardAction>()
                {
                    new CardAction(ActionTypes.ImBack, title: "1. Inline Attachment", value: "1"),
                    new CardAction(ActionTypes.ImBack, title: "2. Internet Attachment", value: "2"),
                    new CardAction(ActionTypes.ImBack, title: "3. Uploaded Attachment", value: "3"),
                },
            };

            // Add the card to our reply.
            reply.Attachments = new List<Attachment>() { card.ToAttachment() };

            return reply;
        }

        private Activity GetHeroPostBack(ITurnContext currentContext)
        {
            var reply = currentContext.Activity.CreateReply();
            var card = new HeroCard
            {
                Buttons = new List<CardAction>()
                {
                    new CardAction(title: "Much Quieter", type: ActionTypes.PostBack, value: "Shh! My Bot friend hears me."),
                    new CardAction(ActionTypes.OpenUrl, title: "Azure Bot Service", value: "https://azure.microsoft.com/en-us/services/bot-service/"),
                },
            };
            reply.Attachments = new List<Attachment>() { card.ToAttachment() };
            return reply;
        }

        private Activity GetAdaptiveCard(ITurnContext currentContext)
        {
            var reply = currentContext.Activity.CreateReply();
            var cardAttachment = CreateAdaptiveCardAttachment(_env.WebRootPath + "/Resouces/activityupdateadaptive.json");
            reply.Attachments = new List<Attachment>() { cardAttachment };
            return reply;
        }

        private IActivity GetCrousleCard(ITurnContext currentContext)
        {          
            var activity = MessageFactory.Carousel(
              new Attachment[]
              {
                new HeroCard(
                    title: "Docker",
                    images: new CardImage[] { new CardImage(url: "http://localhost:3978/images/docker.png") },
                    buttons: new CardAction[]
                    {
                        new CardAction(title: "Go to Item", type: ActionTypes.ImBack, value: "Docker"),
                    })
                .ToAttachment(),
                new HeroCard(
                    title: "Visual Studio",
                    images: new CardImage[] { new CardImage(url: "http://localhost:3978/images/visualstudio.png") },
                    buttons: new CardAction[]
                    {
                        new CardAction(title: "Go to Item", type: ActionTypes.ImBack, value: "VisualStudio")
                    })
                .ToAttachment(),
                new HeroCard(
                    title: "Xamrine",
                    images: new CardImage[] { new CardImage(url: "http://localhost:3978/images/xamarin.jpg") },
                    buttons: new CardAction[]
                    {
                        new CardAction(title: "Go to Item", type: ActionTypes.ImBack, value: "Xamrine")
                    })
                .ToAttachment()
              });           
            return activity;
        }


        private Attachment CreateAdaptiveCardAttachment(string filePath)
        {
            var adaptiveCardJson = File.ReadAllText(filePath);
            var adaptiveCardAttachment = new Attachment()
            {
                ContentType = "application/vnd.microsoft.card.adaptive",
                Content = JsonConvert.DeserializeObject(adaptiveCardJson),
            };
            return adaptiveCardAttachment;
        }
    }
}
