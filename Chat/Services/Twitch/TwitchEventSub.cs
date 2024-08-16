using CP_SDK.Chat.SimpleJSON;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;

namespace CP_SDK.Chat.Services.Twitch
{
    /// <summary>
    /// Twitch EventSub socket implementation
    /// </summary>
    public class TwitchEventSub
    {
        /// <summary>
        /// Subscription model
        /// </summary>
        public class Subscription
        {
            public string                       ID;
            public string                       RoomID;
            public string                       Type;
            public string                       Version;
            public Dictionary<string, string>   Conditions;

            ////////////////////////////////////////////////////////////////////////////
            ////////////////////////////////////////////////////////////////////////////

            /// <summary>
            /// Build the subsciption payload
            /// </summary>
            /// <param name="p_SessionID">ID of the session</param>
            /// <returns></returns>
            public JSONObject BuildSubscriptionPayload(string p_SessionID)
            {
                JSONObject l_Condition = new JSONObject();
                foreach (var l_KVP in Conditions)
                    l_Condition.Add(l_KVP.Key, l_KVP.Value);

                JSONObject l_Transport = new JSONObject();
                l_Transport.Add("method",       "websocket");
                l_Transport.Add("session_id",   p_SessionID);

                JSONObject l_Result = new JSONObject();
                l_Result.Add("type",        Type);
                l_Result.Add("version",     Version);
                l_Result.Add("condition",   l_Condition);
                l_Result.Add("transport",   l_Transport);

                return l_Result;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private TwitchService                           m_TwitchService;
        private Network.WebSocketClient                 m_EventSubSocket;
        private object                                  m_MessageReceivedLock   = new object();
        private string                                  m_SessionID             = string.Empty;
        private Dictionary<string, List<Subscription>>  m_ChannelSubscriptions  = new Dictionary<string, List<Subscription>>();

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_TwitchService">Twitch service instance</param>
        public TwitchEventSub(TwitchService p_TwitchService)
        {
            m_TwitchService = p_TwitchService;

            /// EventSub socket
            m_EventSubSocket = new Network.WebSocketClient();
            m_EventSubSocket.OnOpen             += EventSubSocket_OnOpen;
            m_EventSubSocket.OnClose            += EventSubSocket_OnClose;
            m_EventSubSocket.OnError            += EventSubSocket_OnError;
            m_EventSubSocket.OnMessageReceived  += EventSubSocket_OnMessageReceived;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Connect to the event sub
        /// </summary>
        public void Connect()
        {
            m_EventSubSocket.Connect("wss://eventsub.wss.twitch.tv/ws");
        }
        /// <summary>
        /// Disconnect from the event sub
        /// </summary>
        public void Disconnect()
        {
            m_EventSubSocket.Disconnect();
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Subscribe for channel follow
        /// </summary>
        /// <param name="p_RoomID">ID of the channel</param>
        public void Subscribe_ChannelFollow(string p_RoomID)
        {
            AddSubscription(new Subscription()
            {
                RoomID      = p_RoomID,
                Type        = "channel.follow",
                Version     = "2",
                Conditions  = new Dictionary<string, string>()
                {
                    { "broadcaster_user_id",    p_RoomID                                },
                    { "moderator_user_id",      m_TwitchService.HelixAPI.TokenUserID    },
                }
            });
        }
        /// <summary>
        /// Unsubscribe all event for a channel ID
        /// </summary>
        /// <param name="p_RoomID">ID of the channel</param>
        public void UnsubscrbibeAll(string p_RoomID)
        {
            if (string.IsNullOrEmpty(m_SessionID))
                return;

            lock (m_ChannelSubscriptions)
            {
                if (!m_ChannelSubscriptions.TryGetValue(p_RoomID, out var l_ChannelSubscriptions))
                    return;

                foreach (var l_Subscription in l_ChannelSubscriptions)
                    DoUnsubscribe(l_Subscription);

                m_ChannelSubscriptions.Remove(p_RoomID);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Add a subscription and do it immediatly if ready, otherwise queue it
        /// </summary>
        /// <param name="p_Subscription">Subscription to add</param>
        private void AddSubscription(Subscription p_Subscription)
        {
            lock (m_ChannelSubscriptions)
            {
                if (!m_ChannelSubscriptions.ContainsKey(p_Subscription.RoomID))
                    m_ChannelSubscriptions.Add(p_Subscription.RoomID, new List<Subscription>());

                if (m_ChannelSubscriptions[p_Subscription.RoomID].Any(x => x.Type == p_Subscription.Type))
                    return;

                m_ChannelSubscriptions[p_Subscription.RoomID].Add(p_Subscription);

                if (!string.IsNullOrEmpty(m_SessionID))
                    DoSubscribe(p_Subscription);
            }
        }
        /// <summary>
        /// Do all subscribes
        /// </summary>
        private void DoAllSubscibes()
        {
            if (string.IsNullOrEmpty(m_SessionID))
                return;

            lock (m_ChannelSubscriptions)
            {
                foreach (var l_KVP in m_ChannelSubscriptions)
                {
                    foreach (var l_Subscription in l_KVP.Value)
                        DoSubscribe(l_Subscription);
                }
            }
        }
        /// <summary>
        /// Do a single subscription
        /// </summary>
        /// <param name="p_Subscription">Subscription to do</param>
        private void DoSubscribe(Subscription p_Subscription)
        {
            m_TwitchService.HelixAPI.WebClient.PostAsync(
                "eventsub/subscriptions",
                Network.WebContent.FromJson(p_Subscription.BuildSubscriptionPayload(m_SessionID).ToString()),
                CancellationToken.None,
                (p_Result) =>
                {
                    if (!p_Result.IsSuccessStatusCode)
                        return;

                    JSONNode l_JSON = JSON.Parse(p_Result?.BodyString);
                    JSONNode l_Value;

                    if (l_JSON.TryGetKey("data", out var l_Data) && l_Data.IsArray && l_Data.AsArray.Count == 1)
                    {
                        var l_FirstData = l_Data.AsArray[0];
                        if (l_FirstData.TryGetKey("id", out l_Value)) { p_Subscription.ID = l_Value.Value; }
                    }
                },
                true
            );
        }
        /// <summary>
        /// Do an unsubscribe
        /// </summary>
        /// <param name="p_Subscription">Subscription to remove</param>
        private void DoUnsubscribe(Subscription p_Subscription)
        {
            if (string.IsNullOrEmpty(p_Subscription.ID))
                return;

            m_TwitchService.HelixAPI.WebClient.DeleteAsync(
                $"eventsub/subscriptions?id={p_Subscription.ID}",
                CancellationToken.None,
                null,
                true
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Twitch EventSub socket open
        /// </summary>
        private void EventSubSocket_OnOpen()
        {
            ChatPlexSDK.Logger.Info("TwitchEventSub connection opened");
            //ResubscribeChannelsTopics();
        }
        /// <summary>
        /// Twitch EventSub socket close
        /// </summary>
        private void EventSubSocket_OnClose(WebSocketCloseStatus? p_CloseStatus, string p_CloseStatusDescription)
        {
            ChatPlexSDK.Logger.Info($"TwitchEventSub connection closed {p_CloseStatus}:{p_CloseStatusDescription}");
            m_SessionID = null;
        }
        /// <summary>
        /// Twitch EventSub socket error
        /// </summary>
        private void EventSubSocket_OnError()
        {
            ChatPlexSDK.Logger.Error($"An error occurred in TwitchEventSub connection");
            m_SessionID = null;
        }
        /// <summary>
        /// When a twitch EventSub message is received
        /// </summary>
        /// <param name="p_RawMessage">Raw message</param>
        private void EventSubSocket_OnMessageReceived(string p_RawMessage)
        {
            lock (m_MessageReceivedLock)
            {
                JSONNode l_JSON = JSON.Parse(p_RawMessage);
                JSONNode l_Value;
                string l_MessageType        = string.Empty;
                string l_SubscriptionType   = string.Empty;

                if (l_JSON.TryGetKey("metadata", out var l_Metadata))
                {
                    if (l_Metadata.TryGetKey("message_type",        out l_Value)) { l_MessageType      = l_Value.Value; }
                    if (l_Metadata.TryGetKey("subscription_type",   out l_Value)) { l_SubscriptionType = l_Value.Value; }
                }

                if (l_JSON.TryGetKey("payload", out var l_Payload))
                {
                    switch (l_MessageType)
                    {
                        case "session_welcome":
                            if (l_Payload.TryGetKey("session", out var l_Session))
                                Handle_SessionWelcome(l_Session);

                            break;

                        case "notification":
                            switch (l_SubscriptionType)
                            {
                                case "channel.follow":
                                    if (l_Payload.TryGetKey("event", out var l_Event))
                                        Handle_Notification_ChannelFollow(l_Event);
                                    break;

                            }
                            break;
                    }
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// message_type='session_welcome'
        /// </summary>
        private void Handle_SessionWelcome(JSONNode p_Session)
        {
            JSONNode l_Value;
            string l_ID = string.Empty;

            if (p_Session.TryGetKey("id", out l_Value)) { l_ID = l_Value.Value; }

            if (!string.IsNullOrEmpty(l_ID))
            {
                m_SessionID = l_ID;
                DoAllSubscibes();
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// message_type='notification' subscription_type='channel.follow'
        /// </summary>
        private void Handle_Notification_ChannelFollow(JSONNode p_Event)
        {
            JSONNode l_Value;
            string l_UserID             = string.Empty;
            string l_UserLogin          = string.Empty;
            string l_UserName           = string.Empty;
            string l_BroadcasterUserID  = string.Empty;

            if (p_Event.TryGetKey("user_id",                out l_Value)) { l_UserID            = l_Value.Value; }
            if (p_Event.TryGetKey("user_login",             out l_Value)) { l_UserLogin         = l_Value.Value; }
            if (p_Event.TryGetKey("user_name",              out l_Value)) { l_UserName          = l_Value.Value; }
            if (p_Event.TryGetKey("broadcaster_user_id",    out l_Value)) { l_BroadcasterUserID = l_Value.Value; }

            if (!string.IsNullOrEmpty(l_UserID) && !string.IsNullOrEmpty(l_UserLogin) && !string.IsNullOrEmpty(l_UserName) && !string.IsNullOrEmpty(l_BroadcasterUserID))
            {
                var l_FollowUser = m_TwitchService.GetTwitchUser(l_UserID, l_UserLogin, l_UserName);
                var l_FollowChannel = m_TwitchService._ChannelsRaw.Select(x => x.Value).FirstOrDefault(x => x.Roomstate.RoomId == l_BroadcasterUserID);

                if (l_FollowUser != null && !l_FollowUser._HadFollowed)
                {
                    l_FollowUser._HadFollowed = true;
                    m_TwitchService.m_OnChannelFollowCallbacks?.InvokeAll(m_TwitchService, l_FollowChannel, l_FollowUser);
                }
            }
        }
    }
}
