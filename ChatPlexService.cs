using CP_SDK.Network;
using CP_SDK.Unity;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using UnityEngine;

namespace CP_SDK
{
    /// <summary>
    /// ChatPlexService
    /// </summary>
    public static class ChatPlexService
    {
        public enum EState
        {
            Disconnected,
            LinkRequest,
            LinkWait,
            Connecting,

            Connected,

            Error
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        private static bool                     m_ThreadCondition = true;
        private static Thread                   m_Thread;
        private static EState                   m_State = EState.Disconnected;
        private static WebClientCore            m_WebClientCore;
        private static JsonRPCClient            m_JsonRPCClient;
        private static string                   m_LinkRequestID;
        private static string                   m_LinkCode;
        private static string                   m_LastError;
        private static string                   m_ActiveSubscription;
        private static string[]                 m_UnlockedFeatures;
        private static ConcurrentQueue<Action>  m_OnTokenReadyQueue = new ConcurrentQueue<Action>();
        private static string                   m_DeviceName;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        public static EState    State               => m_State;
        public static string    Token               => ChatPlexServiceConfig.Instance.Token;
        public static string    LinkCode            => m_LinkCode;
        public static string    LastError           => m_LastError;
        public static string    ActiveSubscription  => m_State == EState.Connected ? m_ActiveSubscription : string.Empty;
        public static string[]  UnlockedFeatures    => m_UnlockedFeatures;

        public static event Action<EState, EState>  StateChanged;

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Init the service
        /// </summary>
        internal static void Init()
        {
            m_WebClientCore = new WebClientCore("https://api.chatplex.org/", TimeSpan.FromSeconds(10), false, true);
            m_JsonRPCClient = new JsonRPCClient(m_WebClientCore);

            m_Thread = new Thread(ThreadRunner);
            m_Thread.Start();

            m_DeviceName = SystemInfo.deviceName;
        }
        /// <summary>
        /// Release the service
        /// </summary>
        internal static void Release()
        {
            m_ThreadCondition = false;
            m_Thread.Join();
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Add a callback to be called when the token is ready (call immediatly if ready)
        /// </summary>
        /// <param name="action">Callback to be caled</param>
        public static void OnTokenReady(Action action)
        {
            if (m_State == EState.Connected)
                action();
            else
                m_OnTokenReadyQueue.Enqueue(action);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Start linking procedure
        /// </summary>
        internal static void StartLinking()
        {
            if (m_State != EState.Disconnected)
                return;

            ChangeState(EState.LinkRequest);
        }
        /// <summary>
        /// Stop linking procedure
        /// </summary>
        internal static void StopLinking()
        {
            if (m_State != EState.LinkRequest && m_State != EState.LinkWait)
                return;

            ChangeState(EState.Disconnected);
        }
        /// <summary>
        /// Refresh the session
        /// </summary>
        internal static void Refresh()
        {
            ChangeState(EState.Disconnected);
        }
        /// <summary>
        /// Disconnect and erase the saved connected application token
        /// </summary>
        internal static void Disconnect()
        {
            ChatPlexServiceConfig.Instance.Token = string.Empty;
            ChatPlexServiceConfig.Instance.Save();

            ChangeState(EState.Disconnected);
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Change state and notify listenners
        /// </summary>
        /// <param name="newState"></param>
        private static void ChangeState(EState newState)
        {
            var l_OldState = m_State;
            m_State = newState;

            MTThreadInvoker.EnqueueOnThread(() => StateChanged?.Invoke(l_OldState, newState));
        }
        /// <summary>
        /// Fire on token ready actions
        /// </summary>
        private static void FireOnTokenReady()
        {
            while (m_OnTokenReadyQueue.TryDequeue(out var l_Action))
            {
                try
                {
                    l_Action();
                }
                catch (Exception l_Exception)
                {
                    ChatPlexSDK.Logger.Error("[CP_SDK][ChatPlexService.FireOnTokenReady] Error:");
                    ChatPlexSDK.Logger.Error(l_Exception);
                }
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Thread function
        /// </summary>
        private static void ThreadRunner()
        {
            while (m_ThreadCondition)
            {
                if (m_State == EState.Disconnected && !string.IsNullOrEmpty(ChatPlexServiceConfig.Instance.Token))
                {
                    m_WebClientCore.RemoveHeader("Authorization");

                    ChangeState(EState.Connecting);

                    var l_Result = m_JsonRPCClient.Request(
                        "Account_AuthByConnectedApplicationToken",
                        new JObject()
                        {
                            ["ConnectedApplicationToken"] = ChatPlexServiceConfig.Instance.Token
                        }
                    );

                    if (IsRPCSuccess(l_Result))
                        OnAuthed(l_Result);
                    else
                    {
                        if (l_Result.Result != null && l_Result.Result.ContainsKey("Result") && l_Result.Result["Result"].Value<bool>() == false)
                        {
                            ChatPlexServiceConfig.Instance.Token = string.Empty;
                            ChatPlexServiceConfig.Instance.Save();

                            ChangeState(EState.Disconnected);
                        }
                        else
                            OnError(l_Result);
                    }
                }
                else if (m_State == EState.LinkRequest)
                {
                    var l_Result = m_JsonRPCClient.Request(
                        "ConnectedApplication_CreateLinkRequest",
                        new JObject()
                        {
                            ["ApplicationIdentifier"] = ChatPlexSDK.ProductName,
                            ["ApplicationDeviceName"] = m_DeviceName
                        }
                    );

                    if (IsRPCSuccess(l_Result))
                    {
                        m_LinkRequestID = l_Result.Result["RequestID"].Value<string>();
                        m_LinkCode = l_Result.Result["Code"].Value<string>();

                        ChangeState(EState.LinkWait);
                    }
                    else
                        OnError(l_Result);
                }
                else if (m_State == EState.LinkWait)
                {
                    var l_Result = m_JsonRPCClient.Request(
                        "ConnectedApplication_GetLinkRequestStatus",
                        new JObject()
                        {
                            ["RequestID"] = m_LinkRequestID,
                        }
                    );

                    if (IsRPCSuccess(l_Result))
                    {
                        if (l_Result.Result["ResultToken"].Value<string>() != null)
                        {
                            ChatPlexServiceConfig.Instance.Token = l_Result.Result["ResultToken"].Value<string>();
                            ChatPlexServiceConfig.Instance.Save();

                            ChangeState(EState.Disconnected);
                        }
                    }
                    else
                        OnError(l_Result);
                }

                if (m_State == EState.LinkWait)
                    Thread.Sleep(1500);
                else
                    Thread.Sleep(100);
            }
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// Is RPC call result a success result?
        /// </summary>
        /// <param name="rpcResult">Result of the RPC command</param>
        /// <returns>True if success</returns>
        private static bool IsRPCSuccess(JsonRPCResult rpcResult)
        {
            return rpcResult.Result != null && rpcResult.Result.ContainsKey("Result") && rpcResult.Result["Result"].Value<bool>() == true;
        }

        ////////////////////////////////////////////////////////////////////////////
        ////////////////////////////////////////////////////////////////////////////

        /// <summary>
        /// When we are Authed
        /// </summary>
        /// <param name="rpcResult">Result of the RPC command</param>
        private static void OnAuthed(JsonRPCResult rpcResult)
        {
            m_ActiveSubscription    = rpcResult.Result["ActiveSubscription"]?.Value<string>() ?? string.Empty;
            m_UnlockedFeatures      = (rpcResult.Result["UnlockedFeatures"] as JArray).Values<string>().ToArray();

            m_WebClientCore.SetHeader("Authorization", $"ConnectedApplicationToken {ChatPlexServiceConfig.Instance.Token}");

            ChangeState(EState.Connected);
            FireOnTokenReady();
        }
        /// <summary>
        /// On error received
        /// </summary>
        /// <param name="rpcResult">Result of the RPC command</param>
        private static void OnError(JsonRPCResult rpcResult)
        {
            var l_Error = "Unknow server error!";
            if (rpcResult.Result != null && rpcResult.Result.ContainsKey("Error"))
                l_Error = rpcResult.Result["Error"].Value<string>();

            m_LastError = l_Error;
            ChangeState(EState.Error);
        }
    }
}
