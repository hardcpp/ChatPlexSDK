using CP_SDK.OBS.Models;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace CP_SDK.OBS
{
    /// <summary>
    /// Server update handlers
    /// </summary>
    public partial class Service
    {
        /// <summary>
        /// RecordStateChanged event callback
        /// </summary>
        /// <param name="p_JObject">Reply</param>
        private static void HandleEvent_RecordStateChanged(JObject p_JObject)
        {
            var l_OldValue = IsRecording;
            var l_NewState = p_JObject["outputActive"]?.Value<bool>() ?? false;

            if (l_OldValue != l_NewState)
            {
                IsRecording = l_NewState;
                OnRecordingStatusChanged?.Invoke(l_NewState);
            }

            LastRecordedFileName = p_JObject["outputPath"]?.Value<string>() ?? null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// CurrentPreviewSceneChanged event callback
        /// </summary>
        /// <param name="p_JObject">Reply</param>
        private static void HandleEvent_CurrentPreviewSceneChanged(JObject p_JObject)
        {
            var l_NewActiveSceneUUID = p_JObject["sceneUuid"]?.Value<string>() ?? null;

            DeserializeScene(l_NewActiveSceneUUID, p_JObject);

            if ((ActivePreviewScene == null || ActivePreviewScene.sceneUuid != l_NewActiveSceneUUID) && m_Scenes.TryGetValue(l_NewActiveSceneUUID, out var l_Scene))
            {
                var l_OldScene = ActivePreviewScene;
                ActivePreviewScene = l_Scene;

                OnActivePreviewSceneChanged?.Invoke(l_OldScene, l_Scene);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// CurrentProgramSceneChanged event callback
        /// </summary>
        /// <param name="p_JObject">Reply</param>
        private static void HandleEvent_CurrentProgramSceneChanged(JObject p_JObject)
        {
            var l_NewActiveSceneUUID = p_JObject["sceneUuid"]?.Value<string>() ?? null;

            DeserializeScene(l_NewActiveSceneUUID, p_JObject);

            if ((ActiveProgramScene == null || ActiveProgramScene.sceneName != l_NewActiveSceneUUID) && m_Scenes.TryGetValue(l_NewActiveSceneUUID, out var l_Scene))
            {
                var l_OldScene = ActiveProgramScene;
                ActiveProgramScene = l_Scene;

                OnActiveProgramSceneChanged?.Invoke(l_OldScene, l_Scene);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// SceneItemEnableStateChanged event callback
        /// </summary>
        /// <param name="p_JObject">Reply</param>
        private static void HandleEvent_SceneItemEnableStateChanged(JObject p_JObject)
        {
            var l_SceneUUID     = p_JObject["sceneUuid"]?.Value<string>() ?? null;
            var l_SceneName     = p_JObject["sceneName"]?.Value<string>() ?? null;
            var l_SceneItemID   = p_JObject["sceneItemId"]?.Value<int>() ?? -1;
            var l_SourceItem    = null as SceneItem;

            if (!m_Scenes.TryGetValue(l_SceneUUID, out var l_Scene))
            {
                foreach (var l_CurrentScene in m_Scenes.Values)
                {
                    var l_Group = l_CurrentScene.GetSourceItemByName(l_SceneName);
                    if (l_Group == null || l_Group.SubItems == null || l_Group.SubItems.Count == 0)
                        continue;

                    l_SourceItem = l_Group.SubItems.FirstOrDefault(x => x.sceneItemId == l_SceneItemID);
                    if (l_SourceItem != null)
                        break;
                }
            }
            else
                l_SourceItem = l_Scene.GetSourceItemByID(l_SceneItemID);

            if (l_SourceItem != null)
            {
                l_SourceItem.sceneItemEnabled = p_JObject["sceneItemEnabled"]?.Value<bool>() ?? true;
                OnSourceVisibilityChanged?.Invoke(l_Scene, l_SourceItem, l_SourceItem.sceneItemEnabled);
            }
            else
            {
                ChatPlexSDK.Logger.Error("[CP_SDK.OBS][Service.Update_SceneItemVisibilityChanged] Source \"" + l_SceneItemID.ToString() + "\" is missing, refreshing all scenes");
                RefreshSceneList();
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// StreamStateChanged event callback
        /// </summary>
        /// <param name="p_JObject">Reply</param>
        private static void HandleEvent_StreamStateChanged(JObject p_JObject)
        {
            var l_OldValue = IsStreaming;
            var l_NewState = p_JObject["outputActive"]?.Value<bool>() ?? false;

            if (l_OldValue != l_NewState)
            {
                IsStreaming = l_NewState;
                OnStreamingStatusChanged?.Invoke(l_NewState);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// StudioModeStateChanged event callback
        /// </summary>
        /// <param name="p_JObject">Reply</param>
        private static void HandleEvent_StudioModeStateChanged(JObject p_JObject)
        {
            var l_OldValue = IsInStudioMode;
            var l_NewState = p_JObject["studioModeEnabled"]?.Value<bool>() ?? false;

            ActivePreviewScene  = ActiveProgramScene;
            IsInStudioMode      = l_NewState;
            OnStudioModeChanged?.Invoke(l_NewState, ActivePreviewScene);
        }
    }
}
