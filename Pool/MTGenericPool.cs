namespace CP_SDK.Pool
{
    /// <summary>
    /// Provides a static implementation of Pool.ObjectPool_1.
    /// </summary>
    public class MTGenericPool<T> where T : class, new()
    {
        /// <summary>
        /// Static collection
        /// </summary>
        internal static readonly MTObjectPool<T> s_Pool = new MTObjectPool<T>(() => new T());

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Simple get
        /// </summary>
        /// <returns></returns>
        public static T Get()
            => s_Pool.Get();
        /// <summary>
        /// Release an element
        /// </summary>
        /// <param name="p_Element">Element to release</param>
        public static void Release(T p_Element)
            => s_Pool.Release(p_Element);
    }
}