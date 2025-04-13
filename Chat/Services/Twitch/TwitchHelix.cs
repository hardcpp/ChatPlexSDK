using CP_SDK.Chat.Models.Twitch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CP_SDK.Chat.Services.Twitch
{
    /// <summary>
    /// Global result enum
    /// </summary>
    public enum EHelixResult
    {
        OK,
        InvalidRequest,
        AuthorizationFailed,
        NetworkError,
        TokenMissingScope,
        InvalidResult
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_TokenValidate
    {
        [JsonProperty] public string client_id = "";
        [JsonProperty] public string login = "";
        [JsonProperty] public List<string> scopes = new List<string>();
        [JsonProperty] public string user_id = "";
        [JsonProperty] public int expires_in = 0;
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Twitch HelixAPI
    /// </summary>
    public class TwitchHelix
    {
        internal const int STREAM_UPDATE_INTERVAL = 5000;

        internal const int POLL_UPDATE_INTERVAL = 10000;
        internal const int ACTIVE_POLL_UPDATE_INTERVAL = 2000;

        internal const int HYPETRAIN_UPDATE_INTERVAL = 10000;
        internal const int ACTIVE_HYPETRAIN_UPDATE_INTERVAL = 2000;

        internal const int PREDICTION_UPDATE_INTERVAL = 10000;
        internal const int ACTIVE_PREDICTION_UPDATE_INTERVAL = 2000;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private TwitchService           m_TwitchService             = null;
        private Network.WebClientUnity  m_WebClient                 = new Network.WebClientUnity("https://api.twitch.tv/helix/", TimeSpan.FromSeconds(10), true);
        private Network.WebClientCore   m_WebClientEx               = new Network.WebClientCore("https://api.twitch.tv/helix/", TimeSpan.FromSeconds(10), true);
        private string                  m_TokenUserID               = "";
        private string                  m_TokenUserName             = "";
        private string                  m_APIToken                  = "";
        private List<string>            m_APITokenScopes            = new List<string>();
        private DateTime                m_LastStreamCheckTime       = DateTime.MinValue;
        private DateTime                m_LastPollCheckTime         = DateTime.MinValue;
        private Helix_Poll              m_LastPoll                  = null;
        private DateTime                m_LastHypeTrainCheckTime    = DateTime.MinValue;
        private Helix_HypeTrain         m_LastHypeTrain             = null;
        private DateTime                m_LastPredictionCheckTime   = DateTime.MinValue;
        private Helix_Prediction        m_LastPrediction            = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        public Network.WebClientUnity   WebClient           => m_WebClient;
        public Network.WebClientCore    WebClientEx         => m_WebClientEx;
        public string                   BroadcasterUserID   => m_TokenUserID;
        public string                   TokenUserID         => m_TokenUserID;
        public string                   TokenUserName       => m_TokenUserName;

        public event Action<bool, Helix_TokenValidate, string>  OnTokenValidate;
        public event Action<Helix_Poll>                         OnActivePollChanged;
        public event Action<Helix_HypeTrain>                    OnActiveHypeTrainChanged;
        public event Action<Helix_Prediction>                   OnActivePredictionChanged;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="p_TwitchService">Twitch service instance</param>
        public TwitchHelix(TwitchService p_TwitchService)
        {
            m_TwitchService = p_TwitchService;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On token changed
        /// </summary>
        /// <param name="p_Token">New token</param>
        internal void OnTokenChanged(string p_Token)
        {
            try
            {
                m_WebClient.SetHeader("Client-Id",       TwitchService.TWITCH_CLIENT_ID);
                m_WebClient.SetHeader("Authorization",   "Bearer " + p_Token.Replace("oauth:", ""));
            }
            catch (System.Exception) { }

            try
            {
                m_WebClientEx.SetHeader("Client-Id",     TwitchService.TWITCH_CLIENT_ID);
                m_WebClientEx.SetHeader("Authorization", "Bearer " + p_Token.Replace("oauth:", ""));
            }
            catch (System.Exception) { }

            m_APITokenScopes    = new List<string>();
            m_APIToken          = p_Token;

            ValidateToken();
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Update loop
        /// </summary>
        internal void Update()
        {
            if (string.IsNullOrEmpty(m_TokenUserID))
                return;

            #region Stream
            if ((DateTime.Now - m_LastStreamCheckTime).TotalMilliseconds > STREAM_UPDATE_INTERVAL)
            {
                m_LastStreamCheckTime = DateTime.Now;
                GetStream((p_Status, p_Result, p_Error) =>
                {
                    m_LastStreamCheckTime = DateTime.Now;

                    var l_Channel = m_TwitchService._ChannelsRaw.Select(x => x.Value).FirstOrDefault(x => x.Roomstate.RoomId == BroadcasterUserID);
                    if (p_Result != null && l_Channel != null)
                        m_TwitchService.m_OnLiveStatusUpdatedCallbacks?.InvokeAll(m_TwitchService, l_Channel, true, p_Result.viewer_count);
                    else if (l_Channel != null)
                        m_TwitchService.m_OnLiveStatusUpdatedCallbacks?.InvokeAll(m_TwitchService, l_Channel, false, 0);
                });
            }
            #endregion

            #region Poll
            int l_Interval = POLL_UPDATE_INTERVAL;
            if (m_LastPoll != null && (m_LastPoll.status == EHelix_PollStatus.ACTIVE || m_LastPoll.status == EHelix_PollStatus.COMPLETED || m_LastPoll.status == EHelix_PollStatus.TERMINATED))
                l_Interval = ACTIVE_POLL_UPDATE_INTERVAL;

            if ((DateTime.Now - m_LastPollCheckTime).TotalMilliseconds > l_Interval)
            {
                m_LastPollCheckTime = DateTime.Now;
                GetLastPoll((p_Status, p_Result, p_Error) =>
                {
                    m_LastPollCheckTime = DateTime.Now;

                    if (p_Status == EHelixResult.OK && Helix_Poll.HasChanged(m_LastPoll, p_Result))
                    {
                        m_LastPoll = p_Result;
                        OnActivePollChanged?.Invoke(p_Result);
                    }
                    else if (p_Status != EHelixResult.OK)
                        m_LastPoll = null;
                });
            }
            #endregion

            #region Hype train
            l_Interval = HYPETRAIN_UPDATE_INTERVAL;
            if (m_LastHypeTrain != null && m_LastHypeTrain.event_data.expires_at > DateTime.UtcNow)
                l_Interval = ACTIVE_HYPETRAIN_UPDATE_INTERVAL;

            if ((DateTime.Now - m_LastHypeTrainCheckTime).TotalMilliseconds > l_Interval)
            {
                m_LastHypeTrainCheckTime = DateTime.Now;
                GetLastHypeTrain((p_Status, p_Result, p_Error) =>
                {
                    m_LastHypeTrainCheckTime = DateTime.Now;

                    if (p_Status == EHelixResult.OK && Helix_HypeTrain.HasChanged(m_LastHypeTrain, p_Result))
                    {
                        m_LastHypeTrain = p_Result;
                        OnActiveHypeTrainChanged?.Invoke(p_Result);
                    }
                    else if (p_Status != EHelixResult.OK)
                        m_LastHypeTrain = null;
                });
            }
            #endregion

            #region Prediction
            l_Interval = PREDICTION_UPDATE_INTERVAL;
            if (m_LastPrediction != null && m_LastPrediction.status == EHelix_PredictionStatus.ACTIVE)
                l_Interval = ACTIVE_PREDICTION_UPDATE_INTERVAL;

            if ((DateTime.Now - m_LastPredictionCheckTime).TotalMilliseconds > l_Interval)
            {
                m_LastPredictionCheckTime = DateTime.Now;
                GetLastPrediction((p_Status, p_Result, p_Error) =>
                {
                    m_LastPredictionCheckTime = DateTime.Now;

                    if (p_Status == EHelixResult.OK && Helix_Prediction.HasChanged(m_LastPrediction, p_Result))
                    {
                        m_LastPrediction = p_Result;
                        OnActivePredictionChanged?.Invoke(p_Result);
                    }
                    else if (p_Status != EHelixResult.OK)
                        m_LastPrediction = null;
                });
            }
            #endregion
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Validate API Token
        /// </summary>
        private void ValidateToken()
        {
            var l_WebClient = new Network.WebClientUnity("", TimeSpan.FromSeconds(10), true);
            try
            {
                l_WebClient.SetHeader("Authorization", "OAuth " + m_APIToken.Replace("oauth:", ""));
            }
            catch { }

            l_WebClient.GetAsync("https://id.twitch.tv/oauth2/validate", CancellationToken.None, (p_Result) =>
            {
#if DEBUG
                if (p_Result != null)
                {
                    ChatPlexSDK.Logger.Debug("[CP_SDK.Chat.Service.Twitch][TwitchHelix.ValidateToken] Receiving:");
                    ChatPlexSDK.Logger.Debug(p_Result.BodyString);
                }
#endif

                if (p_Result != null && !p_Result.IsSuccessStatusCode)
                {
                    ChatPlexSDK.Logger.Error("[CP_SDK.Chat.Service.Twitch][TwitchHelix.ValidateToken] Failed with message:");
                    ChatPlexSDK.Logger.Error(p_Result.BodyString);
                }

                if (p_Result != null && p_Result.IsSuccessStatusCode)
                {
                    if (p_Result.TryGetObject<Helix_TokenValidate>(out var l_Validate))
                    {
                        m_APITokenScopes    = new List<string>(l_Validate.scopes);
                        m_TokenUserID       = l_Validate.user_id;
                        m_TokenUserName     = l_Validate.login;

                        OnTokenValidate?.Invoke(true, l_Validate, l_Validate.user_id);
                    }
                    else
                        ChatPlexSDK.Logger.Error("[CP_SDK.Chat.Service.Twitch][TwitchHelix.ValidateToken] Failed to parse reply");
                }
                else
                {
                    m_APITokenScopes.Clear();
                    m_TokenUserID    = string.Empty;
                    m_TokenUserName  = string.Empty;

                    OnTokenValidate?.Invoke(false, null, string.Empty);
                }
            }, true);
        }
        /// <summary>
        /// Has API Token scope
        /// </summary>
        /// <param name="p_Scope">Scope to check</param>
        /// <returns></returns>
        public bool HasTokenPermission(string p_Scope)
        {
            if (m_APITokenScopes == null)
                return false;

            return m_APITokenScopes.Contains(p_Scope, StringComparer.InvariantCultureIgnoreCase);
        }

        ////////////////////////////////////////////////////////////////////////////
        /// Ban
        ////////////////////////////////////////////////////////////////////////////

        public void BanUser(Helix_BanUser_Query.BanQuery p_Query, Action<EHelixResult, Helix_Ban, string> p_Callback)
        {
            PostQuery<Helix_BanUser_Query, THelixMultiModel<Helix_Ban>>(
                "moderator:manage:banned_users",
                $"moderation/bans?broadcaster_id={BroadcasterUserID}&moderator_id={TokenUserID}",
                new Helix_BanUser_Query() { data = p_Query },
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }
        public void UnbanUser(Helix_UnbanUser_Query p_Query, Action<EHelixResult, string> p_Callback)
        {
            if (!p_Query.Validate(out var l_Message))
            {
                ChatPlexSDK.Logger.Error($"[CP_SDK.Chat.Service.Twitch][TwitchHelix: {typeof(Helix_UnbanUser_Query).Name}] Error validating data:");
                ChatPlexSDK.Logger.Error(l_Message);
                return;
            }

            DeleteQuery<HelixEmptyModel>(
                "moderator:manage:banned_users",
                $"moderation/bans?broadcaster_id={BroadcasterUserID}&moderator_id={TokenUserID}&user_id={p_Query.user_id}",
                (p_Status, _, p_Error) => p_Callback?.Invoke(p_Status, p_Error)
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        /// Clip
        ////////////////////////////////////////////////////////////////////////////

        public void CreateClip(Helix_CreateClip_Query p_Query, Action<EHelixResult, Helix_Clip, string> p_Callback)
        {
            PostQuery<Helix_CreateClip_Query, THelixMultiModel<Helix_Clip>>(
                "clips:edit",
                $"clips?broadcaster_id={BroadcasterUserID}&has_delay={p_Query.has_delay}",
                null,
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        /// HypeTrain
        ////////////////////////////////////////////////////////////////////////////

        public void GetLastHypeTrain(Action<EHelixResult, Helix_HypeTrain, string> p_Callback)
        {
            GetQuery<THelixMultiModel<Helix_HypeTrain>>(
                "channel:read:hype_train",
                $"hypetrain/events?broadcaster_id={BroadcasterUserID}&first=1",
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        /// Marker
        ////////////////////////////////////////////////////////////////////////////

        public void CreateMarker(Helix_CreateMarker_Query p_Query, Action<EHelixResult, Helix_Marker, string> p_Callback)
        {
            p_Query.user_id = BroadcasterUserID;

            PostQuery<Helix_CreateMarker_Query, THelixMultiModel<Helix_Marker>>(
                "channel:manage:broadcast",
                $"streams/markers",
                p_Query,
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        /// Prediction
        ////////////////////////////////////////////////////////////////////////////

        public void CreatePrediction(Helix_CreatePrediction_Query p_Query, Action<EHelixResult, Helix_Prediction, string> p_Callback)
        {
            p_Query.broadcaster_id = BroadcasterUserID;

            PostQuery<Helix_CreatePrediction_Query, THelixMultiModel<Helix_Prediction>>(
                "channel:manage:predictions",
                $"predictions",
                p_Query,
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }
        public void GetLastPrediction(Action<EHelixResult, Helix_Prediction, string> p_Callback)
        {
            GetQuery<THelixMultiModel<Helix_Prediction>>(
                "channel:read:predictions",
                $"predictions?broadcaster_id={BroadcasterUserID}&first=1",
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }
        public void EndPrediction(Helix_EndPrediction_Query p_Query, Action<EHelixResult, Helix_Prediction, string> p_Callback)
        {
            p_Query.broadcaster_id = BroadcasterUserID;

            PatchQuery<Helix_EndPrediction_Query, THelixMultiModel<Helix_Prediction>>(
                "channel:manage:predictions",
                $"predictions",
                p_Query,
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        /// Poll
        ////////////////////////////////////////////////////////////////////////////

        public void CreatePoll(Helix_CreatePoll_Query p_Query, Action<EHelixResult, Helix_Poll, string> p_Callback)
        {
            p_Query.broadcaster_id = BroadcasterUserID;

            PostQuery<Helix_CreatePoll_Query, THelixMultiModel<Helix_Poll>>(
                "channel:manage:polls",
                $"polls",
                p_Query,
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }
        public void GetLastPoll(Action<EHelixResult, Helix_Poll, string> p_Callback)
        {
            GetQuery<THelixMultiModel<Helix_Poll>>(
                "channel:read:polls",
                $"polls?broadcaster_id={BroadcasterUserID}&first=1",
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }
        public void EndPoll(Helix_EndPoll_Query p_Query, Action<EHelixResult, Helix_Poll, string> p_Callback)
        {
            p_Query.broadcaster_id = BroadcasterUserID;

            PatchQuery<Helix_EndPoll_Query, THelixMultiModel<Helix_Poll>>(
                "channel:manage:polls",
                $"polls",
                p_Query,
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        /// User
        ////////////////////////////////////////////////////////////////////////////

        public void GetUserByID(string p_ID, Action<EHelixResult, Helix_User, string> p_Callback)
        {
            if (string.IsNullOrEmpty(p_ID))
            {
                p_Callback?.Invoke(EHelixResult.InvalidRequest, null, $"No ID specified");
                return;
            }
            GetQuery<THelixMultiModel<Helix_User>>(
                null,
                $"users?id={p_ID}",
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }
        public void GetUserByLogin(string p_Login, Action<EHelixResult, Helix_User, string> p_Callback)
        {
            if (string.IsNullOrEmpty(p_Login))
            {
                p_Callback?.Invoke(EHelixResult.InvalidRequest, null, $"No Login specified");
                return;
            }
            GetQuery<THelixMultiModel<Helix_User>>(
                null,
                $"users?login={p_Login}",
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        /// Stream
        ////////////////////////////////////////////////////////////////////////////

        public void GetStream(Action<EHelixResult, Helix_Stream, string> p_Callback)
        {
            GetQuery<THelixMultiModel<Helix_Stream>>(
                null,
                $"streams?user_id={BroadcasterUserID}&first=1",
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        /// CustomReward
        ////////////////////////////////////////////////////////////////////////////

        public void GetCustomReward(string p_CustomRewardID, Action<EHelixResult, Helix_CustomReward, string> p_Callback)
        {
            GetQuery<THelixMultiModel<Helix_CustomReward>>(
                null,
                $"channel_points/custom_rewards?broadcaster_id={BroadcasterUserID}&id={p_CustomRewardID}",
                (p_CallResult, p_Result, p_Error) => p_Callback?.Invoke(p_CallResult, p_Result?.data?.Length > 0 ? p_Result.data[0] : null, p_Error)
            );
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private void GetQuery<t_Result>(string p_TokenScope, string p_URL, Action<EHelixResult, t_Result, string> p_Callback)
            where t_Result : class, new()
        {
            if (!string.IsNullOrEmpty(p_TokenScope) && !HasTokenPermission(p_TokenScope))
            {
                p_Callback?.Invoke(EHelixResult.TokenMissingScope, null, $"Missing permission '{p_TokenScope}' from token scopes");
                return;
            }

            m_WebClient.GetAsync(
                p_URL,
                CancellationToken.None,
                (p_Result) => QueryHandler(p_Result, p_Callback),
                true
            );
        }
        private void PostQuery<t_Query, t_Result>(string p_TokenScope, string p_URL, t_Query p_Query, Action<EHelixResult, t_Result, string> p_Callback)
            where t_Query : IHelixQuery
            where t_Result : class, new()
        {
            if (!string.IsNullOrEmpty(p_TokenScope) && !HasTokenPermission(p_TokenScope))
            {
                p_Callback?.Invoke(EHelixResult.TokenMissingScope, null, $"Missing permission '{p_TokenScope}' from token scopes");
                return;
            }

            if (!p_Query.Validate(out var l_Message))
            {
                ChatPlexSDK.Logger.Error($"[CP_SDK.Chat.Service.Twitch][TwitchHelix: {typeof(t_Query).Name}] Error validating data:");
                ChatPlexSDK.Logger.Error(l_Message);
                return;
            }

            m_WebClient.PostAsync(
                p_URL,
                p_Query != null ? Network.WebContent.FromJson(p_Query) : Network.WebContent.FromJson(new JObject()),
                CancellationToken.None,
                (p_Result) => QueryHandler(p_Result, p_Callback),
                true
            );
        }
        private void PatchQuery<t_Query, t_Result>(string p_TokenScope, string p_URL, t_Query p_Query, Action<EHelixResult, t_Result, string> p_Callback)
            where t_Query : IHelixQuery
            where t_Result : class, new()
        {
            if (!string.IsNullOrEmpty(p_TokenScope) && !HasTokenPermission(p_TokenScope))
            {
                p_Callback?.Invoke(EHelixResult.TokenMissingScope, null, $"Missing permission '{p_TokenScope}' from token scopes");
                return;
            }

            if (!p_Query.Validate(out var l_Message))
            {
                ChatPlexSDK.Logger.Error($"[CP_SDK.Chat.Service.Twitch][TwitchHelix: {typeof(t_Query).Name}] Error validating data:");
                ChatPlexSDK.Logger.Error(l_Message);
                return;
            }

            m_WebClient.PatchAsync(
                p_URL,
                p_Query != null ? Network.WebContent.FromJson(p_Query) : Network.WebContent.FromJson(new JObject()),
                CancellationToken.None,
                (p_Result) => QueryHandler(p_Result, p_Callback),
                true
            );
        }
        private void DeleteQuery<t_Result>(string p_TokenScope, string p_URL, Action<EHelixResult, t_Result, string> p_Callback)
            where t_Result : class, new()
        {
            if (!string.IsNullOrEmpty(p_TokenScope) && !HasTokenPermission(p_TokenScope))
            {
                p_Callback?.Invoke(EHelixResult.TokenMissingScope, null, $"Missing permission '{p_TokenScope}' from token scopes");
                return;
            }

            m_WebClient.DeleteAsync(
                p_URL,
                CancellationToken.None,
                (p_Result) => QueryHandler(p_Result, p_Callback),
                true
            );
        }
        private void QueryHandler<t_Result>(Network.WebResponse p_Result, Action<EHelixResult, t_Result, string> p_Callback)
            where t_Result : class, new()
        {
#if DEBUG
            if (p_Result != null)
            {
                ChatPlexSDK.Logger.Debug($"[CP_SDK.Chat.Service.Twitch][TwitchHelix: {typeof(t_Result).Name}] Receiving:");
                ChatPlexSDK.Logger.Debug(p_Result.BodyString);
            }
#endif

            if (p_Result != null && !p_Result.IsSuccessStatusCode)
            {
                ChatPlexSDK.Logger.Error($"[CP_SDK.Chat.Service.Twitch][TwitchHelix: {typeof(t_Result).Name}] Failed with message:");
                ChatPlexSDK.Logger.Error(p_Result.BodyString);
            }

            /// Break the binding for the MTThreadInvoker
            Task.Run(() =>
            {
                if (p_Result == null)
                    p_Callback?.Invoke(EHelixResult.NetworkError, null, "Internal error, could not get a result");
                else if (p_Result.StatusCode == System.Net.HttpStatusCode.BadRequest)
                    p_Callback?.Invoke(EHelixResult.InvalidRequest, null, p_Result.BodyString);
                else if (p_Result.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    p_Callback?.Invoke(EHelixResult.AuthorizationFailed, null, p_Result.BodyString);
                else if (p_Result.IsSuccessStatusCode)
                {
                    if (typeof(t_Result) == typeof(HelixEmptyModel))
                        p_Callback?.Invoke(EHelixResult.OK, null, string.Empty);
                    else if (p_Result.TryGetObject<t_Result>(out var l_HelixResult))
                        p_Callback?.Invoke(EHelixResult.OK, l_HelixResult, string.Empty);
                    else
                        p_Callback?.Invoke(EHelixResult.InvalidResult, l_HelixResult, "Failed to deserialize result");
                }
                else
                    p_Callback?.Invoke(EHelixResult.NetworkError, null, p_Result.BodyString);
            }).ConfigureAwait(false);
        }
    }
}
