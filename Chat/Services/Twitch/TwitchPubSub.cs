using CP_SDK.Chat.Models.Twitch;
using CP_SDK.Chat.SimpleJSON;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CP_SDK.Chat.Services.Twitch
{
    /// <summary>
    /// Twitch PubSub socket implementation
    /// </summary>
    public class TwitchPubSub
    {
        private TwitchService                       m_TwitchService;
        private Network.WebSocketClient             m_PubSubSocket;
        private DateTime                            m_LastPubSubPing        = DateTime.UtcNow;
        private object                              m_MessageReceivedLock   = new object();
        private Dictionary<string, List<string>>    m_ChannelTopics         = new Dictionary<string, List<string>>();

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_TwitchService">Twitch service instance</param>
        public TwitchPubSub(TwitchService p_TwitchService)
        {
            m_TwitchService = p_TwitchService;

            /// PubSub socket
            m_PubSubSocket = new Network.WebSocketClient();
            m_PubSubSocket.OnOpen               += PubSubSocket_OnOpen;
            m_PubSubSocket.OnClose              += PubSubSocket_OnClose;
            m_PubSubSocket.OnError              += PubSubSocket_OnError;
            m_PubSubSocket.OnMessageReceived    += PubSubSocket_OnMessageReceived;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Connect
        /// </summary>
        public void Connect()
        {
            m_PubSubSocket.Connect("wss://pubsub-edge.twitch.tv:443");
        }
        /// <summary>
        /// Disconnect
        /// </summary>
        public void Disconnect()
        {
            m_PubSubSocket.Disconnect();
            m_ChannelTopics.Clear();
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On tick
        /// </summary>
        /// <param name="p_Now">Current time</param>
        public void OnTick(DateTime p_Now)
        {
            if ((p_Now - m_LastPubSubPing).TotalSeconds >= 60)
            {
                if (m_PubSubSocket.IsConnected)
                    m_PubSubSocket.SendMessage("{ \"type\": \"PING\" }");

                m_LastPubSubPing = p_Now;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Subscribe topics for a channel
        /// </summary>
        /// <param name="p_Topics">Topics to listen</param>
        /// <param name="p_RoomID">ID of the channel</param>
        public void SubscribeTopics(string[] p_Topics, string p_RoomID, string p_ChannelName)
        {
            ChatPlexSDK.Logger.Warning("TwitchPubSub Send topics");

            var l_OAuth = m_TwitchService.OAuthTokenAPI;
            if (l_OAuth != null && l_OAuth.Contains("oauth:"))
                l_OAuth = l_OAuth.Replace("oauth:", "");

            var l_Topics = new JSONArray();

            lock (m_ChannelTopics)
            {
                if (!m_ChannelTopics.ContainsKey(p_RoomID))
                    m_ChannelTopics.Add(p_RoomID, new List<string>());

                foreach (var l_Topic in p_Topics)
                {
                    if (m_ChannelTopics[p_RoomID].Contains(l_Topic))
                        continue;

                    m_ChannelTopics[p_RoomID].Add(l_Topic);

                    if (l_Topic == "video-playback")
                        l_Topics.Add(new JSONString(l_Topic + "." + p_ChannelName));
                    else
                        l_Topics.Add(new JSONString(l_Topic + "." + p_RoomID));
                }
            }

            var l_JSONDataData = new JSONObject();
            l_JSONDataData.Add("topics", l_Topics);
            if (l_OAuth != null)
                l_JSONDataData.Add("auth_token", l_OAuth);

            var l_JSONData = new JSONObject();
            l_JSONData.Add("type", "LISTEN");
            l_JSONData.Add("data", l_JSONDataData);

            m_PubSubSocket.SendMessage(l_JSONData.ToString());
        }
        /// <summary>
        /// Resubscribe all topics for all channel
        /// </summary>
        public void ResubscribeChannelsTopics()
        {
            var l_OAuth = m_TwitchService.OAuthTokenAPI;
            if (l_OAuth != null && l_OAuth.Contains("oauth:"))
                l_OAuth = l_OAuth.Replace("oauth:", "");

            var l_Topics = new JSONArray();
            lock (m_ChannelTopics)
            {
                foreach (var l_CurrentChannel in m_ChannelTopics)
                {
                    foreach (var l_CurrentTopic in l_CurrentChannel.Value)
                    {
                        if (l_CurrentTopic == "video-playback")
                        {
                            var l_Channel = m_TwitchService._ChannelsRaw.Select(x => x.Value).Where(x => x.Roomstate.RoomId == l_CurrentChannel.Key).FirstOrDefault();
                            if (l_Channel != null)
                                l_Topics.Add(new JSONString(l_CurrentTopic + "." + l_Channel.Name));
                        }
                        else
                            l_Topics.Add(new JSONString(l_CurrentTopic + "." + l_CurrentChannel.Key));
                    }
                }
            }

            /// Skip if no topics
            if (l_Topics.Count == 0)
                return;

            var l_JSONDataData = new JSONObject();
            l_JSONDataData.Add("topics", l_Topics);
            if (l_OAuth != null)
                l_JSONDataData.Add("auth_token", l_OAuth);

            var l_JSONData = new JSONObject();
            l_JSONData.Add("type", "LISTEN");
            l_JSONData.Add("data", l_JSONDataData);

            m_PubSubSocket.SendMessage(l_JSONData.ToString());
        }
        /// <summary>
        /// Unsubscribe all topics for a channel
        /// </summary>
        /// <param name="p_RoomID">ID of the channel</param>
        public void UnsubscribeTopics(string p_RoomID, string p_ChannelName)
        {
            var l_OAuth = m_TwitchService.OAuthTokenAPI;
            if (l_OAuth != null && l_OAuth.Contains("oauth:"))
                l_OAuth = l_OAuth.Replace("oauth:", "");

            var l_Topics = new JSONArray();

            lock (m_ChannelTopics)
            {
                if (!m_ChannelTopics.ContainsKey(p_RoomID))
                    m_ChannelTopics.Add(p_RoomID, new List<string>());

                foreach (var l_CurrentTopic in m_ChannelTopics[p_RoomID])
                {
                    if (l_CurrentTopic == "video-playback")
                        l_Topics.Add(new JSONString(l_CurrentTopic + "." + p_ChannelName));
                    else
                        l_Topics.Add(new JSONString(l_CurrentTopic + "." + p_RoomID));
                }

                m_ChannelTopics[p_RoomID].Clear();
            }

            var l_JSONDataData = new JSONObject();
            l_JSONDataData.Add("topics", l_Topics);
            if (l_OAuth != null)
                l_JSONDataData.Add("auth_token", l_OAuth);

            var l_JSONData = new JSONObject();
            l_JSONData.Add("type", "UNLISTEN");
            l_JSONData.Add("data", l_JSONDataData);

            m_PubSubSocket.SendMessage(l_JSONData.ToString());
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Twitch PubSub socket open
        /// </summary>
        private void PubSubSocket_OnOpen()
        {
            ChatPlexSDK.Logger.Info("TwitchPubSub connection opened");
            ResubscribeChannelsTopics();
        }
        /// <summary>
        /// Twitch PubSub socket close
        /// </summary>
        private void PubSubSocket_OnClose()
        {
            ChatPlexSDK.Logger.Info("TwitchPubSub connection closed");
        }
        /// <summary>
        /// Twitch PubSub socket error
        /// </summary>
        private void PubSubSocket_OnError()
        {
            ChatPlexSDK.Logger.Error("An error occurred in TwitchPubSub connection");
        }
        /// <summary>
        /// When a twitch PubSub message is received
        /// </summary>
        /// <param name="p_RawMessage">Raw message</param>
        private void PubSubSocket_OnMessageReceived(string p_RawMessage)
        {
            lock (m_MessageReceivedLock)
            {
                ///ChatPlexSDK.Logger.Warning("TwitchPubSub " + p_RawMessage);

                var l_MessageType = "";
                if (SimpleJSON.JSON.Parse(p_RawMessage).TryGetKey("type", out var l_TypeValue))
                    l_MessageType = l_TypeValue.Value;

                switch (l_MessageType?.ToLower())
                {
                    case "response":
                        var resp = new PubSubTopicListenResponse(p_RawMessage);
                        ChatPlexSDK.Logger.Warning("TwitchPubSub joined topic result " + resp.Successful);
                        break;

                    case "message":
                        ///ChatPlexSDK.Logger.Warning("TwitchPubSub " + SimpleJSON.JSON.Parse(p_RawMessage));
                        var l_Message = new PubSubMessage(p_RawMessage);
                        switch (l_Message.Topic.Split('.')[0])
                        {
                            case "channel-subscribe-events-v1":
                                var l_SubscriptionMessage = l_Message.MessageData as PubSubChannelSubscription;

                                var l_SubscriptionUser      = m_TwitchService.GetTwitchUser(l_SubscriptionMessage.UserId, l_SubscriptionMessage.Username, l_SubscriptionMessage.DisplayName);
                                var l_SubscriptionChannel   = m_TwitchService._ChannelsRaw.Select(x => x.Value).FirstOrDefault(x => x.Roomstate.RoomId == l_SubscriptionMessage.ChannelId);
                                var l_SubscriptionEvent     = new TwitchSubscriptionEvent()
                                {
                                    DisplayName             = l_SubscriptionMessage.DisplayName,
                                    SubPlan                 = l_SubscriptionMessage.SubscriptionPlan.ToString(),
                                    IsGift                  = l_SubscriptionMessage.IsGift,
                                    RecipientDisplayName    = l_SubscriptionMessage.RecipientDisplayName,
                                    PurchasedMonthCount     = System.Math.Max(1, l_SubscriptionMessage.PurchasedMonthCount)
                                };

                                m_TwitchService.m_OnChannelSubscriptionCallbacks?.InvokeAll(m_TwitchService, l_SubscriptionChannel, l_SubscriptionUser, l_SubscriptionEvent);
                                return;

                            case "channel-bits-events-v2":
                                var l_ChannelBitsMessage = l_Message.MessageData as PubSubChannelBitsEvents;

                                var l_BitsUser = m_TwitchService.GetTwitchUser(
                                    !l_ChannelBitsMessage.IsAnonymous ? l_ChannelBitsMessage.UserId   : null,
                                    !l_ChannelBitsMessage.IsAnonymous ? l_ChannelBitsMessage.Username : "AnAnonymousCheerer",
                                    !l_ChannelBitsMessage.IsAnonymous ? l_ChannelBitsMessage.Username : "AnAnonymousCheerer");
                                var l_BitsChannel = m_TwitchService._ChannelsRaw.Select(x => x.Value).FirstOrDefault(x => x.Roomstate.RoomId == l_ChannelBitsMessage.ChannelId);

                                m_TwitchService.m_OnChannelBitsCallbacks?.InvokeAll(m_TwitchService, l_BitsChannel, l_BitsUser, l_ChannelBitsMessage.BitsUsed);
                                break;

                            case "channel-points-channel-v1":
                                var l_ChannelPointsMessage = l_Message.MessageData as PubSubChannelPointsEvents;

                                var l_PointsUser    = m_TwitchService.GetTwitchUser(l_ChannelPointsMessage.UserId, l_ChannelPointsMessage.UserName, l_ChannelPointsMessage.UserDisplayName);
                                var l_PointsChannel = m_TwitchService._ChannelsRaw.Select(x => x.Value).FirstOrDefault(x => x.Roomstate.RoomId == l_ChannelPointsMessage.ChannelId);
                                var l_PointsEvent   = new TwitchChannelPointEvent()
                                {
                                    RewardID        = l_ChannelPointsMessage.RewardID,
                                    TransactionID   = l_ChannelPointsMessage.TransactionID,
                                    Title           = l_ChannelPointsMessage.Title,
                                    Cost            = l_ChannelPointsMessage.Cost,
                                    Prompt          = l_ChannelPointsMessage.Prompt,
                                    UserInput       = l_ChannelPointsMessage.UserInput,
                                    Image           = l_ChannelPointsMessage.Image,
                                    BackgroundColor = l_ChannelPointsMessage.BackgroundColor
                                };

                                m_TwitchService.m_OnChannelPointsCallbacks?.InvokeAll(m_TwitchService, l_PointsChannel, l_PointsUser, l_PointsEvent);
                                break;

                            case "following":
                                var l_FollowMessage = l_Message.MessageData as PubSubFollowing;
                                l_FollowMessage.FollowedChannelId = l_Message.Topic.Split('.')[1];

                                var l_FollowUser    = m_TwitchService.GetTwitchUser(l_FollowMessage.UserId, l_FollowMessage.Username, l_FollowMessage.DisplayName);
                                var l_FollowChannel = m_TwitchService._ChannelsRaw.Select(x => x.Value).FirstOrDefault(x => x.Roomstate.RoomId == l_FollowMessage.FollowedChannelId);

                                if (l_FollowUser != null && !l_FollowUser._HadFollowed)
                                {
                                    l_FollowUser._HadFollowed = true;
                                    m_TwitchService.m_OnChannelFollowCallbacks?.InvokeAll(m_TwitchService, l_FollowChannel, l_FollowUser);
                                }

                                break;

                            case "video-playback":
                                var l_VideoPlaybackMessage  = l_Message.MessageData as PubSubVideoPlayback;
                                var l_Channel               = m_TwitchService._ChannelsRaw.Where(x => x.Key.ToLower() == l_Message.Topic.Split('.')[1].ToLower()).Select(x => x.Value).SingleOrDefault();

                                l_Channel.Live          = l_VideoPlaybackMessage.Type == PubSubVideoPlayback.VideoPlaybackType.StreamUp || l_VideoPlaybackMessage.Viewers != 0;
                                l_Channel.ViewerCount   = l_VideoPlaybackMessage.Viewers;

                                if (l_Channel != null)
                                    m_TwitchService.m_OnLiveStatusUpdatedCallbacks?.InvokeAll(m_TwitchService, l_Channel, l_Channel.Live, l_Channel.ViewerCount);

                                break;

                        }
                        break;

                }
            }
        }

    }
}
