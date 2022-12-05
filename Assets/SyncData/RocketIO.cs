using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BestHTTP.SocketIO;
using BestHTTP.SocketIO.Transports;
using System;
using UniRx;
using Newtonsoft.Json;
using UnityEditor.PackageManager.Requests;
using UnityEditor;

public class RocketIO : SingletonClass<RocketIO>, IService
{
    private static string TAG = typeof(RocketIO).Name;

    private SocketManager socketManager;
    private static Socket socket;

    private const string server = "http://127.0.0.1:8000/socket.io/";
    private const string server_test = "http://127.0.0.1:8000/socket.io/";


    public ConnectState connectState = ConnectState.INITIAL;

    public enum ConnectState
    {
        INITIAL,
        ACTION_CONNECTED,
        ACTION_DISCONNECTED,
        ACTION_RECONNECTED,
        ACTION_RESCONNECTING,
    }

    public enum Server
    {
        Public,
        Test
    }

    public string GetServer(Server type)
    {
        switch(type)
        {
            case Server.Public:
                {
                    return server;
                }
            case Server.Test:
                {
                    return server_test;
                }
            default:
                return server;
        }
    }
    public enum ELoginState
    {
        LOGINED,
        LOGOUT,
        BANNED
    }

    public static ELoginState LoginState { get; private set; } = ELoginState.LOGOUT;

    public static bool IsLogined
    {
        get
        {
            return LoginState == ELoginState.LOGINED;
        }
    }

    public static bool IntenetAvaiable
    {
        get { return Application.internetReachability != NetworkReachability.NotReachable; }
    }

    public void Init()
    {

        LoginState = ELoginState.LOGOUT;

        Connect(GetServer(Server.Test));

    }

    public void Connect(string serverUri)
    {
        if (socketManager == null || !socketManager.GetSocket().IsOpen)
        {
            DebugCustom.LogColor(TAG, "ConnectSocketIO: ", serverUri);

            SocketOptions options = new SocketOptions();
            options.AutoConnect = true;
            options.Reconnection = true;
            options.ConnectWith = TransportTypes.WebSocket;
            options.Timeout = TimeSpan.FromMilliseconds(60000);
            socketManager = new SocketManager(new Uri(serverUri), options);
            socketManager.Encoder = new JsonDotNetEncoder();
            socket = socketManager.GetSocket();
            DebugCustom.LogColor(TAG, "-----Test Connect socket ----------: ", serverUri);

            socket.On("connect", OnConnect);
            socket.On("disconnect", OnDisconnect);
            socket.On("reconnecting", OnReconnecting);
            socket.On("reconnect", OnReconnect);

            socket.On("connect_error", (socket, packet, args) =>
            {
                Debug.LogError("connect_error");
            });
            socket.On("connect_timeout", (socket, packet, args) =>
            {
                Debug.LogError("connect_timeout");
            });
        }
        socketManager.Open();

    }
    private static Subject<Unit> _ConnectSubject = new Subject<Unit>();
    private static Subject<Unit> _loginAsyncSubject = new Subject<Unit>();

    private void OnReconnecting(Socket socket, Packet packet, params object[] args)
    {
        DebugCustom.Log(TAG, "OnReconnecting");
        connectState = ConnectState.ACTION_RESCONNECTING;
    }


    private void OnConnect(Socket socket, Packet package, object[] args)
    {
        DebugCustom.LogColor(TAG, "OnConnected");
        connectState = ConnectState.ACTION_CONNECTED;
        SendLoginRequest().CatchIgnore().Subscribe();
        _ConnectSubject.OnNext(Unit.Default);
    }

    public bool Logining = false;

    public IObservable<Unit> SendLoginRequest(bool showLoading = false)
    {
        if (IsLogined)
        {
            return Observable.Return(Unit.Default);
        } else
        {
          if(Logining)
            {
                return _loginAsyncSubject;

            } else
            {
                _loginAsyncSubject = new Subject<Unit>(); 
            }

            Logining = true;

            Connected
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(6))
                .Subscribe(_ =>
                {
                    if (LoginState == ELoginState.BANNED)
                    {
                        Logining = false;
                    }
                    else
                    {
                        SendMessageP(LoginRequest, (e) =>
                        {
                            OnLoginSuccess(e);
                            Logining = false;
                        }, (exception) =>
                        {
                            Logining = false;
                        });
                    }
                }, ex =>
                {
                    MessageError error = new MessageError
                    {
                        ErrorMessage = "ServerDown"
                    };
                    Logining = false;

                });
            return _loginAsyncSubject;
        }
    }

    public void OnLoginSuccess(MessageResponse msg)
    {
        DebugCustom.LogColor("OnLoginSucess: ");
        PersionModel profile = JsonConvert.DeserializeObject<PersionModel>(msg.Body.ToString());
        LoginState = ELoginState.LOGINED; 
    }

    private void SendMessageP(MessageRequest messageOut, Action<MessageResponse> SuccessCallback, Action<MessageError> ErrorCallback = null)
    {
        DebugCustom.LogColor("------------ send login request ---------: ", JsonUtility.ToJson(messageOut, true));
        if (!IntenetAvaiable)
        {
            if (ErrorCallback != null)
            {
                var err = new MessageError() { ErrorMessage = "No internet connection" };
                ErrorCallback(err);
            }
            return;
        }
        MessageData messageData = new MessageData(messageOut.Name, messageOut);
        DebugCustom.LogColor("------------ pass msg data ---------: ", JsonUtility.ToJson(messageData, true));

        SendMsg(messageData, SuccessCallback, ErrorCallback);
    }


    private void SendMsg(MessageData messageData, Action<MessageResponse> SuccessCallback, Action<MessageError> ErrorCallback = null)
    {
        try
        {
            DebugCustom.LogColor("------------ send msg 1 ---------: ", JsonUtility.ToJson(messageData, true));
            
            socket.Emit("msg", (socket, packet, args) =>
            {
                MessageResponse mesageData = JsonConvert.DeserializeObject<MessageResponse>(args[0].ToString());
                DebugCustom.LogColorJson("SendMsg", mesageData);

                if (mesageData != null)
                {
                    if (mesageData.IsSuccess)
                    {
                        if (SuccessCallback != null)
                        {
                            SuccessCallback(mesageData);
                        }
                    }
                    else
                    {
                        if (ErrorCallback != null)
                        {
                            ErrorCallback(mesageData.Error);
                        }
                    }
                }
                else
                {
                    if (ErrorCallback != null)
                        ErrorCallback(new MessageError());
                }
            }, messageData);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }
    }


    private void OnReconnect(Socket socket, Packet packet, object[] args)
    {
        DebugCustom.Log(TAG, "OnReconnect");
    }

    private void OnDisconnect(Socket socket, Packet packet, object[] args)
    {
        DebugCustom.LogColorJson(TAG, "OnDisconnect", args, packet);
        connectState = ConnectState.ACTION_DISCONNECTED;
    }

    //public static void OnDestroy()
    //{
    //    DebugCustom.Log(TAG, "OnDestroy");
    //    if (socketManager != null)
    //    {
    //        socketManager.Close();
    //    }
    //}


    public IObservable<Unit> Connected
    {
        get
        {
            if (connectState == ConnectState.ACTION_CONNECTED)
            {
                return Observable.Return(Unit.Default);
            }
            else
            {
                return _ConnectSubject;
            }
        }
    }



    public static MobieLoginRequest LoginRequest
    {
        get
        {
            MobieLoginRequest request = new MobieLoginRequest();
            request.OS = SystemInfo.operatingSystem;
#if UNITY_EDITOR
            request.Platform = 2;
#elif UNITY_IOS
                request.Platform = 1;
#else
                request.Platform = 0;
#endif
            request.DeviceId = RocketUtils.GetDeviceId();
            request.DeviceModel = SystemInfo.deviceModel;
            request.AppVersion = DragonConfig.versionCode;

         //   DebugCustom.Log(TAG, "message request example", JsonUtility.ToJson(request, true));
            return request;
        }
    }

}