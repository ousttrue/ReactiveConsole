using System;
using System.Collections;
using UniRx.Diagnostics;
using UnityEngine;


namespace ReactiveConsole
{
    public class WSConsoleSample : MonoBehaviour
    {
        [SerializeField]
        int m_port = 80;

        [SerializeField]
        TextAsset m_html;
        [SerializeField]
        TextAsset m_js;
        [SerializeField]
        TextAsset m_css;

        [SerializeField]
        float m_interval = 5.0f;

        WSConsole m_console;

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

#if UNITY_EDITOR
            // こっちは元ファイル変えてF5でリロードできる
            // host directory for reload
            dispatcher.Solvers.Add(new FolderMounter("/",
                UnityPath.FromAsset(m_html).Parent.FullPath,
                FolderMounter.JsTxtFilter));
#else
            dispatcher.Solvers.Add(new FileMounter("/index.html", m_html.bytes));
            dispatcher.Solvers.Add(new FileMounter("/wsconsole.js", m_js.bytes));
            dispatcher.Solvers.Add(new FileMounter("/wsconsole.css", m_css.bytes));
#endif

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
            var f = new SimpleJsonFormatter();
            m_console = new WSConsole(m_port, m_http, f);
        }

        private void OnDisable()
        {
            if (m_console != null)
            {
                m_console.Dispose();
                m_console = null;
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
