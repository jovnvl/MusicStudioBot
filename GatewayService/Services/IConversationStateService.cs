using GatewayService.Models.Conversation;

namespace GatewayService.Services
{
    public interface IConversationStateService
    {
        UserConversationData GetOrCreate(long chatId);
        void Clear(long chatId);
    }
}
