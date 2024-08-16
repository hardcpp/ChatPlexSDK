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
        /// <summary>
        /// GetRecordingStatus request callback
        /// </summary>
        /// <param name="p_RequestID">Request ID</param>
        /// <param name="p_Result">Is successfull</param>
        /// <param name="p_JObject">Reply</param>
        private static void HandleRequest_GetRecordStatus(string p_RequestID, bool p_Result, JObject p_JObject)
        {
            if (!p_Result)
                return;

            var l_IsRecording = p_JObject["outputActive"]?.Value<bool>() ?? false;
            if (l_IsRecording != IsRecording)
            {
                IsRecording = l_IsRecording;
                OnRecordingStatusChanged?.Invoke(IsRecording);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// GetSceneItemList request callback
        /// </summary>
        /// <param name="p_RequestID">Request ID</param>
        /// <param name="p_Result">Is successfull</param>
        /// <param name="p_JObject">Reply</param>
        private static void HandleRequest_GetSceneItemList(string p_RequestID, bool p_Result, JObject p_JObject)
        {
            if (!p_Result || !p_RequestID.StartsWith("Scene_"))
                return;

            var l_SceneUUID = p_RequestID.Substring("Scene_".Length);
            DeserializeScene(l_SceneUUID, p_JObject, p_CreateIfMissing: false);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// GetSceneList request callback
        /// </summary>
        /// <param name="p_RequestID">Request ID</param>
        /// <param name="p_Result">Is successfull</param>
        /// <param name="p_JObject">Reply</param>
        private static void HandleRequest_GetSceneList(string p_RequestID, bool p_Result, JObject p_JObject)
        {
            if (!p_Result)
                return;

            var l_CurrentPreviewSceneUuid = p_JObject["currentPreviewSceneUuid"]?.Value<string>() ?? "";
            var l_CurrentProgramSceneUuid = p_JObject["currentProgramSceneUuid"]?.Value<string>() ?? "";

            var l_JsonScenes        = p_JObject["scenes"] as JArray;
            var l_ExistingScenes    = Pool.MTListPool<string>.Get();
            try
            {
                l_ExistingScenes.Clear();

                for (int l_I = 0; l_I < l_JsonScenes.Count; ++l_I)
                {
                    var l_SceneUUID = l_JsonScenes[l_I]["sceneUuid"].Value<string>();
                    DeserializeScene(l_SceneUUID, l_JsonScenes[l_I] as JObject);
                    l_ExistingScenes.Add(l_SceneUUID);
                }

                var l_CurrentSceneList = m_Scenes.Values.ToArray();
                for (int l_I = 0; l_I < l_CurrentSceneList.Length; ++l_I)
                {
                    if (l_ExistingScenes.Contains(l_CurrentSceneList[l_I].sceneUuid))
                        continue;

                    var l_SceneToRemove = l_CurrentSceneList[l_I];
                    if (m_Scenes.TryRemove(l_CurrentSceneList[l_I].sceneUuid, out _))
                    {
                        ChatPlexSDK.Logger.Debug("[CP_SDK.OBS][Service.HandleRequest_GetSceneList] Scene \"" + l_SceneToRemove.sceneName + "\" was destroyed!");
                        Models.Scene.Release(l_SceneToRemove);
                    }
                }
            }
            catch (System.Exception l_Exception)
            {
                ChatPlexSDK.Logger.Error("[CP_SDK.OBS][Service.HandleRequest_GetSceneList] Error:");
                ChatPlexSDK.Logger.Error(l_Exception);
            }
            finally
            {
                l_ExistingScenes.Clear();
                Pool.MTListPool<string>.Release(l_ExistingScenes);
            }

            if (p_RequestID == "FinalGetSceneList")
            {
                OnSceneListRefreshed?.Invoke();

                if ((ActiveProgramScene == null || ActiveProgramScene.sceneUuid != l_CurrentProgramSceneUuid) && m_Scenes.TryGetValue(l_CurrentProgramSceneUuid, out var l_Scene))
                {
                    var l_OldScene = ActiveProgramScene;
                    ActiveProgramScene = l_Scene;
                    OnActiveProgramSceneChanged?.Invoke(l_OldScene, l_Scene);
                }

                if ((ActivePreviewScene == null || ActivePreviewScene.sceneUuid != l_CurrentPreviewSceneUuid) && m_Scenes.TryGetValue(l_CurrentPreviewSceneUuid, out l_Scene))
                {
                    var l_OldScene = ActivePreviewScene;
                    ActivePreviewScene = l_Scene;

                    OnActivePreviewSceneChanged?.Invoke(l_OldScene, l_Scene);
                }
            }
            else if (p_RequestID == "FirstGetSceneList")
            {
                var l_Queries = new List<(string Type, string ID, JObject Data)>();
                foreach (var l_KVP in m_Scenes)
                    l_Queries.Add(("GetSceneItemList", $"Scene_{l_KVP.Value.sceneUuid}", new JObject() { ["sceneUuid"] = l_KVP.Value.sceneUuid }));

                l_Queries.Add(("GetSceneList",              "FinalGetSceneList",    null));
                l_Queries.Add(("GetSceneTransitionList",    null,                   null));
                l_Queries.Add(("GetStreamStatus",           null,                   null));
                l_Queries.Add(("GetRecordStatus",           null,                   null));

                SendRequestBatch("InitialQueries", l_Queries);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// GetSceneTransitionList request callback
        /// </summary>
        /// <param name="p_RequestID">Request ID</param>
        /// <param name="p_Result">Is successfull</param>
        /// <param name="p_JObject">Reply</param>
        private static void HandleRequest_GetSceneTransitionList(string p_RequestID, bool p_Result, JObject p_JObject)
        {
            if (!p_Result)
                return;

            var l_CurrentSceneTransitionUuid = p_JObject["currentSceneTransitionUuid"]?.Value<string>() ?? "";

            var l_JsonTransitions        = p_JObject["transitions"] as JArray;
            var l_ExistingTransitions    = Pool.MTListPool<string>.Get();
            try
            {
                l_ExistingTransitions.Clear();

                for (int l_I = 0; l_I < l_JsonTransitions.Count; ++l_I)
                {
                    var l_TransitionUUID = l_JsonTransitions[l_I]["transitionUuid"].Value<string>();
                    DeserializeTransition(l_TransitionUUID, l_JsonTransitions[l_I] as JObject);
                    l_ExistingTransitions.Add(l_TransitionUUID);
                }

                var l_CurrentTransitionList = m_Transitions.Values.ToArray();
                for (int l_I = 0; l_I < l_CurrentTransitionList.Length; ++l_I)
                {
                    if (l_ExistingTransitions.Contains(l_CurrentTransitionList[l_I].transitionUuid))
                        continue;

                    var l_TransitionToRemove = l_CurrentTransitionList[l_I];
                    if (m_Transitions.TryRemove(l_CurrentTransitionList[l_I].transitionUuid, out _))
                    {
                        ChatPlexSDK.Logger.Debug("[CP_SDK.OBS][Service.HandleRequest_GetSceneTransitionList] Transition \"" + l_TransitionToRemove.transitionName + "\" was destroyed!");
                        Models.Transition.Release(l_TransitionToRemove);
                    }
                }
            }
            catch (System.Exception l_Exception)
            {
                ChatPlexSDK.Logger.Error("[CP_SDK.OBS][Service.HandleRequest_GetSceneTransitionList] Error:");
                ChatPlexSDK.Logger.Error(l_Exception);
            }
            finally
            {
                l_ExistingTransitions.Clear();
                Pool.MTListPool<string>.Release(l_ExistingTransitions);
            }

            OnTransitionListRefreshed?.Invoke();

            if ((ActiveTransition == null || ActiveTransition.transitionUuid != l_CurrentSceneTransitionUuid) && m_Transitions.TryGetValue(l_CurrentSceneTransitionUuid, out var l_Transition))
            {
                var l_OldTransition = ActiveTransition;
                ActiveTransition = l_Transition;

                OnActiveTransitionChanged?.Invoke(l_OldTransition, l_Transition);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// GetStreamStatus request callback
        /// </summary>
        /// <param name="p_RequestID">Request ID</param>
        /// <param name="p_Result">Is successfull</param>
        /// <param name="p_JObject">Reply</param>
        private static void HandleRequest_GetStreamStatus(string p_RequestID, bool p_Result, JObject p_JObject)
        {
            if (!p_Result)
                return;

            var l_IsStreaming = p_JObject["outputActive"]?.Value<bool>() ?? false;
            if (l_IsStreaming != IsStreaming)
            {
                IsStreaming = l_IsStreaming;
                OnStreamingStatusChanged?.Invoke(IsStreaming);
            }
        }
    }
}
