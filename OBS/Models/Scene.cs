using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.SceneManagement;

namespace CP_SDK.OBS.Models
{
    /// <summary>
    /// Scene model
    /// </summary>
    public class Scene
    {
        private static Pool.MTObjectPool<Scene> s_Pool = new Pool.MTObjectPool<Scene>(createFunc: () => new Scene(), defaultCapacity: 40);

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        public string           sceneUuid   { get; private set; } = "";
        public string           sceneName   { get; private set; } = "";
        public int              sceneIndex  { get; private set; } = -1;
        public List<SceneItem>  sceneItems  { get; private set; } = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        private Scene() { }
        /// <summary>
        /// Get new scene instance from JObject
        /// </summary>
        /// <param name="p_Object">JObject to deserialize</param>
        /// <returns></returns>
        internal static Scene FromJObject(JObject p_Object)
        {
            var l_Scene = s_Pool.Get();
            if (l_Scene.sceneItems == null)
            {
                l_Scene.sceneItems = Pool.MTListPool<SceneItem>.Get();
                l_Scene.sceneItems.Clear();
            }

            l_Scene.Deserialize(p_Object);

            return l_Scene;
        }
        /// <summary>
        /// Release scene instance
        /// </summary>
        /// <param name="p_Scene"></param>
        internal static void Release(Scene p_Scene)
        {
            s_Pool.Release(p_Scene);

            if (p_Scene.sceneItems != null)
            {
                try
                {
                    for (int l_I = 0; l_I < p_Scene.sceneItems.Count; ++l_I)
                        SceneItem.Release(p_Scene.sceneItems[l_I]);

                    p_Scene.sceneItems.Clear();
                }
                catch (System.Exception l_Exception)
                {
                    ChatPlexSDK.Logger.Error("[CP_SDK.OBS.Models][Scene.Release] Error:");
                    ChatPlexSDK.Logger.Error(l_Exception);
                }

                Pool.MTListPool<SceneItem>.Release(p_Scene.sceneItems);
            }

            p_Scene.sceneItems = null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Deserialize from JObject
        /// </summary>
        /// <param name="p_Object">JObject to deserialize</param>
        /// <param name="p_SourcesOnly">Only sources?</param>
        internal void Deserialize(JObject p_Object, bool p_SourcesOnly = false)
        {
            if (!p_SourcesOnly)
            {
                sceneUuid    = p_Object["sceneUuid"]?.Value<string>() ?? "";
                sceneName    = p_Object["sceneName"]?.Value<string>() ?? "";
                sceneIndex   = p_Object["sceneIndex"]?.Value<int>()   ?? 0;
            }

            var l_SceneItems        = p_Object.ContainsKey("sceneItems") && p_Object["sceneItems"].Type == JTokenType.Array ? p_Object["sceneItems"] as JArray : null;
            var l_SceneItemCount    = l_SceneItems?.Count;

            if (l_SceneItems != null)
            {
                var l_OldList   = sceneItems;
                var l_NewList   = Pool.MTListPool<SceneItem>.Get();

                try
                {
                    for (int l_I = 0; l_I < l_SceneItemCount; ++l_I)
                    {
                        var l_JObject   = l_SceneItems[l_I] as JObject;
                        var l_Existing  = l_OldList.FirstOrDefault(x => x.sourceUuid == (l_JObject["sourceUuid"]?.Value<string>() ?? null));

                        if (l_Existing != null)
                        {
                            l_Existing.Deserialize(this, l_JObject);
                            l_NewList.Add(l_Existing);
                            l_OldList.Remove(l_Existing);
                        }
                        else
                        {
                            var l_New = SceneItem.FromJObject(this, l_SceneItems[l_I] as JObject);
                            l_NewList.Add(l_New);
                        }
                    }
                }
                catch (System.Exception l_Exception)
                {
                    ChatPlexSDK.Logger.Error("[CP_SDK.OBS.Models][Source.Deserialize] Error:");
                    ChatPlexSDK.Logger.Error(l_Exception);
                }
                finally
                {
                    sceneItems = l_NewList;

                    for (int l_I = 0; l_I < l_OldList.Count; ++l_I)
                        SceneItem.Release(l_OldList[l_I]);

                    l_OldList.Clear();
                    Pool.MTListPool<SceneItem>.Release(l_OldList);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set as active scene
        /// </summary>
        public void SetCurrentProgram()
            => Service.SetCurrentProgramScene(this);
        /// <summary>
        /// Set as active preview scene
        /// </summary>
        public void SetCurrentPreview()
            => Service.SetCurrentPreviewScene(this);
        /// <summary>
        /// Get source by name
        /// </summary>
        /// <param name="p_Name">Name of the source</param>
        /// <returns></returns>
        public SceneItem GetSourceItemByName(string p_Name)
        {
            for (int l_I = 0; l_I < sceneItems.Count; ++l_I)
            {
                var l_Source = sceneItems[l_I];
                if (l_Source.sourceName == p_Name)
                    return l_Source;
            }

            return null;
        }
        /// <summary>
        /// Get source by scene item ID
        /// </summary>
        /// <param name="p_SceneItemID">ID of the source</param>
        /// <returns></returns>
        public SceneItem GetSourceItemByID(int p_SceneItemID)
        {
            for (int l_I = 0; l_I < sceneItems.Count; ++l_I)
            {
                var l_Source = sceneItems[l_I];
                if (l_Source.sceneItemId == p_SceneItemID)
                    return l_Source;
            }

            return null;
        }
    }
}
