namespace GatewayService.Models.Conversation
{
    public class UserConversationData
    {
        public ConversationState State { get; set; } = ConversationState.None;
        public Dictionary<string, object> Data { get; set; } = new();

        public void SetValue(string key, object value) => Data[key] = value;

        public T? GetValue<T>(string key) => Data.TryGetValue(key, out var value) ? (T)value : default;

        public void Clear()
        {
            State = ConversationState.None;
            Data.Clear();
        }
    }
}
