using Newtonsoft.Json;
using System;
using System.IO;

namespace CP_SDK
{
    /// <summary>
    /// ChatPlex SDK config
    /// </summary>
    internal class ChatPlexServiceConfig : Config.JsonConfig<ChatPlexServiceConfig>
    {
        [JsonProperty] internal string Token = "";

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get relative config path
        /// </summary>
        /// <returns></returns>
        public override string GetRelativePath()
            => $"ChatPlexService";
        public override string GetFullPath()
            => System.IO.Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), $".ChatPlex/{GetRelativePath()}.json"));

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On config init
        /// </summary>
        /// <param name="p_OnCreation">On creation</param>
        protected override void OnInit(bool p_OnCreation)
        {

        }
    }
}