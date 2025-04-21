namespace CP_SDK.Extensions
{
    /// <summary>
    /// String tools
    /// </summary>
    public static class StringU
    {
        /// <summary>
        /// Is hex only string
        /// </summary>
        /// <returns></returns>
        public static bool IsOnlyHexSymbols(this string str)
        {
            for (var l_I = 0; l_I < str.Length; ++l_I)
            {
                var l_Char = str[l_I];
                if ((l_Char >= '0' && l_Char <= '9') || (l_Char >= 'a' && l_Char <= 'f') || (l_Char >= 'A' && l_Char <= 'F'))
                    continue;

                return false;
            }

            return true;
        }
    }
}
