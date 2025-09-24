using Newtonsoft.Json.Linq;

namespace CP_SDK.OBS.Models
{
    /// <summary>
    /// Scene item filter
    /// </summary>
    public class SceneItemFilter
    {
        private static Pool.MTObjectPool<SceneItemFilter> s_Pool = new Pool.MTObjectPool<SceneItemFilter>(createFunc: () => new SceneItemFilter(), defaultCapacity: 400);

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        public SceneItem OwnerSceneItem { get; internal set; } = null;

        public bool     filterEnabled   { get; internal set; }
        public int      filterIndex     { get; internal set; }
        public string   filterKind      { get; internal set; }
        public string   filterName      { get; internal set; }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get new scene item filter instance from JObject
        /// </summary>
        /// <param name="ownerSceneItem">Owner scene item</param>
        /// <param name="jobject">JObject to deserialize</param>
        /// <returns></returns>
        internal static SceneItemFilter FromJObject(SceneItem ownerSceneItem, JObject jobject)
        {
            var l_Source = s_Pool.Get();
            l_Source.Deserialize(ownerSceneItem, jobject);

            return l_Source;
        }
        /// <summary>
        /// Release scene item filter instance
        /// </summary>
        /// <param name="source"></param>
        internal static void Release(SceneItemFilter source)
        {
            s_Pool.Release(source);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Deserialize from JObject
        /// </summary>
        /// <param name="ownerSceneItem">Owner scene item</param>
        /// <param name="jobject">JObject to deserialize</param>
        internal void Deserialize(SceneItem ownerSceneItem, JObject jobject)
        {
            OwnerSceneItem = ownerSceneItem;

            filterEnabled   = jobject["filterEnabled"]?.Value<bool>() ?? false;
            filterIndex     = jobject["filterIndex"]?.Value<int>() ?? 0;
            filterKind      = jobject["filterKind"]?.Value<string>() ?? string.Empty;
            filterName      = jobject["filterName"]?.Value<string>() ?? string.Empty;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set this source item filter enabled state
        /// </summary>
        /// <param name="enabled">New value</param>
        public void SetEnabled(bool enabled)
        {
            filterEnabled = enabled;
            Service.SetSceneItemFilterEnabled(OwnerSceneItem.OwnerScene, OwnerSceneItem, this, enabled);
        }
        /// <summary>
        /// Toggle this source item filter enabled state
        /// </summary>
        public void Toggle()
            => SetEnabled(!filterEnabled);
    }
}
