using CP_SDK.Chat.Interfaces;
using CP_SDK.Chat.Models.Twitch;
using System;
using System.Linq;

namespace CP_SDK.Chat.Services.Twitch
{
    /// <summary>
    /// Twitch slash command emulator
    /// </summary>
    public static class TwitchIRCLegacyCommandEmulator
    {
        private static char[] m_SplitOptions = new char[] { ' ' };

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        public static bool Intercept(TwitchService p_Service, IChatChannel p_Channel, string p_Message)
        {
            if (p_Message.Length < 1 || p_Message[0] != '/')
                return false;

            var l_Parts = p_Message.Split(m_SplitOptions, StringSplitOptions.RemoveEmptyEntries);

            switch (l_Parts[0])
            {
                case "/ban":        Command_Ban(p_Service, p_Channel, l_Parts);         break;
                case "/unban":      Command_Unban(p_Service, p_Channel, l_Parts);       break;
                case "/timeout":    Command_Timeout(p_Service, p_Channel, l_Parts);     break;
                case "/untimeout":  Command_Untimeout(p_Service, p_Channel, l_Parts);   break;
            }

            return true;
        }

        ////////////////////////////////////////////////////////////////////////////
        /// Ban
        ////////////////////////////////////////////////////////////////////////////

        private static void Command_Ban(TwitchService p_Service, IChatChannel p_Channel, string[] p_Parts)
        {
            if (p_Parts.Length < 2)
            {
                p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Syntax is /ban [UserName] [Reason]");
                return;
            }

            var l_UserName  = p_Parts[1];
            var l_Reason    = p_Parts.Length > 2 ? string.Join(" ", p_Parts.Skip(2)) : null;

            p_Service.HelixAPI.GetUserByLogin(l_UserName, (p_Status, p_Result, p_Error) =>
            {
                if (p_Status != EHelixResult.OK)
                {
                    p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Failed to ban user {l_UserName}, user not found!");
                    return;
                }

                p_Service.HelixAPI.BanUser(new Helix_BanUser_Query.BanQuery(p_Result.id, null, l_Reason), (p_Status2, p_Result2, p_Error2) =>
                {
                    if (p_Status2 == EHelixResult.OK)
                        p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Banned user {l_UserName}");
                    else
                    {
                        p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Failed to ban user {l_UserName}");
                        Service.Multiplexer.InternalBroadcastSystemMessage($"Error: {p_Error2}");
                    }
                });
            });
        }
        private static void Command_Unban(TwitchService p_Service, IChatChannel p_Channel, string[] p_Parts)
        {
            if (p_Parts.Length < 2)
            {
                p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Syntax is /unban [UserName]");
                return;
            }

            var l_UserName = p_Parts[1];

            p_Service.HelixAPI.GetUserByLogin(l_UserName, (p_Status, p_Result, p_Error) =>
            {
                if (p_Status != EHelixResult.OK)
                {
                    p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Failed to unban user {l_UserName}, user not found!");
                    return;
                }

                p_Service.HelixAPI.UnbanUser(new Helix_UnbanUser_Query(p_Result.id), (p_Status2, p_Error2) =>
                {
                    if (p_Status2 == EHelixResult.OK)
                        p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Unbanned user {l_UserName}");
                    else
                    {
                        p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Failed to unban user {l_UserName}");
                        Service.Multiplexer.InternalBroadcastSystemMessage($"Error: {p_Error2}");
                    }
                });
            });
        }

        ////////////////////////////////////////////////////////////////////////////
        /// Timeout
        ////////////////////////////////////////////////////////////////////////////

        private static void Command_Timeout(TwitchService p_Service, IChatChannel p_Channel, string[] p_Parts)
        {
            int l_Duration = 0;
            if (p_Parts.Length < 2)
            {
                p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Syntax is /timeout [UserName] [Duration] [Reason]");
                return;
            }

            if (p_Parts.Length >= 3)
                int.TryParse(p_Parts[2], out l_Duration);

            if (l_Duration <= 0)
                l_Duration = 10 * 60;

            var l_UserName  = p_Parts[1];
            var l_Reason    = p_Parts.Length > 2 ? string.Join(" ", p_Parts.Skip(2)) : null;

            p_Service.HelixAPI.GetUserByLogin(l_UserName, (p_Status, p_Result, p_Error) =>
            {
                if (p_Status != EHelixResult.OK)
                {
                    p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Failed to timeout user {l_UserName}, user not found!");
                    return;
                }

                p_Service.HelixAPI.BanUser(new Helix_BanUser_Query.BanQuery(p_Result.id, l_Duration, l_Reason), (p_Status2, p_Result2, p_Error2) =>
                {
                    if (p_Status2 == EHelixResult.OK)
                    {
                        if (l_Duration > 60)
                            p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Timeout user {l_UserName} for {(int)(l_Duration / 60)} minute(s)");
                        else if (l_Duration != 0)
                            p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Timeout user {l_UserName} for {l_Duration} second(s)");
                        else
                            p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Timeout user {l_UserName}");
                    }
                    else
                    {
                        p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Failed to timeout user {l_UserName}");
                        Service.Multiplexer.InternalBroadcastSystemMessage($"Error: {p_Error2}");
                    }
                });
            });
        }
        private static void Command_Untimeout(TwitchService p_Service, IChatChannel p_Channel, string[] p_Parts)
        {
            if (p_Parts.Length < 2)
            {
                p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Syntax is /untimeout [UserName]");
                return;
            }

            var l_UserName = p_Parts[1];

            p_Service.HelixAPI.GetUserByLogin(l_UserName, (p_Status, p_Result, p_Error) =>
            {
                if (p_Status != EHelixResult.OK)
                {
                    p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Failed to untimeout user {l_UserName}, user not found!");
                    return;
                }

                p_Service.HelixAPI.UnbanUser(new Helix_UnbanUser_Query(p_Result.id), (p_Status2, p_Error2) =>
                {
                    if (p_Status2 == EHelixResult.OK)
                        p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} untimeout user {l_UserName}");
                    else
                    {
                        p_Service.SendTextMessage(p_Channel, $"@{p_Service.HelixAPI.TokenUserName} Failed to untimeout user {l_UserName}");
                        Service.Multiplexer.InternalBroadcastSystemMessage($"Error: {p_Error2}");
                    }
                });
            });
        }
    }
}
