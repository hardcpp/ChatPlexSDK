using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

namespace TMPro
{
#if !CP_SDK_UNITY_TMPROPROXY_CUSTOM
    public static class TMProProxy
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TMP_Text_SetFontStyle(TMP_Text p_Text, FontStyles p_Style)
            => p_Text.fontStyle = p_Style;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TMP_Text_SetTextOverflowMode(TMP_Text p_Text, TextOverflowModes p_OverflowMode)
            => p_Text.overflowMode = p_OverflowMode;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TMP_Text_SetAlignment(TMP_Text p_Text, TextAlignmentOptions p_Align)
            => p_Text.alignment = p_Align;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TextAlignmentOptions Convert(TextAlignmentOptions p_Align)
            => p_Align;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TMP_FontAsset CreateFontAsset(Font p_Font)
            => TMP_FontAsset.CreateFontAsset(p_Font);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSimpleHashCode(string p_String)
        {
            int num = 0;
            for (int i = 0; i < p_String.Length; i++)
            {
                num = ((num << 5) + num) ^ p_String[i];
            }

            return num;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector2 TMP_Text_GetRendererValues(TMP_Text p_Text)
            => p_Text.GetRenderedValues();

    }
#endif
}
