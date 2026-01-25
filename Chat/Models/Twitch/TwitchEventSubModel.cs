
using System;
using Newtonsoft.Json;

namespace CP_SDK.Chat.Models.Twitch
{
    [Serializable]
    public class EventSub_HypeTrain
    {
        [Serializable]
        public class TopContributions
        {
            [JsonProperty] public string user_id { get; protected set; }
            [JsonProperty] public string user_login { get; protected set; }
            [JsonProperty] public string user_name { get; protected set; }
            [JsonProperty] public string type { get; protected set; }
            [JsonProperty] public int total { get; protected set; }
        }
        
        [JsonProperty] public string id { get; protected set; }
        [JsonProperty] public string broadcaster_user_id { get; protected set; }
        [JsonProperty] public string broadcaster_user_login { get; protected set; }
        [JsonProperty] public string broadcaster_user_name { get; protected set; }
        [JsonProperty] public int total { get; protected set; }
        [JsonProperty] public int? progress { get; protected set; }
        [JsonProperty] public int? goal { get; protected set; }
        [JsonProperty] public TopContributions[] top_contributions { get; protected set; }
        [JsonProperty] public int level { get; protected set; }
        [JsonProperty] public DateTime started_at { get; protected set; }
        [JsonProperty] public DateTime expires_at { get; protected set; }
        [JsonProperty] public DateTime? ended_at { get; protected set; }
        [JsonProperty] public DateTime? cooldown_ends_at { get; protected set; }
        [JsonProperty] public bool is_shared_train { get; protected set; }
        [JsonProperty] public string type { get; protected set; }
        [JsonProperty] public int all_time_high_level { get; protected set; }
        [JsonProperty] public int all_time_high_total { get; protected set; }
    }
}