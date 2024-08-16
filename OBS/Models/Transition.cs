using Newtonsoft.Json.Linq;

namespace CP_SDK.OBS.Models
{
    /// <summary>
    /// Transition model
    /// </summary>
    public class Transition
    {
        private static Pool.MTObjectPool<Transition> s_Pool = new Pool.MTObjectPool<Transition>(createFunc: () => new Transition(), defaultCapacity: 40);

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        public string   transitionUuid          { get; internal set; } = "";
        public string   transitionName          { get; internal set; } = "";
        public string   transitionKind          { get; internal set; } = "";
        public bool     transitionFixed         { get; internal set; } = false;
        public bool     transitionConfigurable  { get; internal set; } = false;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Constructor
        /// </summary>
        private Transition() { }
        /// <summary>
        /// Get new transition instance from JObject
        /// </summary>
        /// <param name="p_Object">JObject to deserialize</param>
        /// <returns></returns>
        internal static Transition FromJObject(JObject p_Object)
        {
            var l_Scene = s_Pool.Get();
            l_Scene.Deserialize(p_Object);

            return l_Scene;
        }
        /// <summary>
        /// Release transition instance
        /// </summary>
        /// <param name="p_Transition"></param>
        internal static void Release(Transition p_Transition)
        {
            s_Pool.Release(p_Transition);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Deserialize from JObject
        /// </summary>
        /// <param name="p_Object">JObject to deserialize</param>
        internal void Deserialize(JObject p_Object)
        {
            transitionUuid          = p_Object["transitionUuid"]?.Value<string>()       ?? "";
            transitionName          = p_Object["transitionName"]?.Value<string>()       ?? "";
            transitionKind          = p_Object["transitionKind"]?.Value<string>()       ?? "";
            transitionFixed         = p_Object["transitionFixed"]?.Value<bool>()        ?? false;
            transitionConfigurable  = p_Object["transitionConfigurable"]?.Value<bool>() ?? false;
        }
    }
}
