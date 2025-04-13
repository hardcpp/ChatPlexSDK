namespace CP_SDK.Chat.Interfaces
{
    public interface IChatSubscriptionEvent
    {
        string  DisplayName             { get; }
        string  SubPlan                 { get; }
        bool    IsGift                  { get; }
    }
}
