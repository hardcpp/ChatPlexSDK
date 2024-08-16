using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace CP_SDK.Unity
{
    /// <summary>
    /// Particle material provider
    /// </summary>
    public static class EnhancedImageParticleMaterialProvider
    {
        private static int SHADER_MAIN_TEXT = Shader.PropertyToID("_MainTex");
        private static int SHADER_BASE_MAP  = Shader.PropertyToID("_BaseMap");

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private static Material m_Material              = null;
        private static Material m_PreviewMaterial       = null;
        private static Material m_CustomMaterial        = null;
        private static Material m_CustomPreviewMaterial = null;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get material
        /// </summary>
        /// <returns></returns>
        public static Material GetMaterial()
        {
            if (m_CustomMaterial)
                return m_CustomMaterial;

            if (!m_Material)
            {
                if (ChatPlexSDK.EmbedAssetBundle != null)
                {
                    var l_BundleMaterial = ChatPlexSDK.EmbedAssetBundle.LoadAsset<Material>("EnhancedImageParticleMaterial");
                    if (l_BundleMaterial != null)
                    {
                        m_Material = l_BundleMaterial;
                        SetMaterialTexture(m_Material, Texture2DU.CreateFromRaw(
                            Misc.Resources.FromRelPath(Assembly.GetExecutingAssembly(), "CP_SDK._Resources.Heart.png")
                        ));

                        return l_BundleMaterial;
                    }
                }

                switch (ChatPlexSDK.RenderPipeline)
                {
                    case ERenderPipeline.BuiltIn:
                        m_Material = CreateBuiltInMaterial();
                        break;

                    case ERenderPipeline.URP:
                        m_Material = CreateURPMaterial();
                        break;

                }
            }

            return m_Material;
        }
        /// <summary>
        /// Get preview material
        /// </summary>
        /// <returns></returns>
        public static Material GetPreviewMaterial()
        {
            if (m_CustomPreviewMaterial)
                return m_CustomPreviewMaterial;

            if (!m_PreviewMaterial)
            {
                if (ChatPlexSDK.EmbedAssetBundle != null)
                {
                    var l_BundleMaterial = ChatPlexSDK.EmbedAssetBundle.LoadAsset<Material>("EnhancedImagePreviewMaterial");
                    if (l_BundleMaterial != null)
                    {
                        m_PreviewMaterial = l_BundleMaterial;
                        return l_BundleMaterial;
                    }
                }
            }

            return m_PreviewMaterial;
        }
        /// <summary>
        /// Destroy instance
        /// </summary>
        internal static void Destroy()
        {
            m_CustomMaterial = null;
            m_CustomPreviewMaterial = null;

            if (!m_Material)
                return;

            GameObject.Destroy(m_Material);
            GameObject.Destroy(m_PreviewMaterial);

            m_Material          = null;
            m_PreviewMaterial   = null;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Set custom material
        /// </summary>
        /// <param name="p_Material">Custom material</param>
        public static void SetCustomMaterial(Material p_Material)
        {
            m_CustomMaterial = p_Material;
        }
        /// <summary>
        /// Set custom preview material
        /// </summary>
        /// <param name="p_PreviewMaterial">Custom preview material</param>
        public static void SetCustomPreviewMaterial(Material p_PreviewMaterial)
        {
            m_CustomPreviewMaterial = p_PreviewMaterial;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Get texture from material
        /// </summary>
        /// <param name="p_Material">Source material</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Texture GetMaterialTexture(Material p_Material)
        {
            switch (ChatPlexSDK.RenderPipeline)
            {
                case ERenderPipeline.BuiltIn:
                    return p_Material.GetTexture(SHADER_MAIN_TEXT);

                case ERenderPipeline.URP:
                    return p_Material.GetTexture(SHADER_BASE_MAP);

            }

            return null;
        }
        /// <summary>
        /// Set material texture
        /// </summary>
        /// <param name="p_Material">Target material</param>
        /// <param name="p_Texture">New texture</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SetMaterialTexture(Material p_Material, Texture p_Texture)
        {
            switch (ChatPlexSDK.RenderPipeline)
            {
                case ERenderPipeline.BuiltIn:
                    p_Material.SetTexture(SHADER_MAIN_TEXT, p_Texture);
                    break;

                case ERenderPipeline.URP:
                    p_Material.SetTexture(SHADER_BASE_MAP, p_Texture);
                    break;

            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Create BuiltIn material
        /// </summary>
        /// <returns></returns>
        private static Material CreateBuiltInMaterial()
        {
            var l_Shader = Shader.Find("Particles/Standard Unlit");
            if (!l_Shader)
                l_Shader = Resources.FindObjectsOfTypeAll<Shader>().FirstOrDefault(x => x.name == "Particles/Standard Unlit");

            var l_Material = new Material(l_Shader);
            l_Material.EnableKeyword("ETC1_EXTERNAL_ALPHA");
            l_Material.EnableKeyword("_ALPHABLEND_ON");
            l_Material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            l_Material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            l_Material.SetOverrideTag("RenderType", "Transparent");
            l_Material.SetFloat("_BlendOp",                         0.00f);
            l_Material.SetFloat("_BumpScale",                       1.00f);
            l_Material.SetFloat("_CameraFadingEnabled",             0.00f);
            l_Material.SetFloat("_CameraFarFadeDistance",           2.00f);
            l_Material.SetFloat("_CameraNearFadeDistance",          1.00f);
            l_Material.SetFloat("_ColorMode",                       0.00f);
            l_Material.SetFloat("_Cull",                            2.00f);
            l_Material.SetFloat("_Cutoff",                          0.50f);
            l_Material.SetFloat("_DetailNormalMapScale",            1.00f);
            l_Material.SetFloat("_DistortionBlend",                 0.50f);
            l_Material.SetFloat("_DistortionEnabled",               0.00f);
            l_Material.SetFloat("_DistortionStrength",              1.00f);
            l_Material.SetFloat("_DistortionStrengthScaled",        0.00f);
            l_Material.SetFloat("_DstBlend",                       10.00f);
            l_Material.SetFloat("_EmissionEnabled",                 0.00f);
            l_Material.SetFloat("_EnableExternalAlpha",             0.00f);
            l_Material.SetFloat("_FlipbookMode",                    0.00f);
            l_Material.SetFloat("_GlossMapScale",                   1.00f);
            l_Material.SetFloat("_Glossiness",                      1.00f);
            l_Material.SetFloat("_GlossyReflections",               0.00f);
            l_Material.SetFloat("_InvFade",                         1.15f);
            l_Material.SetFloat("_LightingEnabled",                 0.00f);
            l_Material.SetFloat("_Metallic",                        0.00f);
            l_Material.SetFloat("_Mode",                            2.00f);
            l_Material.SetFloat("_OcclusionStrength",               1.00f);
            l_Material.SetFloat("_Parallax",                        0.02f);
            l_Material.SetFloat("_SmoothnessTextureChannel",        0.00f);
            l_Material.SetFloat("_SoftParticlesEnabled",            0.00f);
            l_Material.SetFloat("_SoftParticlesFarFadeDistance",    1.00f);
            l_Material.SetFloat("_SoftParticlesNearFadeDistance",   0.00f);
            l_Material.SetFloat("_SrcBlend",                        5.00f);
            l_Material.SetFloat("_UVSec",                           0.00f);
            l_Material.SetFloat("_ZWrite",                          0.00f);
            l_Material.renderQueue      = 3000;
            l_Material.enableInstancing = true;

            SetMaterialTexture(l_Material, Texture2DU.CreateFromRaw(
                Misc.Resources.FromRelPath(Assembly.GetExecutingAssembly(), "CP_SDK._Resources.Heart.png")
            ));

            return l_Material;
        }
        /// <summary>
        /// Create URP Material
        /// </summary>
        /// <returns></returns>
        private static Material CreateURPMaterial()
        {
            var l_Material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            l_Material.EnableKeyword("_ALPHATEST_ON");
            l_Material.EnableKeyword("_COLOROVERLAY_ON");
            l_Material.EnableKeyword("_RECEIVE_SHADOWS_OFF");
            l_Material.SetOverrideTag("RenderType", "TransparentCutout");
            l_Material.SetFloat("_AlphaClip",                       1.00f);
            l_Material.SetFloat("_Blend",                           0.00f);
            l_Material.SetFloat("_BlendOp",                         0.00f);
            l_Material.SetFloat("_BumpScale",                       1.00f);
            l_Material.SetFloat("_CameraFadingEnabled",             0.00f);
            l_Material.SetFloat("_CameraFarFadeDistance",           2.00f);
            l_Material.SetFloat("_CameraNearFadeDistance",          1.00f);
            l_Material.SetFloat("_ColorMode",                       0.00f);
            l_Material.SetFloat("_Cull",                            0.00f);
            l_Material.SetFloat("_Cutoff",                          0.50f);
            l_Material.SetFloat("_DetailNormalMapScale",            1.00f);
            l_Material.SetFloat("_DistortionBlend",                 0.50f);
            l_Material.SetFloat("_DistortionEnabled",               0.00f);
            l_Material.SetFloat("_DistortionStrength",              1.00f);
            l_Material.SetFloat("_DistortionStrengthScaled",        0.10f);
            l_Material.SetFloat("_DstBlend",                       10.00f);
            l_Material.SetFloat("_EmissionEnabled",                 0.00f);
            l_Material.SetFloat("_EnableExternalAlpha",             0.00f);
            l_Material.SetFloat("_FlipbookBlending",                0.00f);
            l_Material.SetFloat("_FlipbookMode",                    0.00f);
            l_Material.SetFloat("_GlossMapScale",                   1.00f);
            l_Material.SetFloat("_Glossiness",                      1.00f);
            l_Material.SetFloat("_GlossyReflections",               0.00f);
            l_Material.SetFloat("_InvFade",                         1.15f);
            l_Material.SetFloat("_LightingEnabled",                 0.00f);
            l_Material.SetFloat("_Metallic",                        0.00f);
            l_Material.SetFloat("_Mode",                            2.00f);

            l_Material.SetFloat("_QueueOffset",                     0.00f);
            l_Material.SetFloat("_ReceiveShadows",                  1.00f);
            l_Material.SetFloat("_Smoothness",                      0.50f);
            l_Material.SetFloat("_SoftParticlesEnabled",            0.00f);
            l_Material.SetFloat("_SoftParticlesFarFadeDistance",    1.00f);
            l_Material.SetFloat("_SoftParticlesNearFadeDistance",   0.00f);
            l_Material.SetFloat("_SrcBlend",                        5.00f);
            l_Material.SetFloat("_Surface",                         1.00f);
            l_Material.SetFloat("_ZWrite",                          0.00f);
            l_Material.SetColor("_EmissionColor",                   new Color(0, 0, 0, 0));
            l_Material.SetColor("_BaseColor",                       new Color(0.8f, 0.8f, 0.8f, 1.0f));
            l_Material.renderQueue      = 3000;
            l_Material.enableInstancing = true;

            SetMaterialTexture(l_Material, Texture2DU.CreateFromRaw(
                Misc.Resources.FromRelPath(Assembly.GetExecutingAssembly(), "CP_SDK._Resources.Heart.png")
            ));

            return l_Material;
        }
    }
}
