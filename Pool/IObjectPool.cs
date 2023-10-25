namespace CP_SDK.Pool
{
    /// <summary>
    /// Object pool interface
    /// </summary>
    /// <typeparam name="t_Type">Pooled object type</typeparam>
    public interface IObjectPool<t_Type> where t_Type : class
    {
        /// <summary>
        /// Released element
        /// </summary>
        int CountInactive { get; }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Simple get
        /// </summary>
        /// <returns></returns>
        t_Type Get();
        /// <summary>
        /// Release an element
        /// </summary>
        /// <param name="p_Element">Element to release</param>
        void Release(t_Type p_Element);

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Clear the object pool
        /// </summary>
        void Clear();
    }
}