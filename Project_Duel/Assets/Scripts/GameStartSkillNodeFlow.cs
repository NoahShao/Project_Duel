using System;
using System.Collections.Generic;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 游戏开始「同节点」技能：按先后手分侧；己方仅一条强制技时自动播横幅+战报，多条强制技需点击且顺序自选；非强制技可点击或「结束当前节点」放弃；敌方由程序自动播完。
    /// </summary>
    public struct GameStartSkillLineEntry
    {
        public bool SideIsPlayer;
        public int GeneralIndex;
        public int SkillIndex;
        public string FlowLine;
        public string RoleDisplayName;
        public string SkillDisplayName;
        /// <summary>规则 Value1：恢复的士气点数。</summary>
        public int MoraleGain;
        /// <summary>规则 Value2：增加的士气上限。</summary>
        public int MoraleCapGain;

        public bool IsMandatorySkill(BattleState state)
        {
            if (state == null)
                return false;
            var side = state.GetSide(SideIsPlayer);
            if (GeneralIndex < 0 || GeneralIndex >= side.GeneralCardIds.Count)
                return false;

            string cid = side.GeneralCardIds[GeneralIndex] ?? string.Empty;
            if (SkillRuleLoader.HasTag(cid, SkillIndex, "\u5f3a\u5236\u6280"))
                return true;

            var data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(cid));
            if (data == null)
                return false;

            List<string> tags = SkillIndex switch
            {
                0 => data.SkillTags1,
                1 => data.SkillTags2,
                2 => data.SkillTags3,
                _ => null
            };

            if (tags == null)
                return false;

            for (int i = 0; i < tags.Count; i++)
            {
                if (string.Equals(tags[i], "\u5f3a\u5236\u6280", StringComparison.Ordinal))
                    return true;
            }

            return false;
        }
    }

    public static class GameStartSkillNodeFlow
    {
        public const string StartGameEffectIdForLog = "start_game_gain_morale_and_max";

        private static string GetSkillNameFromCard(CardData data, int skillIndex)
        {
            return skillIndex switch
            {
                0 => data?.SkillName1 ?? string.Empty,
                1 => data?.SkillName2 ?? string.Empty,
                2 => data?.SkillName3 ?? string.Empty,
                _ => string.Empty
            };
        }

        public static List<GameStartSkillLineEntry> BuildSortedEntries(BattleState state)
        {
            var entries = new List<GameStartSkillLineEntry>();
            if (state == null)
                return entries;

            void CollectSide(bool sideIsPlayer)
            {
                string camp = sideIsPlayer ? "\u5df1\u65b9" : "\u654c\u65b9";
                SideState side = state.GetSide(sideIsPlayer);
                for (int gi = 0; gi < side.GeneralCardIds.Count; gi++)
                {
                    if (!side.IsGeneralFaceUp(gi))
                        continue;

                    string cid = side.GeneralCardIds[gi] ?? string.Empty;
                    CardData data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(cid));
                    string role = data != null && !string.IsNullOrWhiteSpace(data.RoleName) ? data.RoleName : cid;
                    for (int sk = 0; sk < 3; sk++)
                    {
                        SkillRuleEntry rule = SkillRuleLoader.GetRule(cid, sk);
                        if (rule == null || !string.Equals(rule.EffectId, StartGameEffectIdForLog, StringComparison.Ordinal))
                            continue;

                        string skName = !string.IsNullOrWhiteSpace(rule.SkillName)
                            ? rule.SkillName
                            : GetSkillNameFromCard(data, sk);
                        if (string.IsNullOrWhiteSpace(skName))
                            skName = "\u6280\u80fd";

                        int v1 = Mathf.Max(1, rule.Value1);
                        int v2 = Mathf.Max(1, rule.Value2);
                        string flowLine =
                            "\u3010\u5168\u5c40\u3011\u6e38\u620f\u5f00\u59cb\u65f6\uff0c" + camp + "\u89d2\u8272\u3010" + role + "\u3011\u7684\u6280\u80fd\u3010" + skName + "\u3011\u751f\u6548\uff0c\u58eb\u6c14\u4e0a\u9650\u589e\u52a0" + v2 + "\u70b9\u3001\u6062\u590d" + v1 + "\u70b9\u58eb\u6c14\u3002";
                        entries.Add(new GameStartSkillLineEntry
                        {
                            SideIsPlayer = sideIsPlayer,
                            GeneralIndex = gi,
                            SkillIndex = sk,
                            FlowLine = flowLine,
                            RoleDisplayName = role,
                            SkillDisplayName = skName,
                            MoraleGain = v1,
                            MoraleCapGain = v2,
                        });
                    }
                }
            }

            bool initSide = state.InitiativeSideIsPlayer;
            CollectSide(initSide);
            CollectSide(!initSide);

            bool sortInit = state.InitiativeSideIsPlayer;
            entries.Sort((a, b) =>
            {
                int oa = a.SideIsPlayer == sortInit ? 0 : 1;
                int ob = b.SideIsPlayer == sortInit ? 0 : 1;
                int c = oa.CompareTo(ob);
                if (c != 0)
                    return c;
                c = a.GeneralIndex.CompareTo(b.GeneralIndex);
                if (c != 0)
                    return c;
                return a.SkillIndex.CompareTo(b.SkillIndex);
            });

            return entries;
        }

        /// <summary>技能横幅副标题：同时展示士气上限与当前士气恢复。</summary>
        public static string FormatGameStartMoraleSkillBannerSubtext(in GameStartSkillLineEntry e)
        {
            return "\u58eb\u6c14\u4e0a\u9650+" + e.MoraleCapGain + "\uff0c\u6062\u590d" + e.MoraleGain + "\u70b9\u58eb\u6c14";
        }

        private static List<List<GameStartSkillLineEntry>> SplitBySideSegments(List<GameStartSkillLineEntry> entries)
        {
            var result = new List<List<GameStartSkillLineEntry>>();
            List<GameStartSkillLineEntry> cur = null;
            bool? lastSide = null;
            for (int i = 0; i < entries.Count; i++)
            {
                GameStartSkillLineEntry e = entries[i];
                if (lastSide != e.SideIsPlayer)
                {
                    cur = new List<GameStartSkillLineEntry>();
                    result.Add(cur);
                    lastSide = e.SideIsPlayer;
                }

                cur.Add(e);
            }

            return result;
        }

        public static void RunSequence(List<GameStartSkillLineEntry> entries, Action done)
        {
            if (entries == null || entries.Count == 0)
            {
                done?.Invoke();
                return;
            }

            List<List<GameStartSkillLineEntry>> segments = SplitBySideSegments(entries);
            RunSegmentIndex(segments, 0, done);
        }

        private static void RunSegmentIndex(List<List<GameStartSkillLineEntry>> segments, int segIdx, Action done)
        {
            if (segIdx >= segments.Count)
            {
                done?.Invoke();
                return;
            }

            List<GameStartSkillLineEntry> segment = segments[segIdx];
            BattleState st = BattlePhaseManager.GetState();
            var mandatory = new List<GameStartSkillLineEntry>();
            var optional = new List<GameStartSkillLineEntry>();
            for (int i = 0; i < segment.Count; i++)
            {
                GameStartSkillLineEntry e = segment[i];
                if (st != null && e.IsMandatorySkill(st))
                    mandatory.Add(e);
                else
                    optional.Add(e);
            }

            mandatory.Sort((a, b) =>
            {
                int c = a.GeneralIndex.CompareTo(b.GeneralIndex);
                return c != 0 ? c : a.SkillIndex.CompareTo(b.SkillIndex);
            });
            optional.Sort((a, b) =>
            {
                int c = a.GeneralIndex.CompareTo(b.GeneralIndex);
                return c != 0 ? c : a.SkillIndex.CompareTo(b.SkillIndex);
            });

            bool playerSide = segment[0].SideIsPlayer;
            if (!playerSide)
            {
                RunBannerLogChain(mandatory, 0, () =>
                {
                    if (optional.Count == 0)
                        RunSegmentIndex(segments, segIdx + 1, done);
                    else
                        RunBannerLogChain(optional, 0, () => RunSegmentIndex(segments, segIdx + 1, done));
                });
                return;
            }

            if (mandatory.Count == 0 && optional.Count == 0)
            {
                RunSegmentIndex(segments, segIdx + 1, done);
                return;
            }

            // \u4ec5\u4e00\u6761\u5f3a\u5236\u6280\uff1a\u81ea\u52a8\u64ad\u5e4c\u5e45+\u6218\u62a5\u7ed3\u7b97\uff1b\u591a\u6761\u5f3a\u5236\u6216\u975e\u5f3a\u5236\u4ecd\u9700\u70b9\u51fb/\u7ed3\u675f\u8282\u70b9
            if (mandatory.Count == 1)
            {
                RunBannerLogChain(mandatory, 0, () =>
                {
                    if (optional.Count == 0)
                        RunSegmentIndex(segments, segIdx + 1, done);
                    else
                        GameUI.BeginGameStartPassiveNode(null, optional, () => RunSegmentIndex(segments, segIdx + 1, done));
                });
                return;
            }

            GameUI.BeginGameStartPassiveNode(mandatory, optional, () => RunSegmentIndex(segments, segIdx + 1, done));
        }

        private static void RunBannerLogChain(List<GameStartSkillLineEntry> list, int i, Action onDone)
        {
            if (i >= list.Count)
            {
                onDone?.Invoke();
                return;
            }

            GameStartSkillLineEntry e = list[i];
            SkillEffectBanner.Show(
                e.SideIsPlayer,
                false,
                e.RoleDisplayName,
                e.SkillDisplayName,
                FormatGameStartMoraleSkillBannerSubtext(e),
                () => BattleFlowPacing.AddLogThenContinue(e.FlowLine, () =>
                {
                    BattleState st = BattlePhaseManager.GetState();
                    if (st != null)
                        OfflineSkillEngine.ApplyResolvedGameStartMoraleSkill(st, e);
                    GameUI.NotifyPhaseChanged();
                    RunBannerLogChain(list, i + 1, onDone);
                }));
        }
    }
}
