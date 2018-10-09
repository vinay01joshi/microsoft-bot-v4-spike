using System;
using Microsoft.Bot.Builder;

namespace SpikeBot.Bots.States
{
    public class PromptBotAccessor
    {
        public const string TopicStateName = "PrimitivePrompts.TopicStateAccessor";

        public const string UserProfileName = "PrimitivePrompts.UserProfileAccessor";

        public ConversationState ConversationState { get; }

        public UserState UserState { get; }

        public IStatePropertyAccessor<TopicState> TopicStateAccessor { get; set; }

        public IStatePropertyAccessor<UserProfile> UserProfileAccessor { get; set; }

        public PromptBotAccessor(ConversationState conversationState, UserState userState)
        {
            if (conversationState is null)
            {
                throw new ArgumentNullException(nameof(conversationState));
            }

            if (userState is null)
            {
                throw new ArgumentNullException(nameof(userState));
            }

            this.ConversationState = conversationState;
            this.UserState = userState;
        }
    }
}
