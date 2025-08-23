using CP_SDK.Network;
using System;

namespace CP_SDK.Chat.Services
{
    public static class RelayChatServiceProtocol
    {
        public const string PROTOCOL_VERSION = "v1.5";

        public static void Send_AuthConnectedApplicationToken(WebSocketClient p_Socket, string connectedApplicationToken)
            => p_Socket.SendMessage($"AuthConnectedApplicationToken|{PROTOCOL_VERSION}|{connectedApplicationToken}");

        public static void Send_SendMessage(WebSocketClient p_Socket, string p_ChannelID, string p_Message)
            => p_Socket.SendMessage("SendMessage|" + p_ChannelID + "|" + p_Message);
    }
}
