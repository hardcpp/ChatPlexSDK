using IPA.Loader;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CP_SDK.Chat.Models.Twitch
{
    public interface IHelixQuery
    {
        bool Validate(out string p_Message);
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Ban
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_BanUser_Query : IHelixQuery
    {
        [Serializable]
        public class BanQuery
        {
            [JsonProperty] 
            public string user_id { get; set; }
            [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
            public int? duration { get; set; } = null;
            [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
            public string reason { get; set; } = null;

            public BanQuery(string p_UserID, int? p_Duration = null, string p_Reason = null)
            {
                user_id = p_UserID;
                duration = p_Duration;
                reason = p_Reason;
            }
        }

        [JsonProperty] 
        public BanQuery data;

        public bool Validate(out string p_Message)
        {
            p_Message = string.Empty;
            if (data == null)
            {
                p_Message = "You need at least 1 user to ban.";
                return false;
            }

            if (data.duration.HasValue)
                data.duration = data.duration.Value < 1 ? 1 : (data.duration.Value > 1209600 ? 1209600 : data.duration.Value);

            if (data.reason != null && data.reason.Length > 500)
                data.reason = data.reason.Substring(0, 500);

            return true;
        }
    }

    [Serializable]
    public class Helix_UnbanUser_Query : IHelixQuery
    {
        [JsonProperty]
        public string user_id { get; protected set; }

        public Helix_UnbanUser_Query(string p_UserID)
        {
            user_id = p_UserID;
        }

        public bool Validate(out string p_Message)
        {
            p_Message = string.Empty;
            if (string.IsNullOrEmpty(user_id))
            {
                p_Message = "Missing user_id.";
                return false;
            }

            return true;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Clip
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_CreateClip_Query : IHelixQuery
    {
        [JsonProperty] public bool? has_delay { get; protected set; } = false;

        public Helix_CreateClip_Query(bool p_HasDelay = false)
            => has_delay = p_HasDelay;

        public bool Validate(out string p_Message)
        {
            p_Message = string.Empty;
            return true;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Marker
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_CreateMarker_Query : IHelixQuery
    {
        [JsonProperty]
        public string user_id = string.Empty;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)] 
        public string description { get; protected set; } = null;

        public Helix_CreateMarker_Query(string p_Description = null)
            => description = p_Description;

        public bool Validate(out string p_Message)
        {
            p_Message = string.Empty;
            if (string.IsNullOrEmpty(user_id))
            {
                p_Message = "Missing user_id.";
                return false;
            }

            if (description != null && description.Length > 140)
                description = description.Substring(0, 140);

            return true;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Prediction
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_CreatePrediction_Query : IHelixQuery
    {
        [Serializable]
        public class Choice
        {
            [JsonProperty]
            public string title = "";
        }

        [JsonProperty]
        public string broadcaster_id = "";
        [JsonProperty]
        public string title = "";
        [JsonProperty]
        public List<Choice> outcomes = new List<Choice>();
        [JsonProperty]
        public int prediction_window = 60;

        public Helix_CreatePrediction_Query(string p_Title, int p_PredictionWindow = 60, params string[] p_Outcomes)
        {
            title = p_Title;
            if (p_Outcomes != null)
            {
                outcomes = new List<Choice>();
                Array.ForEach<string>(p_Outcomes, (x) => outcomes.Append(new Choice() { title = x }));
            }
            prediction_window = p_PredictionWindow;
        }

        public bool Validate(out string p_Message)
        {
            p_Message = string.Empty;
            if (string.IsNullOrEmpty(title))
            {
                p_Message = "Title must be between 1 and 45 characters.";
                return false;
            }

            if (title.Length > 45)
                title = title.Substring(0, 45);

            if (outcomes == null || outcomes.Count < 2 || outcomes.Count > 10)
            {
                p_Message = "You need between 2 and 10 outcomes.";
                return false;
            }

            foreach (var l_Outcome in outcomes)
            {
                if (string.IsNullOrEmpty(l_Outcome.title))
                {
                    p_Message = "Outcome. Title must be between 1 and 25 characters.";
                    return false;
                }

                if (l_Outcome.title.Length > 25)
                    l_Outcome.title = l_Outcome.title.Substring(0, 25);
            }

            prediction_window = prediction_window < 15 ? 15 : (prediction_window > 1800 ? 1800 : prediction_window);

            return true;
        }
    }

    [Serializable]
    public class Helix_EndPrediction_Query : IHelixQuery
    {
        [JsonProperty]
        public string broadcaster_id = string.Empty;
        [JsonProperty]
        public string id = string.Empty;
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public EHelix_PredictionStatus status = EHelix_PredictionStatus.RESOLVED;
        [JsonProperty(NullValueHandling=NullValueHandling.Ignore)]
        public string winning_outcome_id = null;
        
        public Helix_EndPrediction_Query(string p_ID, EHelix_PredictionStatus p_Status, string p_Winner = null)
        {
            id                  = p_ID;
            status              = p_Status;
            winning_outcome_id  = p_Winner;
        }

        public bool Validate(out string p_Message)
        {
            p_Message = string.Empty;
            if (string.IsNullOrEmpty(id))
            {
                p_Message = "Missing prediction ID.";
                return false;
            }

            if (status != EHelix_PredictionStatus.RESOLVED && status != EHelix_PredictionStatus.CANCELED && status != EHelix_PredictionStatus.LOCKED)
            {
                p_Message = "Status need to be either RESOLVED or CANCELED or LOCKED.";
                return false;
            }

            return true;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Poll
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_CreatePoll_Query : IHelixQuery
    {
        [Serializable]
        public class Choice
        {
            [JsonProperty]
            public string title = "";
        }

        [JsonProperty]
        public string broadcaster_id = "";
        [JsonProperty]
        public string title = "";
        [JsonProperty]
        public List<Choice> choices = new List<Choice>();
        [JsonProperty]
        public int duration = 15;
        [JsonProperty]
        public bool bits_voting_enabled = false;
        [JsonProperty]
        public int bits_per_vote = 0;
        [JsonProperty]
        public bool channel_points_voting_enabled = false;
        [JsonProperty]
        public int channel_points_per_vote = 0;

        public bool Validate(out string p_Message)
        {
            p_Message = string.Empty;
            if (string.IsNullOrEmpty(title))
            {
                p_Message = "Title must be between 1 and 60 characters.";
                return false;
            }

            if (title.Length > 60)
                title = title.Substring(0, 60);

            if (choices == null || choices.Count < 2 || choices.Count > 5)
            {
                p_Message = "You need between 2 and 5 choices.";
                return false;
            }

            foreach (var l_Choice in choices)
            {
                if (string.IsNullOrEmpty(l_Choice.title) || l_Choice.title.Length > 25)
                {
                    p_Message = "Choice.Title must be between 1 and 25 characters.";
                    return false;
                }

                if (l_Choice.title.Length > 25)
                    l_Choice.title = l_Choice.title.Substring(0, 25);
            }

            duration = duration < 15 ? 15 : (duration > 1800 ? 1800 : duration);
            bits_per_vote = bits_per_vote < 0 ? 0 : (bits_per_vote > 10000 ? 10000 : bits_per_vote);
            channel_points_per_vote = channel_points_per_vote < 0 ? 0 : (channel_points_per_vote > 10000 ? 10000 : channel_points_per_vote);

            return true;
        }
    }

    [Serializable]
    public class Helix_EndPoll_Query : IHelixQuery
    {
        [JsonProperty]
        public string broadcaster_id = string.Empty;
        [JsonProperty]
        public string id = string.Empty;
        [JsonProperty]
        [JsonConverter(typeof(StringEnumConverter))]
        public EHelix_PollStatus status = EHelix_PollStatus.TERMINATED;

        public bool Validate(out string p_Message)
        {
            p_Message = string.Empty;
            if (string.IsNullOrEmpty(id))
            {
                p_Message = "Missing poll ID.";
                return false;
            }

            if (status != EHelix_PollStatus.TERMINATED && status != EHelix_PollStatus.ARCHIVED)
            {
                p_Message = "Status need to be either TERMINATED or ARCHIVED.";
                return false;
            }

            return true;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    ////////////////////////////////////////////////////////////////////////////

    /*
    [Serializable]
    public class Helix_CreateReward
    {
        [JsonProperty]
        public string title = "";
        [JsonProperty]
        public int cost = 0;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string prompt = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? is_enabled = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string background_color = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? is_user_input_required = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? is_max_per_stream_enabled = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? max_per_stream = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? is_max_per_user_per_stream_enabled = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? max_per_user_per_stream = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? is_global_cooldown_enabled = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? global_cooldown_seconds = null;
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? should_redemptions_skip_request_queue = null;

        public bool Validate(out string p_Message)
        {
            p_Message = "";
            if (string.IsNullOrEmpty(title) || title.Length > 60)
            {
                p_Message = "Title must be between 1 and 60 characters.";
                return false;
            }

            return true;
        }
    }

    [Serializable]
    public class Helix_Reward
    {
        [Serializable]
        public class MaxPerStream
        {
            [JsonProperty] public bool is_enabled;
            [JsonProperty] public int max_per_stream;
        }


        [JsonProperty] public string broadcaster_id;
        [JsonProperty] public string broadcaster_login;
        [JsonProperty] public string broadcaster_name;
        [JsonProperty] public string id;
        [JsonProperty] public string title;
        [JsonProperty] public string prompt;
        [JsonProperty] public int cost;
        // image
        // default_image
        [JsonProperty] public string background_color;
        [JsonProperty] public bool is_enabled;


        [JsonProperty] public bool is_user_input_required;

        [JsonProperty] public bool is_max_per_user_per_stream_enabled;
        [JsonProperty] public int max_per_user_per_stream;

        [JsonProperty] public bool is_global_cooldown_enabled;
        [JsonProperty] public int global_cooldown_seconds;

        [JsonProperty] public bool is_paused;
        [JsonProperty] public bool is_in_stock;
        [JsonProperty] public bool should_redemptions_skip_request_queue;
        [JsonProperty] public int redemptions_redeemed_current_stream;
        [JsonProperty] public string cooldown_expires_at;
    }
    */


}
