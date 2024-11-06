using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;

namespace CP_SDK.OBS
{
    /// <summary>
    /// OBS service holder
    /// </summary>
    public partial class Service
    {
        private static Network.WebSocketClient                          m_Client            = null;
        private static int                                              m_ReferenceCount    = 0;
        private static object                                           m_Object            = new object();
        private static ConcurrentDictionary<string, Models.Scene>       m_Scenes            = new ConcurrentDictionary<string, Models.Scene>();
        private static ConcurrentDictionary<string, Models.Transition>  m_Transitions       = new ConcurrentDictionary<string, Models.Transition>();

        private enum EOpcode
        {
            SMSG_HELLO                  = 0,
            CMSG_IDENTIFY               = 1,
            SMSG_IDENTIFIED             = 2,

            SMSG_EVENT                  = 5,
            CMSG_REQUEST                = 6,
            SMSG_REQUEST_RESPONSE       = 7,
            CMSG_REQUEST_BATCH          = 8,
            SMSG_REQUEST_BATCH_RESPONSE = 9
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Status of the service
        /// </summary>
        public enum EStatus
        {
            Disconnected,
            Connecting,
            Connected,
            Authing,
            AuthRejected
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On scene list refreshed
        /// </summary>
        public static event Action OnSceneListRefreshed;
        /// <summary>
        /// On transition list refreshed
        /// </summary>
        public static event Action OnTransitionListRefreshed;

        /// <summary>
        /// On active program scene changed(Old, New)
        /// </summary>
        public static event Action<Models.Scene, Models.Scene> OnActiveProgramSceneChanged;
        /// <summary>
        /// On active preview scene changed(Old, New)
        /// </summary>
        public static event Action<Models.Scene, Models.Scene> OnActivePreviewSceneChanged;
        /// <summary>
        /// On active transition changed(Old, New)
        /// </summary>
        public static event Action<Models.Transition, Models.Transition> OnActiveTransitionChanged;
        /// <summary>
        /// On source visibility changed (Scene, Source, IsVisible)
        /// </summary>
        public static event Action<Models.Scene, Models.SceneItem, bool> OnSourceVisibilityChanged;
        /// <summary>
        /// On studio mode change(Active, Scene)
        /// </summary>
        public static event Action<bool, Models.Scene> OnStudioModeChanged;
        /// <summary>
        /// On streaming status changed (IsStreaming)
        /// </summary>
        public static event Action<bool> OnStreamingStatusChanged;
        /// <summary>
        /// On recording status changed (IsRecording)
        /// </summary>
        public static event Action<bool> OnRecordingStatusChanged;

        public static EStatus           Status                  { get; private set; } = EStatus.Disconnected;
        public static bool              IsInStudioMode          { get; private set; } = false;
        public static bool              IsStreaming             { get; private set; } = false;
        public static bool              IsRecording             { get; private set; } = false;
        public static string            LastRecordedFileName    { get; private set; } = string.Empty;
        public static Models.Scene      ActiveProgramScene      { get; private set; } = null;
        public static Models.Scene      ActivePreviewScene      { get; private set; } = null;
        public static Models.Transition ActiveTransition        { get; private set; } = null;

        public static ConcurrentDictionary<string, Models.Scene>        Scenes      => m_Scenes;
        public static ConcurrentDictionary<string, Models.Transition>   Transitions => m_Transitions;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Init
        /// </summary>
        internal static void Init()
        {
            OBSModSettings.Instance.Warmup();
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Acquire
        /// </summary>
        public static void Acquire()
        {
            lock (m_Object)
            {
                if (m_ReferenceCount == 0)
                    Create();

                m_ReferenceCount++;
            }
        }
        /// <summary>
        /// Release
        /// </summary>
        /// <param name="p_OnExit">Should release all instances</param>
        public static void Release(bool p_OnExit = false)
        {
            lock (m_Object)
            {
                if (p_OnExit)
                {
                    Destroy();
                    m_ReferenceCount = 0;
                }
                else
                    m_ReferenceCount--;

                if (m_ReferenceCount < 0) m_ReferenceCount = 0;
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Apply config
        /// </summary>
        public static void ApplyConf()
        {
            OBSModSettings.Instance.Save();

            if (m_ReferenceCount > 0)
            {
                if (OBSModSettings.Instance.Enabled)
                {
                    if (m_Client.IsConnected)
                        m_Client.Disconnect();

                    m_Client.Connect("ws://" + OBSModSettings.Instance.Server);
                }
                else if (!OBSModSettings.Instance.Enabled && Status != EStatus.Disconnected && Status != EStatus.AuthRejected)
                    m_Client.Disconnect();
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create
        /// </summary>
        private static void Create()
        {
            if (m_Client != null)
                return;

            m_Client = new Network.WebSocketClient();
            m_Client.ReconnectDelay      = 15 * 1000;
            m_Client.OnOpen             += WebSocket_OnOpen;
            m_Client.OnClose            += WebSocket_OnClose;
            m_Client.OnError            += WebSocket_OnError;
            m_Client.OnMessageReceived  += WebSocket_OnMessageReceived;

            if (OBSModSettings.Instance.Enabled)
            {
                Status = EStatus.Connecting;
                m_Client.Connect("ws://" + OBSModSettings.Instance.Server);
            }
            else
                Status = EStatus.Disconnected;
        }
        /// <summary>
        /// Destroy
        /// </summary>
        private static void Destroy()
        {
            if (m_Client == null)
                return;

            try
            {
                m_Client.Disconnect();
                m_Client.Dispose();
            }
            catch
            {

            }

            m_Client = null;

            foreach (var l_KVP in m_Scenes)
                Models.Scene.Release(l_KVP.Value);

            IsInStudioMode      = false;
            IsStreaming         = false;
            IsRecording         = false;
            ActiveProgramScene         = null;
            ActivePreviewScene  = null;

            m_Scenes.Clear();
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On web socket open
        /// </summary>
        private static void WebSocket_OnOpen()
        {
            ChatPlexSDK.Logger.Info("[CP_SDK.OBS][Service.WebSocket_OnOpen]");

            Status = EStatus.Authing;
        }
        /// <summary>
        /// On web socket close
        /// </summary>
        private static void WebSocket_OnClose(WebSocketCloseStatus? p_CloseStatus, string p_CloseStatusDescription)
        {
            ChatPlexSDK.Logger.Info($"[CP_SDK.OBS][Service.WebSocket_OnClose] {p_CloseStatus}:{p_CloseStatusDescription}");

            if (Status != EStatus.AuthRejected)
                Status = EStatus.Disconnected;
        }
        /// <summary>
        /// On web socket message
        /// </summary>
        /// <param name="p_Message">Received message</param>
        private static void WebSocket_OnMessageReceived(string p_Message)
        {
#if DEBUG
            ChatPlexSDK.Logger.Info("[CP_SDK.OBS][Service.WebSocket_OnMessageReceived]");
#endif

            try
            {
                var l_JObject   = JObject.Parse(p_Message);
                var l_Opcode    = (EOpcode)l_JObject.Value<int>("op");
                var l_Data      = l_JObject["d"] as JObject;

                switch (l_Opcode)
                {
                    case EOpcode.SMSG_HELLO:                    Handle_SMSG_HELLO(l_Data);                  break;
                    case EOpcode.SMSG_IDENTIFIED:               Handle_SMSG_IDENTIFIED(l_Data);             break;

                    case EOpcode.SMSG_EVENT:                    Handle_SMSG_EVENT(l_Data);                  break;
                    case EOpcode.SMSG_REQUEST_RESPONSE:         Handle_SMSG_REQUEST_RESPONSE(l_Data);       break;
                    case EOpcode.SMSG_REQUEST_BATCH_RESPONSE:   Handle_SMSG_REQUEST_BATCH_RESPONSE(l_Data); break;
                }
            }
            catch (System.Exception l_Exception)
            {
                ChatPlexSDK.Logger.Error("[CP_SDK.OBS][Service.WebSocket_OnMessageReceived] Error:");
                ChatPlexSDK.Logger.Error(l_Exception);
            }
        }
        /// <summary>
        /// On web socket error
        /// </summary>
        private static void WebSocket_OnError()
        {
            ChatPlexSDK.Logger.Info($"[CP_SDK.OBS][Service.WebSocket_OnError]");
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Send a payload
        /// </summary>
        /// <param name="p_Opcode">Opcode</param>
        /// <param name="p_Data">Data</param>
        private static void SendPayload(EOpcode p_Opcode, JObject p_Data)
        {
            m_Client.SendMessage(new JObject()
            {
                ["d"] = p_Data,
                ["op"] = (int)p_Opcode
            }.ToString(Newtonsoft.Json.Formatting.None));
        }
        /// <summary>
        /// Send a request
        /// </summary>
        /// <param name="p_Type">Request type</param>
        /// <param name="p_ID">Request ID</param>
        /// <param name="p_Data">Request data</param>
        internal static void SendRequest(string p_Type, string p_ID = null, JObject p_Data = null)
        {
            if (!m_Client.IsConnected)
                return;

            var l_Payload = new JObject()
            {
                ["requestType"] = p_Type,
                ["requestId"]   = !string.IsNullOrEmpty(p_ID) ? p_ID : Guid.NewGuid().ToString()
            };

            if (p_Data != null)
                l_Payload["requestData"] = p_Data;

            SendPayload(EOpcode.CMSG_REQUEST, l_Payload);
        }
        /// <summary>
        /// Send requests in batch
        /// </summary>
        /// <param name="p_RequestID">ID of the request</param>
        /// <param name="p_Requests">List of request type + id + data</param>
        internal static void SendRequestBatch(string p_RequestID, List<(string Type, string ID, JObject Data)> p_Requests)
        {
            if (!m_Client.IsConnected)
                return;

            m_Client.SendMessage(new JObject()
            {
                ["d"] = new JObject()
                {
                    ["requestId"]       = !string.IsNullOrEmpty(p_RequestID) ? p_RequestID : Guid.NewGuid().ToString(),
                    ["haltOnFailure"]   = false,
                    ["requests"]        = new JArray(p_Requests.Select((x) => {
                        var l_Obj = new JObject() {
                            ["requestType"] = x.Type,
                            ["requestId"]   = !string.IsNullOrEmpty(x.ID) ? x.ID : Guid.NewGuid().ToString()
                        };

                        if (x.Data != null)
                            l_Obj["requestData"] = x.Data;

                        return l_Obj;
                    }))
                },
                ["op"] = (int)EOpcode.CMSG_REQUEST_BATCH
            }.ToString(Newtonsoft.Json.Formatting.None));
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Refresh all the scenes
        /// </summary>
        public static void RefreshSceneList()
            => SendRequest("GetSceneList");

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Change current program scene
        /// </summary>
        /// <param name="p_Scene">Scene to switch to</param>
        public static void SetCurrentProgramScene(Models.Scene p_Scene)
        {
            if (p_Scene == null)
                return;

            SendRequest("SetCurrentProgramScene", null, new JObject() { ["sceneUuid"] = p_Scene.sceneUuid });
        }
        /// <summary>
        /// Change current preview scene
        /// </summary>
        /// <param name="p_Scene">Scene to switch to</param>
        public static void SetCurrentPreviewScene(Models.Scene p_Scene)
        {
            if (p_Scene == null)
                return;

            SendRequest("SetCurrentPreviewScene", null, new JObject() { ["sceneUuid"] = p_Scene.sceneUuid });
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Enable studio mode
        /// </summary>
        public static void EnableStudioMode() => SendRequest("SetStudioModeEnabled", null, new JObject() { ["studioModeEnabled"] = true });
        /// <summary>
        /// Disable studio mode
        /// </summary>
        public static void DisableStudioMode() => SendRequest("SetStudioModeEnabled", null, new JObject() { ["studioModeEnabled"] = false });

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Preview transition to scene
        /// </summary>
        /// <param name="p_DurationMS">Transition duration</param>
        /// <param name="p_Transition">Transition</param>
        public static void CustomStudioModeTransition(int p_DurationMS = -1, Models.Transition p_Transition = null)
        {
            var l_SubRequest = new List<(string Type, string ID, JObject Data)>();

            if (p_Transition != null)
                l_SubRequest.Add(("SetCurrentSceneTransition", null, new JObject() { ["transitionName"] = p_Transition.transitionName }));

            if (p_DurationMS != -1)
                l_SubRequest.Add(("SetCurrentSceneTransitionDuration", null, new JObject() { ["transitionDuration"] = p_DurationMS }));

            l_SubRequest.Add(("TriggerStudioModeTransition", null, null));
            SendRequestBatch(null, l_SubRequest);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Start stream
        /// </summary>
        public static void StartStream() => SendRequest("StartStream");
        /// <summary>
        /// Stop streaming
        /// </summary>
        public static void StopStream() => SendRequest("StopStream");

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Start recording
        /// </summary>
        public static void StartRecording() => SendRequest("ToggleRecord", null, new JObject() { ["outputActive"] = true });
        /// <summary>
        /// Stop recording
        /// </summary>
        public static void StopRecording() => SendRequest("ToggleRecord", null, new JObject() { ["outputActive"] = false });
        /// <summary>
        /// Set record filename format
        /// </summary>
        /// <param name="p_Format">New format</param>
        public static void SetProfileParameter_Output_FilenameFormatting(string p_Format)
        {
            SendRequest("SetProfileParameter", null, new JObject()
            {
                ["parameterCategory"]   = "Output",
                ["parameterName"]       = "FilenameFormatting",
                ["parameterValue"]      = p_Format
            });
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set scene item enabled
        /// </summary>
        /// <param name="p_OwnerScene">Scene that contain the source item</param>
        /// <param name="p_OwnerSceneItem">Owner scene item</param>
        /// <param name="p_SourceItem">Source instance</param>
        /// <param name="p_Enabled">New visibility</param>
        public static void SetSceneItemEnabled(Models.Scene p_OwnerScene, Models.SceneItem p_OwnerSceneItem, Models.SceneItem p_SourceItem, bool p_Enabled)
        {
            if (p_OwnerScene == null || p_SourceItem == null)
                return;

            SendRequest("SetSceneItemEnabled", null, new JObject() {
                ["sceneUuid"]           = p_OwnerSceneItem?.sourceUuid ?? p_OwnerScene.sceneUuid,
                ["sceneItemId"]         = p_SourceItem.sceneItemId,
                ["sceneItemEnabled"]    = p_Enabled
            });
        }
        /// <summary>
        /// Set input muted
        /// </summary>
        /// <param name="p_OwnerScene">Scene that contain the source item</param>
        /// <param name="p_OwnerSceneItem">Owner scene item</param>
        /// <param name="p_SourceItem">Source instance</param>
        /// <param name="p_Muted">New state</param>
        public static void SetInputMute(Models.Scene p_OwnerScene, Models.SceneItem p_OwnerSceneItem, Models.SceneItem p_SourceItem, bool p_Muted)
        {
            if (p_OwnerScene == null || p_SourceItem == null)
                return;

            SendRequest("SetInputMute", null, new JObject()
            {
                ["inputUuid"]   = p_SourceItem.sourceUuid,
                ["inputMuted"]  = p_Muted
            });
        }
        /// <summary>
        /// Toggle input mute state
        /// </summary>
        /// <param name="p_OwnerScene">Scene that contain the source item</param>
        /// <param name="p_OwnerSceneItem">Owner scene item</param>
        /// <param name="p_SourceItem">Source instance</param>
        public static void ToggleInputMute(Models.Scene p_OwnerScene, Models.SceneItem p_OwnerSceneItem, Models.SceneItem p_SourceItem)
        {
            if (p_OwnerScene == null || p_SourceItem == null)
                return;

            SendRequest("ToggleInputMute", null, new JObject()
            {
                ["inputUuid"] = p_SourceItem.sourceUuid,
            });
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Try get scene by name
        /// </summary>
        /// <param name="p_SceneName">Scene name</param>
        /// <param name="p_Scene">Out scene</param>
        /// <returns></returns>
        public static bool TryGetSceneByName(string p_SceneName, out Models.Scene p_Scene)
        {
            p_Scene = null;
            if (p_SceneName == "<i>None</i>")
                return false;

            foreach (var l_KVP in m_Scenes)
            {
                if (l_KVP.Value.sceneName != p_SceneName)
                    continue;

                p_Scene = l_KVP.Value;
                return true;
            }

            return false;
        }
        /// <summary>
        /// Try get transition by name
        /// </summary>
        /// <param name="p_TransitionName">Transition name</param>
        /// <param name="p_Transition">Out scene</param>
        /// <returns></returns>
        public static bool TryGetTransitionByName(string p_TransitionName, out Models.Transition p_Transition)
        {
            p_Transition = null;
            if (p_TransitionName == "<i>None</i>")
                return false;

            foreach (var l_KVP in m_Transitions)
            {
                if (l_KVP.Value.transitionName != p_TransitionName)
                    continue;

                p_Transition = l_KVP.Value;
                return true;
            }

            return false;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Deserialize scene
        /// </summary>
        /// <param name="p_SceneUUID">Scene UUID</param>
        /// <param name="p_Scene">Scene content</param>
        /// <param name="p_CreateIfMissing">Create if missing</param>
        private static void DeserializeScene(string p_SceneUUID, JObject p_Scene, bool p_CreateIfMissing = true)
        {
            if (m_Scenes.TryGetValue(p_SceneUUID, out var l_Existing))
                l_Existing.Deserialize(p_Scene, true);
            else if (p_CreateIfMissing)
            {
                var l_NewScene = Models.Scene.FromJObject(p_Scene);
                m_Scenes.TryAdd(p_SceneUUID, l_NewScene);
            }
        }
        /// <summary>
        /// Deserialize transition
        /// </summary>
        /// <param name="p_TransitionUUID">Scene UUID</param>
        /// <param name="p_Transition">Scene content</param>
        /// <param name="p_CreateIfMissing">Create if missing</param>
        private static void DeserializeTransition(string p_TransitionUUID, JObject p_Transition, bool p_CreateIfMissing = true)
        {
            if (m_Transitions.TryGetValue(p_TransitionUUID, out var l_Existing))
                l_Existing.Deserialize(p_Transition);
            else if (p_CreateIfMissing)
            {
                var l_NewTransition = Models.Transition.FromJObject(p_Transition);
                m_Transitions.TryAdd(p_TransitionUUID, l_NewTransition);
            }
        }
    }
}
