using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CP_SDK.UI
{
    /// <summary>
    /// Loading progress bar
    /// </summary>
    public class LoadingProgressBar : Unity.PersistentSingleton<LoadingProgressBar>
    {
        private static readonly Vector3 POSITION            = new Vector3(0, 2.5f, 4.25f);
        private static readonly Vector3 ROTATION            = new Vector3(0, 0, 0);
        private static readonly Vector3 SCALE               = new Vector3(0.01f, 0.01f, 0.01f);
        private static readonly Vector2 CANVAS_SIZE         = new Vector2(150, 40);
        private static readonly Vector2 LOADING_BAR_SIZE    = new Vector2(100, 10);
        private static readonly Vector2 HEADER_POSITION     = new Vector2(0, 15);
        private static readonly Vector2 HEADER_SIZE         = new Vector2(100, 20);
        private static readonly Color   BACKGROUND_COLOR    = new Color(0, 0, 0, 0.8f);
        private const           float   HEADER_FONT_SIZE    = 10f;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private Components.CFloatingPanel   m_Canvas;
        private Components.CText            m_HeaderText;
        private Components.CImage           m_LoadingBackground;
        private Components.CImage           m_LoadingBar;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On component creation
        /// </summary>
        private void Awake()
        {
            m_Canvas = UISystem.FloatingPanelFactory.Create("", transform);
            m_Canvas.transform.localScale = SCALE;
            m_Canvas.SetTransformDirect(POSITION, ROTATION);
            m_Canvas.SetSize(CANVAS_SIZE);
            m_Canvas.SetBackground(false);

            m_HeaderText = UISystem.TextFactory.Create("", m_Canvas.RTransform);
            if (m_HeaderText)
            {
                m_HeaderText.RTransform.anchoredPosition    = HEADER_POSITION;
                m_HeaderText.RTransform.sizeDelta           = HEADER_SIZE;
                m_HeaderText.SetFontSize(HEADER_FONT_SIZE);
                m_HeaderText.SetAlign(TextAlignmentOptions.Midline);
                m_HeaderText.SetText("...");
            }

            m_LoadingBackground = UISystem.ImageFactory.Create("", m_Canvas.transform);
            m_LoadingBackground.SetWidth(LOADING_BAR_SIZE.x);
            m_LoadingBackground.SetHeight(LOADING_BAR_SIZE.y);
            m_LoadingBackground.SetSprite(Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * 0.5f, 100, 1));
            m_LoadingBackground.SetColor(BACKGROUND_COLOR);
            m_LoadingBackground.ImageC.preserveAspect = false;
            
            m_LoadingBar =  UISystem.ImageFactory.Create("", m_Canvas.transform);
            m_LoadingBar.SetWidth(LOADING_BAR_SIZE.x);
            m_LoadingBar.SetHeight(LOADING_BAR_SIZE.y);
            m_LoadingBar.SetSprite(Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height), Vector2.one * 0.5f, 100, 1));
            m_LoadingBar.SetType(Image.Type.Filled);
            m_LoadingBar.SetColor(new Color(0.1f, 1, 0.1f, 0.5f));
            m_LoadingBar.ImageC.fillMethod = Image.FillMethod.Horizontal;
            m_LoadingBar.ImageC.fillAmount = 0.5f;
            m_LoadingBar.ImageC.preserveAspect = false;

            m_Canvas.GetComponent<Canvas>().enabled = false;
            
            ChatPlexSDK.OnGenericSceneChange += ChatPlexSDK_OnGenericSceneChange;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Show a message with a hide timer
        /// </summary>
        /// <param name="p_Message">Message to display</param>
        /// <param name="p_Time">Time before disapearing</param>
        public void ShowTimedMessage(string p_Message, float p_Time)
        {
            StopAllCoroutines();

            if (m_HeaderText)
                m_HeaderText.SetText(p_Message);

            m_LoadingBar.ImageC.enabled             = false;
            m_LoadingBackground.ImageC.enabled      = false;
            m_LoadingBar.ImageC.fillAmount          = 0.0f;
            m_Canvas.GetComponent<Canvas>().enabled = true;

            StartCoroutine(Coroutine_DisableCanvas(p_Time));
        }
        /// <summary>
        /// Show loading progress bar with a message
        /// </summary>
        /// <param name="p_Message">Message to display</param>
        /// <param name="p_Progress">Current progress</param>
        public void ShowLoadingProgressBar(string p_Message, float p_Progress)
        {
            StopAllCoroutines();

            if (m_HeaderText)
                m_HeaderText.SetText(p_Message);

            m_LoadingBar.ImageC.enabled             = true;
            m_LoadingBar.ImageC.fillAmount          = p_Progress;
            m_LoadingBackground.ImageC.enabled      = true;
            m_Canvas.GetComponent<Canvas>().enabled = true;
        }
        /// <summary>
        /// Set current progress and displayed message
        /// </summary>
        /// <param name="p_Message">Displayed message</param>
        /// <param name="p_Progress">Loading progress</param>
        public void SetProgress(string p_Message, float p_Progress)
        {
            StopAllCoroutines();

            if (m_HeaderText)
                m_HeaderText.SetText(p_Message);

            m_LoadingBar.ImageC.fillAmount  = p_Progress;
            m_Canvas.GetComponent<Canvas>().enabled = true;
        }
        /// <summary>
        /// Set hide timer
        /// </summary>
        /// <param name="p_Time">Time in seconds</param>
        public void HideTimed(float p_Time)
        {
            StopAllCoroutines();
            StartCoroutine(Coroutine_DisableCanvas(p_Time));
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On scene changed
        /// </summary>
        /// <param name="p_NewScene">New scene type</param>
        private void ChatPlexSDK_OnGenericSceneChange(EGenericScene p_NewScene)
        {
            if (p_NewScene != EGenericScene.Menu)
            {
                StopAllCoroutines();
                m_Canvas.GetComponent<Canvas>().enabled = false;
            }
        }
        /// <summary>
        /// Timed canvas disabler
        /// </summary>
        /// <param name="p_Time">Time in seconds</param>
        /// <returns></returns>
        private IEnumerator Coroutine_DisableCanvas(float p_Time)
        {
            yield return new WaitForSecondsRealtime(p_Time);
            m_Canvas.GetComponent<Canvas>().enabled = false;
        }
    }
}
