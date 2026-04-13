using UnityEngine;
using UnityEngine.EventSystems;

namespace JunzhenDuijue
{
    /// <summary>
    /// 游戏启动入口，创建主界面与图鉴界面。
    /// </summary>
    public static class GameBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OnGameLoad()
        {
            try
            {
                RuntimeTraceLogger.MarkSessionStart("GameBootstrap.OnGameLoad");
                EnsureEventSystem();
                CardTableLoader.Load();
                IntroLoader.Load();
                MainMenuUI.Create();
                CompendiumUI.Create();
                DeckSelectUI.Create();
                OnlineLobbyUI.Create();
                MainMenuUI.Show();
                CompendiumUI.Hide();
            }
            catch (System.Exception e)
            {
                RuntimeTraceLogger.Exception("GameBootstrap.OnGameLoad", e);
                Debug.LogException(e);
            }
        }

        private static void EnsureEventSystem()
        {
            if (Object.FindObjectOfType<EventSystem>() == null)
            {
                var go = new GameObject("EventSystem");
                go.AddComponent<EventSystem>();
                go.AddComponent<StandaloneInputModule>();
            }
        }
    }
}
