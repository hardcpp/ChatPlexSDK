using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CP_SDK.Chat.Models.Twitch
{
    public interface IHelixModel
    {

    }

    public class HelixEmptyModel : IHelixModel
    {

    }

    public class THelixMultiModel<t_Type> : IHelixModel
        where t_Type : IHelixModel, new()
    {
        [JsonProperty]
        public t_Type[] data;
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Ban
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_Ban : IHelixModel
    {
        [JsonProperty]
        public string broadcaster_id { get; protected set; }
        [JsonProperty]
        public string moderator_id { get; protected set; }
        [JsonProperty]
        public string user_id { get; protected set; }
        [JsonProperty]
        public string mocreated_atderator_id { get; protected set; }
        [JsonProperty]
        public string end_time { get; protected set; }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Clip
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_Clip : IHelixModel
    {
        [JsonProperty] public string edit_url { get; protected set; }
        [JsonProperty] public string id { get; protected set; }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// HypeTrain
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_HypeTrain : IHelixModel
    {
        [Serializable]
        public class Event_Data
        {
            [Serializable]
            public class Contribution
            {
                [JsonProperty] public int total { get; protected set; }
                [JsonProperty] public string type { get; protected set; }
                [JsonProperty] public string user { get; protected set; }
            }

            [JsonProperty] public string broadcaster_id { get; protected set; }
            [JsonProperty] public DateTime cooldown_end_time { get; protected set; }
            [JsonProperty] public DateTime expires_at { get; protected set; }
            [JsonProperty] public int goal { get; protected set; }
            [JsonProperty] public string id { get; protected set; }
            [JsonProperty] public Contribution last_contribution { get; protected set; }
            [JsonProperty] public int level { get; protected set; }
            [JsonProperty] public DateTime started_at { get; protected set; }
            [JsonProperty] public List<Contribution> top_contributions { get; protected set; } = new List<Contribution>();
            [JsonProperty] public int total { get; protected set; }
        }

        [JsonProperty] public string id { get; protected set; }
        [JsonProperty] public string event_type { get; protected set; }
        [JsonProperty] public DateTime event_timestamp { get; protected set; }
        [JsonProperty] public string version { get; protected set; }
        [JsonProperty] public Event_Data event_data { get; protected set; }

        public static bool HasChanged(Helix_HypeTrain p_Left, Helix_HypeTrain p_Right)
        {
            if (p_Left == null && p_Right == null)
                return false;

            if ((p_Left == null && p_Right != null) || (p_Left != null && p_Right == null))
                return true;

            if ((p_Left.event_data == null && p_Right.event_data != null) || (p_Left.event_data != null && p_Right.event_data == null))
                return true;

            return p_Left.id != p_Right.id
                || p_Left.event_data.started_at != p_Right.event_data.started_at
                || p_Left.event_data.level != p_Right.event_data.level
                || p_Left.event_data.total != p_Right.event_data.total;
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Marker
    ////////////////////////////////////////////////////////////////////////////

    [Serializable]
    public class Helix_Marker : IHelixModel
    {
        [JsonProperty]
        public string id { get; protected set; }
        [JsonProperty]
        public string created_at { get; protected set; }
        [JsonProperty]
        public int position_seconds { get; protected set; }
        [JsonProperty]
        public string description { get; protected set; }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Prediction
    ////////////////////////////////////////////////////////////////////////////

    public enum EHelix_PredictionStatus
    {
        ACTIVE,
        LOCKED,
        RESOLVED,
        CANCELED
    }
    public enum EHelix_PredictionColor
    {
        BLUE,
        PINK
    }

    [Serializable]
    public class Helix_Prediction : IHelixModel
    {
        [Serializable]
        public class Outcome
        {
            [JsonProperty] public string id { get; protected set; }
            [JsonProperty] public string title { get; protected set; }
            [JsonProperty] public int users { get; protected set; }
            [JsonProperty] public int channel_points { get; protected set; }
            //[JsonProperty] public object top_predictors { get; protected set; }
            [JsonConverter(typeof(StringEnumConverter))]
            [JsonProperty] public EHelix_PredictionColor color { get; protected set; }
        }

        [JsonProperty] public string id { get; protected set; }
        [JsonProperty] public string broadcaster_id { get; protected set; }
        [JsonProperty] public string broadcaster_name { get; protected set; }
        [JsonProperty] public string broadcaster_login { get; protected set; }
        [JsonProperty] public string title { get; protected set; }
        [JsonProperty] public string winning_outcome_id { get; protected set; }
        [JsonProperty] public List<Outcome> outcomes { get; protected set; } = new List<Outcome>();
        [JsonProperty] public int prediction_window { get; protected set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty] public EHelix_PredictionStatus status { get; protected set; }
        [JsonProperty] public DateTime created_at { get; protected set; }
        [JsonProperty] public DateTime? ended_at { get; protected set; }
        [JsonProperty] public DateTime? locked_at { get; protected set; }

        public static bool HasChanged(Helix_Prediction p_Left, Helix_Prediction p_Right)
        {
            if (p_Left == null && p_Right == null)
                return false;

            if ((p_Left == null && p_Right != null) || (p_Left != null && p_Right == null))
                return true;

            if ((p_Left.outcomes == null && p_Right.outcomes != null) || (p_Left.outcomes != null && p_Right.outcomes == null))
                return true;

            return p_Left.id != p_Right.id
                || p_Left.status != p_Right.status
                || p_Left.outcomes.Sum(x => x.channel_points) != p_Right.outcomes.Sum(x => x.channel_points);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// Poll
    ////////////////////////////////////////////////////////////////////////////

    public enum EHelix_PollStatus
    {
        ACTIVE,
        COMPLETED,
        TERMINATED,
        ARCHIVED,
        MODERATED,
        INVALID
    }

    [Serializable]
    public class Helix_Poll : IHelixModel
    {
        [Serializable]
        public class Choice
        {
            [JsonProperty] public string id { get; protected set; }
            [JsonProperty] public string title { get; protected set; }
            [JsonProperty] public int votes { get; protected set; }
            [JsonProperty] public int channel_points_votes { get; protected set; }
            [JsonProperty] public int bits_votes { get; protected set; }
        }

        [JsonProperty] public string id { get; protected set; }
        [JsonProperty] public string broadcaster_id { get; protected set; }
        [JsonProperty] public string broadcaster_name { get; protected set; }
        [JsonProperty] public string broadcaster_login { get; protected set; }
        [JsonProperty] public string title { get; protected set; }
        [JsonProperty] public List<Choice> choices { get; protected set; } = new List<Choice>();
        [JsonProperty] public bool bits_voting_enabled { get; protected set; }
        [JsonProperty] public int bits_per_vote { get; protected set; }
        [JsonProperty] public bool channel_points_voting_enabled { get; protected set; }
        [JsonProperty] public int channel_points_per_vote { get; protected set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty] public EHelix_PollStatus status { get; protected set; }
        [JsonProperty] public int duration { get; protected set; }
        [JsonProperty] public DateTime started_at { get; protected set; }
        [JsonProperty] public DateTime? ended_at { get; protected set; }

        public static bool HasChanged(Helix_Poll p_Left, Helix_Poll p_Right)
        {
            if (p_Left == null && p_Right == null)
                return false;

            if ((p_Left == null && p_Right != null) || (p_Left != null && p_Right == null))
                return true;

            if ((p_Left.choices == null && p_Right.choices != null) || (p_Left.choices != null && p_Right.choices == null))
                return true;

            return p_Left.id != p_Right.id
                || p_Left.status != p_Right.status
                || p_Left.choices.Sum(x => x.votes) != p_Right.choices.Sum(x => x.votes);
        }
    }

    ////////////////////////////////////////////////////////////////////////////
    /// User
    ////////////////////////////////////////////////////////////////////////////

    public enum EHelix_UserType
    {
        admin,
        global,
        staff
    }
    public enum EHelix_BroadcasterType
    {
        affiliate,
        partner
    }

    [Serializable]
    public class Helix_User : IHelixModel
    {
        [JsonProperty] public string id { get; protected set; }
        [JsonProperty] public string login { get; protected set; }
        [JsonProperty] public string display_name { get; protected set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty] public EHelix_UserType? type { get; protected set; }
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty] public EHelix_BroadcasterType? broadcaster_type { get; protected set; }
        [JsonProperty] public string description { get; protected set; }
        [JsonProperty] public string profile_image_url { get; protected set; }
        [JsonProperty] public string offline_image_url { get; protected set; }
        [JsonProperty] public int view_count { get; protected set; }
        [JsonProperty] public string created_at { get; protected set; }
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
