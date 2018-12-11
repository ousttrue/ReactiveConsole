using System;
using System.Collections;
using UniRx.Diagnostics;
using UnityEngine;
using UniRx;
using System.IO;
using System.Collections.Generic;

namespace ReactiveConsole
{
    public class WSConsoleSample : MonoBehaviour
    {
        static readonly UniRx.Diagnostics.Logger Logger = new UniRx.Diagnostics.Logger("WSConsoleSample");

        [SerializeField]
        int m_port = 80;

        [SerializeField]
        List<AssetMount> m_mounts = new List<AssetMount>();

        [SerializeField]
        float m_interval = 5.0f;

        WSConsole m_console;

        private void Reset()
        {
            m_mounts.Add(new AssetMount("/index.html", "WSConsoleSample/index"));
            m_mounts.Add(new AssetMount("/wsconsole.css", "WSConsoleSample/wsconsole.css"));
            m_mounts.Add(new AssetMount("/wsconsole.js", "WSConsoleSample/wsconsole.js"));
        }

        private void Awake()
        {
            // UniRx logger to Unity console
            ObservableLogger.Listener.LogToUnityDebug();

            if (m_interval == 0)
            {
                m_interval = 5.0f;
            }
        }

        private DispatchSolver SetupHttpMount()
        {
            var dispatcher = new DispatchSolver();

            foreach(var m in m_mounts)
            {
                dispatcher.Solvers.Add(new FileMounter(m.MountPoint, m.Loader));
            }

            return dispatcher;
        }

        private void OnApplicationQuit()
        {
            Logging.Dispose();
        }

        private void OnEnable()
        {
            Logging.Info("listen " + m_port);
            var m_http = new HttpDispatcher(SetupHttpMount());

            m_console = new WSConsole(m_port, m_http);

            var utf8 = new System.Text.UTF8Encoding(false);
            m_disposable = Logging.Observable.Subscribe(x =>
            {
                try
                {
                    // LogEntry to Json
                    var json = UnityEngine.JsonUtility.ToJson(x);
                    m_console.SendFrame(WebSocketFrameOpCode.Text, new ArraySegment<byte>(utf8.GetBytes(json)));
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
        }

        IDisposable m_disposable;
        private void OnDisable()
        {
            if (m_console != null)
            {
                m_console.Dispose();
                m_console = null;
            }
            if (m_disposable != null)
            {
                m_disposable.Dispose();
                m_disposable = null;
            }
        }

        IEnumerator Start()
        {
            int count = 0;
            while (true)
            {
                yield return new WaitForSeconds(UnityEngine.Random.value * m_interval);
                switch (count++ % 4)
                {
                    case 0:
                        Logging.Debug("Debug");
                        break;
                    case 1:
                        Logging.Info("Info");
                        break;
                    case 2:
                        Logging.Warning("Warning");
                        break;
                    case 3:
                        try
                        {
                            throw new Exception();
                        }
                        catch(Exception ex)
                        {
                            Logging.Error(ex.ToString());
                        }
                        break;
                }
            }
        }
    }
}
