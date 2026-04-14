using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 战报播放后的节奏：【全局】类战报暂停 1 秒；含「无事发生」的战报暂停 0.5 秒，再继续阶段逻辑。
    /// 使用真实时间等待，以便在技能横幅 <c>Time.timeScale=0</c> 时队列仍能推进。
    /// </summary>
    public static class BattleFlowPacing
    {
        public const float GlobalLinePauseSeconds = 1f;
        public const float NothingHappenedPauseSeconds = 0.5f;

        private static readonly Queue<(float delay, Action action)> s_queue = new Queue<(float, Action)>();
        private static GameObject s_host;
        private static MonoBehaviour s_runner;
        private static Coroutine s_processRoutine;

        private static void EnsureHost()
        {
            if (s_host != null)
                return;
            s_host = new GameObject("BattleFlowPacing");
            UnityEngine.Object.DontDestroyOnLoad(s_host);
            s_runner = s_host.AddComponent<PacingRunner>();
        }

        /// <summary>以行首【全局】或正文「无事发生」判定暂停时长；全局优先。</summary>
        public static float GetPauseSecondsForLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return 0f;
            string t = line.TrimStart();
            if (t.StartsWith("\u3010\u5168\u5c40\u3011", StringComparison.Ordinal))
                return GlobalLinePauseSeconds;
            if (line.IndexOf("\u65e0\u4e8b\u53d1\u751f", StringComparison.Ordinal) >= 0)
                return NothingHappenedPauseSeconds;
            return 0f;
        }

        /// <summary>写入战报；若需暂停则在暂停后执行 continuation，否则本帧立即执行。</summary>
        public static void AddLogThenContinue(string line, Action continuation)
        {
            BattleFlowLog.Add(line);
            float d = GetPauseSecondsForLine(line);
            if (d <= 0f)
            {
                if (ToastUI.IsSkillBannerTimeFreezeActive())
                {
                    EnsureHost();
                    s_runner.StartCoroutine(RunWhenSkillBannerUnfrozen(continuation));
                }
                else
                {
                    continuation?.Invoke();
                    BattlePhaseManager.TryOpponentAutoAdvanceAfterBattleFlowPacing();
                }

                return;
            }

            EnsureHost();
            s_queue.Enqueue((d, continuation ?? (() => { })));
            if (s_processRoutine == null)
                s_processRoutine = s_runner.StartCoroutine(ProcessQueue());
        }

        private static IEnumerator RunWhenSkillBannerUnfrozen(Action continuation)
        {
            while (ToastUI.IsSkillBannerTimeFreezeActive())
                yield return null;
            continuation?.Invoke();
            BattlePhaseManager.TryOpponentAutoAdvanceAfterBattleFlowPacing();
        }

        private static IEnumerator ProcessQueue()
        {
            try
            {
                while (s_queue.Count > 0)
                {
                    (float delay, Action act) = s_queue.Dequeue();
                    if (delay > 0f)
                        yield return new WaitForSecondsRealtime(delay);
                    while (ToastUI.IsSkillBannerTimeFreezeActive())
                        yield return null;
                    act?.Invoke();
                    BattlePhaseManager.TryOpponentAutoAdvanceAfterBattleFlowPacing();
                }
            }
            finally
            {
                s_processRoutine = null;
                if (s_queue.Count > 0 && s_runner != null)
                    s_processRoutine = s_runner.StartCoroutine(ProcessQueue());
            }
        }

        private sealed class PacingRunner : MonoBehaviour { }
    }
}
