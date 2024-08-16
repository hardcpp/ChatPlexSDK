using CP_SDK.Chat.Interfaces;

namespace CP_SDK.Chat.Models.Twitch
{
    public class TwitchTempChannel
    {
        public string   GroupIdentifier { get; internal set; }
        public string   Prefix          { get; internal set; }
        public bool     CanSend         { get; internal set; }

        public TwitchTempChannel(string p_GroupIdentifier, string p_Prefix, bool p_CanSendMessage)
        {
            GroupIdentifier = p_GroupIdentifier;
            Prefix = p_Prefix;
            CanSend = p_CanSendMessage;
        }
    }
}
