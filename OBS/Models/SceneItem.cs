using Newtonsoft.Json.Linq;

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

        public Scene                OwnerScene          { get; internal set; } = null;

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
        /// <param name="p_Scene">Owner scene</param>
        /// <param name="p_Object">JObject to deserialize</param>
        /// <returns></returns>
        internal static SceneItem FromJObject(Scene p_Scene, JObject p_Object)
        {
            var l_Source = s_Pool.Get();
            l_Source.Deserialize(p_Scene, p_Object);

            return l_Source;
        }
        /// <summary>
        /// Release source instance
        /// </summary>
        /// <param name="p_Source"></param>
        internal static void Release(SceneItem p_Source)
        {
            s_Pool.Release(p_Source);
            p_Source.OwnerScene = null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Deserialize from JObject
        /// </summary>
        /// <param name="p_Scene">Owner scene</param>
        /// <param name="p_Object">JObject to deserialize</param>
        internal void Deserialize(Scene p_Scene, JObject p_Object)
        {
            OwnerScene = p_Scene;

            sourceUuid          = p_Object["sourceUuid"]?.Value<string>()       ?? "";
            sourceType          = p_Object["sourceType"]?.Value<string>()       ?? "";
            sourceName          = p_Object["sourceName"]?.Value<string>()       ?? "";
            sceneItemId         = p_Object["sceneItemId"]?.Value<int>()         ?? 0;
            sceneItemIndex      = p_Object["sceneItemIndex"]?.Value<int>()      ?? 0;
            sceneItemLocked     = p_Object["sceneItemLocked"]?.Value<bool>()    ?? false;
            sceneItemEnabled    = p_Object["sceneItemEnabled"]?.Value<bool>()   ?? true;
            sceneItemBlendMode  = p_Object["parentGroupName"]?.Value<string>()  ?? null;
            inputKind           = p_Object["parentGroupName"]?.Value<string>()  ?? null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set this source item enabled
        /// </summary>
        /// <param name="p_Enabled">New value</param>
        public void SetEnabled(bool p_Enabled)
            => Service.SetSceneItemEnabled(OwnerScene, this, p_Enabled);
        /// <summary>
        /// Set this source muted
        /// </summary>
        /// <param name="p_Muted">New state</param>
        public void SetMuted(bool p_Muted)
            => Service.SetInputMute(OwnerScene, this, p_Muted);
        /// <summary>
        /// Toggle this source mute state
        /// </summary>
        public void ToggleMute()
            => Service.ToggleInputMute(OwnerScene, this);
    }
}
