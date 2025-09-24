using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;

namespace CP_SDK.OBS
{
    /// <summary>
    /// Service request handlers
    /// </summary>
    public partial class Service
    {
        private static void Handle_SMSG_HELLO(JObject p_JObject)
        {
            var l_Reply = new JObject()
            {
                ["rpcVersion"]          = p_JObject.Value<int>("rpcVersion"),
                //["eventSubscriptions"]  = 33
            };

            if (p_JObject.TryGetValue("authentication", out var l_JAuthentication))
            {
                var l_Challenge     = l_JAuthentication["challenge"]?.Value<string>() ?? "";
                var l_Salt          = l_JAuthentication["salt"]?.Value<string>() ?? "";
                var l_Secret        = System.Convert.ToBase64String(Security.SHA256.GetHash(OBSModSettings.Instance.Password + l_Salt));
                var l_AuthResponse  = System.Convert.ToBase64String(Security.SHA256.GetHash(l_Secret + l_Challenge));

                l_Reply["authentication"] = l_AuthResponse;
            }

            SendPayload(EOpcode.CMSG_IDENTIFY, l_Reply);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private static void Handle_SMSG_IDENTIFIED(JObject p_JObject)
        {
            Status = EStatus.Connected;

            SendRequest("GetSceneList", "FirstGetSceneList");
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private static void Handle_SMSG_EVENT(JObject p_JObject)
        {
            var l_EventType = p_JObject["eventType"]?.Value<string>() ?? string.Empty;

            p_JObject.TryGetValue("eventData", out var l_EventData);

            switch (l_EventType)
            {
                case "RecordStateChanged":
                    HandleEvent_RecordStateChanged((JObject)l_EventData);
                    break;
                case "CurrentPreviewSceneChanged":
                    HandleEvent_CurrentPreviewSceneChanged((JObject)l_EventData);
                    break;
                case "CurrentProgramSceneChanged":
                    HandleEvent_CurrentProgramSceneChanged((JObject)l_EventData);
                    break;
                case "SceneItemEnableStateChanged":
                    HandleEvent_SceneItemEnableStateChanged((JObject)l_EventData);
                    break;
                case "StreamStateChanged":
                    HandleEvent_StreamStateChanged((JObject)l_EventData);
                    break;
                case "StudioModeStateChanged":
                    HandleEvent_StudioModeStateChanged((JObject)l_EventData);
                    break;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private static void Handle_SMSG_REQUEST_RESPONSE(JObject p_JObject)
        {
            var l_RequestID     = p_JObject["requestId"]?.Value<string>() ?? string.Empty;
            var l_RequestType   = p_JObject["requestType"]?.Value<string>() ?? string.Empty;
            var l_Result        = p_JObject["requestStatus"]?["result"]?.Value<bool>() ?? false;

            p_JObject.TryGetValue("responseData", out var l_ResponseData);

            switch (l_RequestType)
            {
                case "GetRecordStatus":
                    HandleRequest_GetRecordStatus(l_RequestID, l_Result, (JObject)l_ResponseData);
                    break;
                case "GetSourceFilterList":
                    HandleRequest_GetSourceFilterList(l_RequestID, l_Result, (JObject)l_ResponseData);
                    break;
                case "GetSceneItemList":
                    HandleRequest_GetSceneItemList(l_RequestID, l_Result, (JObject)l_ResponseData);
                    break;
                case "GetGroupSceneItemList":
                    HandleRequest_GetGroupSceneItemList(l_RequestID, l_Result, (JObject)l_ResponseData);
                    break;
                case "GetSceneList":
                    HandleRequest_GetSceneList(l_RequestID, l_Result, (JObject)l_ResponseData);
                    break;
                case "GetSceneTransitionList":
                    HandleRequest_GetSceneTransitionList(l_RequestID, l_Result, (JObject)l_ResponseData);
                    break;
                case "GetStreamStatus":
                    HandleRequest_GetStreamStatus(l_RequestID, l_Result, (JObject)l_ResponseData);
                    break;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private static void Handle_SMSG_REQUEST_BATCH_RESPONSE(JObject p_JObject)
        {
            var l_MasterRequestID   = p_JObject["requestId"]?.Value<string>() ?? string.Empty;
            var l_Results           = p_JObject["results"] as JArray;

            foreach (JObject l_Result in l_Results)
                Handle_SMSG_REQUEST_RESPONSE(l_Result);
        }
    }
}
