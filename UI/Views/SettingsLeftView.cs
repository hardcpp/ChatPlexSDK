using CP_SDK.XUI;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

namespace CP_SDK.UI.Views
{
    /// <summary>
    /// Settings left view controller
    /// </summary>
    public sealed class SettingsLeftView : ViewController<SettingsLeftView>
    {
        private XUIText             m_StatusText;
        private XUIText             m_SubscriptionText;
        private XUIPrimaryButton    m_PrimaryButton;
        private XUISecondaryButton  m_SecondaryButton;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private bool m_IsLinking = false;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On view creation
        /// </summary>
        protected override sealed void OnViewCreation()
        {
            var l_Bytes     = Misc.Resources.FromRelPath(Assembly.GetExecutingAssembly(), "CP_SDK._Resources.ChatPlexLogoTransparent.png");
            var l_Sprite    = Unity.SpriteU.CreateFromRaw(l_Bytes);

            Templates.FullRectLayout(
                Templates.TitleBar("ChatPlex Account"),

                XUIPrimaryButton.Make("")
                    .SetBackgroundSprite(null)
                    .SetIconSprite(l_Sprite)
                    .SetWidth(52)
                    .SetHeight(52),

                XUIText.Make("Not connected")
                    .Bind(ref m_StatusText),

                XUIText.Make(" ")
                    .Bind(ref m_SubscriptionText),

                XUIVLayout.Make(
                    XUIPrimaryButton.Make("Connect", OnPrimaryButtonPressed)
                        .Bind(ref m_PrimaryButton),
                    XUISecondaryButton.Make("Disconnect", OnSecondaryButtonPressed)
                        .Bind(ref m_SecondaryButton)
                )
                .SetWidth(60f)
                .SetPadding(0)
                .ForEachDirect<XUIPrimaryButton>(y =>
                {
                    y.SetHeight(8f);
                    y.OnReady((x) => x.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained);
                })
                .ForEachDirect<XUISecondaryButton>(y =>
                {
                    y.SetHeight(8f);
                    y.OnReady((x) => x.CSizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained);
                })
            )
            .SetBackground(true, null, true)
            .BuildUI(transform);

            ChatPlexService.StateChanged += ChatPlexService_StateChanged;

            ChatPlexService_StateChanged(ChatPlexService.State, ChatPlexService.State);
        }
        /// <summary>
        /// On view deactivation
        /// </summary>
        protected sealed override void OnViewDeactivation()
        {
            ChatPlexService.StateChanged -= ChatPlexService_StateChanged;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On primary button pressed
        /// </summary>
        private void OnPrimaryButtonPressed()
        {
            if (ChatPlexService.State == ChatPlexService.EState.Disconnected)
            {
                m_IsLinking = true;
                ChatPlexService.StartLinking();
                ShowLoadingModal("Loading...", true, OnLoadingCancel);
            }
            else if (ChatPlexService.State == ChatPlexService.EState.Error || ChatPlexService.State == ChatPlexService.EState.Connected)
            {
                ChatPlexService.Refresh();
            }
        }
        /// <summary>
        /// On secondary button pressed
        /// </summary>
        private void OnSecondaryButtonPressed()
        {
            ChatPlexService.Disconnect();
        }
        /// <summary>
        /// On Loading cancel
        /// </summary>
        private void OnLoadingCancel()
        {
            if (ChatPlexService.State == ChatPlexService.EState.LinkRequest || ChatPlexService.State == ChatPlexService.EState.LinkWait)
            {
                m_IsLinking = false;
                ChatPlexService.StopLinking();
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// On ChatPlex service state change
        /// </summary>
        /// <param name="oldState">Old state</param>
        /// <param name="newState">New state</param>
        private void ChatPlexService_StateChanged(ChatPlexService.EState oldState, ChatPlexService.EState newState)
        {
            Unity.MTMainThreadInvoker.Enqueue(() =>
            {
                if (m_IsLinking)
                {
                    if (newState == ChatPlexService.EState.LinkRequest)
                        ShowLoadingModal("Creating link request...", true, OnLoadingCancel);
                    else if (newState == ChatPlexService.EState.LinkWait)
                        ShowLoadingModal($"Go to https://chatplex.org/link and the input following code\n{ChatPlexService.LinkCode}", true, OnLoadingCancel);
                    else if (newState == ChatPlexService.EState.Error)
                    {
                        m_IsLinking = false;

                        CloseLoadingModal();
                        ShowMessageModal("Error: " + ChatPlexService.LastError);
                    }
                    else
                    {
                        m_IsLinking = false;
                        CloseLoadingModal();
                    }
                }

                switch (newState)
                {
                    case ChatPlexService.EState.Disconnected:
                        m_StatusText.SetColor(Color.red);
                        m_StatusText.SetText("Disconected!");
                        m_PrimaryButton.SetInteractable(true);
                        m_PrimaryButton.SetText("Connect");
                        m_SecondaryButton.SetInteractable(false);
                        break;

                    case ChatPlexService.EState.Error:
                        m_StatusText.SetColor(Color.red);
                        m_StatusText.SetText("Disconected, error!");
                        m_PrimaryButton.SetInteractable(true);
                        m_PrimaryButton.SetText("Connect");
                        m_SecondaryButton.SetInteractable(false);
                        break;

                    case ChatPlexService.EState.Connecting:
                        m_StatusText.SetColor(Color.blue);
                        m_StatusText.SetText("Connecting...");
                        m_PrimaryButton.SetInteractable(false);
                        m_PrimaryButton.SetText("Connect");
                        m_SecondaryButton.SetInteractable(false);
                        break;

                    case ChatPlexService.EState.LinkRequest:
                    case ChatPlexService.EState.LinkWait:
                        m_StatusText.SetColor(Color.blue);
                        m_StatusText.SetText("Linking account...");
                        m_PrimaryButton.SetInteractable(false);
                        m_PrimaryButton.SetText("Connect");
                        m_SecondaryButton.SetInteractable(false);
                        break;

                    case ChatPlexService.EState.Connected:
                        m_StatusText.SetColor(Color.green);
                        m_StatusText.SetText("Connected!");
                        m_PrimaryButton.SetInteractable(true);
                        m_PrimaryButton.SetText("Refresh");
                        m_SecondaryButton.SetInteractable(true);
                        break;
                }

                m_SubscriptionText.SetText(ChatPlexService.ActiveSubscription);
            });
        }
    }
}
