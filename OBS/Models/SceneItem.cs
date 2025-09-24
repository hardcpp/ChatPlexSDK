using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using static HMUI.NavigationController;

namespace CP_SDK.OBS.Models
{
    /// <summary>
    /// Source item model
    /// </summary>
    public class SceneItem
    {
        private static Pool.MTObjectPool<SceneItem> s_Pool = new Pool.MTObjectPool<SceneItem>(createFunc: () => new SceneItem(), defaultCapacity: 400);

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        public class SceneItemTransform
        {
            public int      alignment       { get; internal set; }
            public int      boundsAlignment { get; internal set; }
            public float    boundsHeight    { get; internal set; }
            public string   boundsType      { get; internal set; }
            public float    boundsWidth     { get; internal set; }
            public int      cropBottom      { get; internal set; }
            public int      cropLeft        { get; internal set; }
            public int      cropRight       { get; internal set; }
            public int      cropTop         { get; internal set; }
            public float    height          { get; internal set; }
            public float    positionX       { get; internal set; }
            public float    positionY       { get; internal set; }
            public float    rotation        { get; internal set; }
            public float    scaleX          { get; internal set; }
            public float    scaleY          { get; internal set; }
            public float    sourceHeight    { get; internal set; }
            public float    sourceWidth     { get; internal set; }
            public float    width           { get; internal set; }

            /// <summary>
            /// Deserialize from JObject
            /// </summary>
            /// <param name="p_Object">JObject to deserialize</param>
            internal void Deserialize(JObject p_Object)
            {
                alignment       = p_Object["alignment"]?.Value<int>()       ?? 0;
                boundsAlignment = p_Object["boundsAlignment"]?.Value<int>() ?? 0;
                boundsHeight    = p_Object["boundsHeight"]?.Value<float>()  ?? 0f;
                boundsType      = p_Object["boundsType"]?.Value<string>()   ?? string.Empty;
                boundsWidth     = p_Object["boundsWidth"]?.Value<float>()   ?? 0f;
                cropBottom      = p_Object["cropBottom"]?.Value<int>()      ?? 0;
                cropLeft        = p_Object["cropLeft"]?.Value<int>()        ?? 0;
                cropRight       = p_Object["cropRight"]?.Value<int>()       ?? 0;
                cropTop         = p_Object["cropTop"]?.Value<int>()         ?? 0;
                height          = p_Object["height"]?.Value<float>()        ?? 0f;
                positionX       = p_Object["positionX"]?.Value<float>()     ?? 0f;
                positionY       = p_Object["positionY"]?.Value<float>()     ?? 0f;
                rotation        = p_Object["rotation"]?.Value<float>()      ?? 0f;
                scaleX          = p_Object["scaleX"]?.Value<float>()        ?? 0f;
                scaleY          = p_Object["scaleY"]?.Value<float>()        ?? 0f;
                sourceHeight    = p_Object["sourceHeight"]?.Value<float>()  ?? 0f;
                sourceWidth     = p_Object["sourceWidth"]?.Value<float>()   ?? 0f;
                width           = p_Object["width"]?.Value<float>()         ?? 0f;
            }
        }


        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        public Scene                    OwnerScene          { get; internal set; } = null;
        public SceneItem                OwnerSceneItem      { get; internal set; } = null;
        public List<SceneItem>          SubItems            { get; internal set; } = null;
        public List<SceneItemFilter>    Filters             { get; internal set; } = null;

        public string               sourceUuid          { get; internal set; } = "";
        public string               sourceType          { get; internal set; } = "";
        public string               sourceName          { get; internal set; } = "";
        public SceneItemTransform   sceneItemTransform  { get; internal set; } = new SceneItemTransform();
        public int                  sceneItemId         { get; internal set; } = 0;
        public int                  sceneItemIndex      { get; internal set; } = 0;
        public bool                 sceneItemLocked     { get; internal set; } = false;
        public bool                 sceneItemEnabled    { get; internal set; } = true;
        public string               sceneItemBlendMode  { get; internal set; } = "";
        public string               inputKind           { get; internal set; } = "";

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        private SceneItem() { }
        /// <summary>
        /// Get new source instance from JObject
        /// </summary>
        /// <param name="p_OwnerScene">Owner scene</param>
        /// <param name="p_OwnerSceneItem">Owner scene item</param>
        /// <param name="p_Object">JObject to deserialize</param>
        /// <returns></returns>
        internal static SceneItem FromJObject(Scene p_OwnerScene, SceneItem p_OwnerSceneItem, JObject p_Object)
        {
            var l_Source = s_Pool.Get();
            l_Source.Deserialize(p_OwnerScene, p_OwnerSceneItem, p_Object);

            return l_Source;
        }
        /// <summary>
        /// Release source instance
        /// </summary>
        /// <param name="p_Source"></param>
        internal static void Release(SceneItem p_Source)
        {
            s_Pool.Release(p_Source);

            if (p_Source.SubItems != null)
            {
                try
                {
                    for (int l_I = 0; l_I < p_Source.SubItems.Count; ++l_I)
                        Release(p_Source.SubItems[l_I]);

                    p_Source.SubItems.Clear();
                }
                catch (System.Exception l_Exception)
                {
                    ChatPlexSDK.Logger.Error("[CP_SDK.OBS.Models][SceneItem.Release] Error:");
                    ChatPlexSDK.Logger.Error(l_Exception);
                }

                Pool.MTListPool<SceneItem>.Release(p_Source.SubItems);
            }

            if (p_Source.Filters != null)
            {
                try
                {
                    for (int l_I = 0; l_I < p_Source.Filters.Count; ++l_I)
                        SceneItemFilter.Release(p_Source.Filters[l_I]);

                    p_Source.Filters.Clear();
                }
                catch (System.Exception l_Exception)
                {
                    ChatPlexSDK.Logger.Error("[CP_SDK.OBS.Models][SceneItem.Release] Error:");
                    ChatPlexSDK.Logger.Error(l_Exception);
                }

                Pool.MTListPool<SceneItemFilter>.Release(p_Source.Filters);
            }

            p_Source.SubItems       = null;
            p_Source.Filters        = null;
            p_Source.OwnerScene     = null;
            p_Source.OwnerSceneItem = null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Deserialize from JObject
        /// </summary>
        /// <param name="p_OwnerScene">Owner scene</param>
        /// <param name="p_OwnerSceneItem">Owner scene item</param>
        /// <param name="p_Object">JObject to deserialize</param>
        internal void Deserialize(Scene p_OwnerScene, SceneItem p_OwnerSceneItem, JObject p_Object)
        {
            OwnerScene      = p_OwnerScene;
            OwnerSceneItem  = p_OwnerSceneItem;

            sourceUuid          = p_Object["sourceUuid"]?.Value<string>()       ?? "";
            sourceType          = p_Object["sourceType"]?.Value<string>()       ?? "";
            sourceName          = p_Object["sourceName"]?.Value<string>()       ?? "";
            sceneItemId         = p_Object["sceneItemId"]?.Value<int>()         ?? 0;
            sceneItemIndex      = p_Object["sceneItemIndex"]?.Value<int>()      ?? 0;
            sceneItemLocked     = p_Object["sceneItemLocked"]?.Value<bool>()    ?? false;
            sceneItemEnabled    = p_Object["sceneItemEnabled"]?.Value<bool>()   ?? true;
            sceneItemBlendMode  = p_Object["parentGroupName"]?.Value<string>()  ?? null;
            inputKind           = p_Object["parentGroupName"]?.Value<string>()  ?? null;

            /// If the scene item is a group we need to query sub items
            if (p_Object["isGroup"].Type == JTokenType.Boolean && p_Object["isGroup"].Value<bool>() == true && sourceType == "OBS_SOURCE_TYPE_SCENE")
            {
                Service.SendRequest(
                    "GetGroupSceneItemList",
                    $"Scene_{OwnerScene.sceneUuid}|Group_{sourceUuid}",
                    new JObject() { ["sceneUuid"] = sourceUuid }
                );
            }

            Service.SendRequest(
                "GetSourceFilterList",
                $"Scene_{OwnerScene.sceneUuid}|Filters_{sourceUuid}",
                new JObject() { ["sourceName"] = sourceName }
            );
        }
        internal void DeserializeSubItems(JArray p_JArray)
        {
            var l_SubItemCount = p_JArray.Count;
            if (l_SubItemCount == 0)
            {
                if (SubItems != null)
                {
                    for (int l_I = 0; l_I < SubItems.Count; ++l_I)
                        SceneItem.Release(SubItems[l_I]);

                    SubItems.Clear();
                    Pool.MTListPool<SceneItem>.Release(SubItems);
                    SubItems = null;
                }
                return;
            }

            if (SubItems == null)
            {
                SubItems = Pool.MTListPool<SceneItem>.Get();
                SubItems.Clear();
            }

            var l_OldList = SubItems;
            var l_NewList = Pool.MTListPool<SceneItem>.Get();

            try
            {
                for (int l_I = 0; l_I < l_SubItemCount; ++l_I)
                {
                    var l_JObject   = p_JArray[l_I] as JObject;
                    var l_Existing  = l_OldList.FirstOrDefault(x => x.sourceUuid == (l_JObject["sourceUuid"]?.Value<string>() ?? null));

                    if (l_Existing != null)
                    {
                        l_Existing.Deserialize(OwnerScene, this, l_JObject);
                        l_NewList.Add(l_Existing);
                        l_OldList.Remove(l_Existing);
                    }
                    else
                    {
                        var l_New = SceneItem.FromJObject(OwnerScene, this, l_JObject);
                        l_NewList.Add(l_New);
                    }
                }
            }
            catch (System.Exception l_Exception)
            {
                ChatPlexSDK.Logger.Error("[CP_SDK.OBS.Models][Scene.DeserializeSubItems] Error:");
                ChatPlexSDK.Logger.Error(l_Exception);
            }
            finally
            {
                SubItems = l_NewList;

                for (int l_I = 0; l_I < l_OldList.Count; ++l_I)
                    Release(l_OldList[l_I]);

                l_OldList.Clear();
                Pool.MTListPool<SceneItem>.Release(l_OldList);
            }
        }
        internal void DeserializeFilters(JArray jarray)
        {
            var l_SubItemCount = jarray.Count;
            if (l_SubItemCount == 0)
            {
                if (Filters != null)
                {
                    for (int l_I = 0; l_I < Filters.Count; ++l_I)
                        SceneItemFilter.Release(Filters[l_I]);

                    Filters.Clear();
                    Pool.MTListPool<SceneItemFilter>.Release(Filters);
                    Filters = null;
                }
                return;
            }

            if (Filters == null)
            {
                Filters = Pool.MTListPool<SceneItemFilter>.Get();
                Filters.Clear();
            }

            var l_OldList = Filters;
            var l_NewList = Pool.MTListPool<SceneItemFilter>.Get();

            try
            {
                for (int l_I = 0; l_I < l_SubItemCount; ++l_I)
                {
                    var l_JObject = jarray[l_I] as JObject;
                    var l_Existing = l_OldList.FirstOrDefault(x => x.filterName == (l_JObject["filterName"]?.Value<string>() ?? null));

                    if (l_Existing != null)
                    {
                        l_Existing.Deserialize(this, l_JObject);
                        l_NewList.Add(l_Existing);
                        l_OldList.Remove(l_Existing);
                    }
                    else
                    {
                        var l_New = SceneItemFilter.FromJObject(this, l_JObject);
                        l_NewList.Add(l_New);
                    }
                }
            }
            catch (System.Exception l_Exception)
            {
                ChatPlexSDK.Logger.Error("[CP_SDK.OBS.Models][Scene.DeserializeFilters] Error:");
                ChatPlexSDK.Logger.Error(l_Exception);
            }
            finally
            {
                Filters = l_NewList;

                for (int l_I = 0; l_I < l_OldList.Count; ++l_I)
                    SceneItemFilter.Release(l_OldList[l_I]);

                l_OldList.Clear();
                Pool.MTListPool<SceneItemFilter>.Release(l_OldList);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set this source item enabled
        /// </summary>
        /// <param name="p_Enabled">New value</param>
        public void SetEnabled(bool p_Enabled)
            => Service.SetSceneItemEnabled(OwnerScene, OwnerSceneItem, this, p_Enabled);
        /// <summary>
        /// Set this source muted
        /// </summary>
        /// <param name="p_Muted">New state</param>
        public void SetMuted(bool p_Muted)
            => Service.SetInputMute(OwnerScene, OwnerSceneItem, this, p_Muted);
        /// <summary>
        /// Toggle this source mute state
        /// </summary>
        public void ToggleMute()
            => Service.ToggleInputMute(OwnerScene, OwnerSceneItem, this);
    }
}
