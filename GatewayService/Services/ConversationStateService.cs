using GatewayService.Models.Conversation;

namespace GatewayService.Services
{
    public class ConversationStateService : IConversationStateService
    {
        private readonly Dictionary<long, UserConversationData> _conversations = new();

        public UserConversationData GetOrCreate(long chatId)
        {
            if (!_conversations.ContainsKey(chatId))
                _conversations[chatId] = new UserConversationData();
            return _conversations[chatId];
        }

        public void Clear(long chatId)
        {
            if (_conversations.ContainsKey(chatId))
                _conversations[chatId].Clear();
        }
    }
}
