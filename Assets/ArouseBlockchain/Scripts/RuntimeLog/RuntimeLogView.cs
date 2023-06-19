using UnityEngine;
using UnityEngine.UI;

namespace ArouseBlockchain.Common
{
    public class RuntimeLogView : MonoBehaviour
    {
        public GameObject LogsContainer;
        public GameObject LogPrefab;

        private static RuntimeLogView _instance = null;

        void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
            }
        }
    
        void Start()
        {
            Application.logMessageReceived += SpawnLogs;
        }

        private void OnApplicationQuit()
        {
            Application.logMessageReceived -= SpawnLogs;
        }

        public static void SpawnLogs(string text)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var log = Instantiate(_instance.LogPrefab, _instance.LogsContainer.transform);
                log.GetComponent<Text>().text = "> " + text;
            });
        }

        private void SpawnLogs(string condition, string stacktrace, LogType type)
        {
            // Enqueue To Main Thread For UI Component Visualization
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                var log = Instantiate(LogPrefab, LogsContainer.transform);
                log.GetComponent<Text>().text = "> " + condition;
            });
        }
    }
}
