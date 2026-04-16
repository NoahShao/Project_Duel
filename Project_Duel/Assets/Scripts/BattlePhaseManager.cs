using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace JunzhenDuijue
{
    public static class BattlePhaseManager
    {
        private const string LogPrefix = "[BattleFlow]";
        private static BattleState _state;

        /// <summary>游戏开始战报/同节点技能链未完成前，禁止一般操作（仅允许同节点技能点击）。</summary>
        private static bool _awaitingGameStartSequence;

        private static int _opponentPlannedAttackGeneral = int.MinValue;
        private static int _opponentPlannedAttackSkill = int.MinValue;
        private static string _opponentPlannedAttackSkillName = string.Empty;

        public static event Action<bool> OnPreparationStart;
        public static event Action<bool> OnPreparationMain;
        public static event Action<bool> OnPreparationEnd;
        public static event Action<bool> OnIncomeStart;
        public static event Action<bool> OnIncomeMain;
        public static event Action<bool> OnIncomeEnd;
        public static event Action<bool> OnPrimaryStart;
        public static event Action<bool> OnPrimaryMain;
        public static event Action<bool> OnPrimaryEnd;
        public static event Action<bool> OnPlayPhaseStart;
        public static event Action<bool> OnPlayPhaseMain;
        public static event Action<bool> OnPlayPhaseEnd;
        public static event Action<bool> OnAttackSelectionRequested;
        public static event Action<bool> OnDefenseStart;
        public static event Action<bool> OnDefenseMain;
        public static event Action<bool> OnDefenseEnd;
        public static event Action<bool> OnResolveStart;
        public static event Action<bool> OnResolveMain;
        public static event Action<bool> OnResolveEnd;
        public static event Action<bool> OnDiscardStart;
        public static event Action<bool, int> OnDiscardMain;
        public static event Action<bool> OnDiscardEnd;

        public static BattleState GetState()
        {
            return _state;
        }

        public static void Bind(BattleState state)
        {
            _state = state;
            Log("Bind state: " + DescribeState());
        }

        public static bool IsAwaitingGameStartSequence() => _state != null && _awaitingGameStartSequence;

        public static void OnGameStart()
        {
            if (_state == null)
                return;

            _awaitingGameStartSequence = true;
            BattleFlowLog.Clear();
            string opener = "\u3010\u5168\u5c40\u3011\u786e\u5b9a\u5148\u624b\u73a9\u5bb6\u4e3a\uff1a" + (_state.PlayerGoesFirst ? "\u5df1\u65b9" : "\u654c\u65b9") + "\u3002";

            Debug.Log("[BattlePhaseManager] ========== 游戏开始 ==========");
            Debug.Log("[BattlePhaseManager] 先手: " + (_state.IsPlayerTurn ? "玩家" : "对手"));

            BattleFlowPacing.AddLogThenContinue(opener, () =>
            {
                if (_state == null)
                    return;

                BattleState.Draw(_state.Player, BattleState.DefaultHandLimit);
                BattleState.Draw(_state.Opponent, BattleState.DefaultHandLimit);
                OfflineSkillEngine.RefreshContinuousState(_state);
                LogGameStartSkillLinesThen(() =>
                {
                    if (_state == null)
                        return;

                    _awaitingGameStartSequence = false;
                    _state.CurrentPhase = BattlePhase.Preparation;
                    _state.CurrentPhaseStep = PhaseStep.Start;
                    Debug.Log("[BattlePhaseManager] --> 进入阶段: " + _state.CurrentPhase + " / " + _state.CurrentPhaseStep);
                    RunPhaseStep();
                });
            });
        }

        public static void EndTurn()
        {
            if (_state == null)
                return;
            if (GameUI.IsBattleMatchEnded())
                return;
            if (IsAwaitingGameStartSequence())
                return;
            if (ToastUI.IsSkillBannerTimeFreezeActive())
                return;

            Debug.Log("[BattlePhaseManager] >>> EndTurn() 被调用");
            Debug.Log("[BattlePhaseManager] 当前阶段: " + _state.CurrentPhase + " / " + _state.CurrentPhaseStep);
            Debug.Log("[BattlePhaseManager] 当前行动方: " + (_state.IsPlayerTurn ? "玩家" : "对手"));
            Debug.Log("[BattlePhaseManager] 回合数: " + _state.TurnNumber);

            if (_state.CurrentPhase == BattlePhase.Primary && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                _state.CurrentPhaseStep = PhaseStep.End;
                RunPhaseStep();
                return;
            }

            if (_state.CurrentPhase == BattlePhase.Main && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                Debug.Log("[BattlePhaseManager] 出牌区卡牌数: " + _state.ActiveSide.PlayedThisPhase.Count);
                if (_state.ActiveSide.PlayedThisPhase.Count > 0 && _state.PendingAttackSkillKind == SelectedSkillKind.None)
                {
                    Debug.Log("[BattlePhaseManager] >>> 触发攻击技能选择!");
                    if (_state.IsPlayerTurn)
                    {
                        Debug.Log("[BattlePhaseManager] >>> 弹出攻击技能选择框 (等待玩家)");
                        OnAttackSelectionRequested?.Invoke(true);
                        GameUI.NotifyPhaseChanged();
                        return;
                    }

                    Debug.Log("[BattlePhaseManager] >>> 对手自动选择攻击技能");
                    AutoSelectAttackSkill(false, () =>
                    {
                        if (_state == null)
                            return;
                        OfferHuBuGuanYouThenContinueMainEnd(() =>
                        {
                            if (_state == null)
                                return;
                            _state.CurrentPhaseStep = PhaseStep.End;
                            Debug.Log("[BattlePhaseManager] --> 进入 End 步骤");
                            RunPhaseStep();
                            GameUI.NotifyPhaseChanged();
                        });
                    });
                    return;
                }

                _state.CurrentPhaseStep = PhaseStep.End;
                Debug.Log("[BattlePhaseManager] --> 进入 End 步骤");
                RunPhaseStep();
                return;
            }

            if (_state.CurrentPhase == BattlePhase.Defense && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                _state.CurrentPhaseStep = PhaseStep.End;
                RunPhaseStep();
                return;
            }
        }

        /// <summary>
        /// 战报停顿改为异步后，<see cref="RunPhaseStep"/> 可能在首帧提前返回，阶段尚未到主动/出牌；
        /// 在每次战报续跑结束时调用，以补执行原 <c>TurnEnd</c> 里针对对手的同步 <c>EndTurn</c> 循环。
        /// </summary>
        /// <summary>技能横幅解冻后由 <see cref="ToastUI"/> 调用，补跑对手自动推进。</summary>
        public static void NotifyToastBannerUnblocked()
        {
            TryOpponentAutoAdvanceAfterBattleFlowPacing();
        }

        public static void TryOpponentAutoAdvanceAfterBattleFlowPacing()
        {
            if (_state == null)
                return;
            if (GameUI.IsBattleMatchEnded())
                return;
            if (IsAwaitingGameStartSequence())
                return;
            if (ToastUI.IsSkillBannerTimeFreezeActive())
                return;
            if (_state.IsPlayerTurn)
                return;
            if (!GameUI.IsOpponentTurnAutoEndEnabled())
                return;

            const int maxSteps = 64;
            for (int i = 0; i < maxSteps; i++)
            {
                if (_state == null || _state.IsPlayerTurn || !GameUI.IsOpponentTurnAutoEndEnabled())
                    break;
                if (_state.CurrentPhase != BattlePhase.Primary && _state.CurrentPhase != BattlePhase.Main)
                    break;
                EndTurn();
            }

            GameUI.NotifyPhaseChanged();
        }

        public static void NotifyAttackSkillSelected(bool attackerIsPlayer, int generalIndex, int skillIndex, string skillName)
        {
            Debug.Log("[BattlePhaseManager] >>> NotifyAttackSkillSelected: " + skillName);
            if (_state == null)
                return;
            if (GameUI.IsBattleMatchEnded())
                return;
            if (_state.CurrentPhase != BattlePhase.Main || _state.CurrentPhaseStep != PhaseStep.Main)
                return;
            if (_state.IsPlayerTurn != attackerIsPlayer)
                return;

            SetAttackSkillSelection(generalIndex, skillIndex, skillName, () =>
            {
                if (_state == null || _state.PendingGenericAttackShapeChoicePending)
                    return;
                OfferHuBuGuanYouThenContinueMainEnd(() =>
                {
                    if (_state == null)
                        return;
                    _state.CurrentPhaseStep = PhaseStep.End;
                    Debug.Log("[BattlePhaseManager] 攻击技能已选择，进入 Defense 阶段");
                    RunPhaseStep();
                    GameUI.NotifyPhaseChanged();
                });
            });

            if (_state.PendingGenericAttackShapeChoicePending)
            {
                GameUI.OpenGenericAttackShapePopup(openedAfterConfigurePending: true);
                GameUI.NotifyPhaseChanged();
                return;
            }
        }

        /// <summary>玩家在「通用攻击」多牌型弹窗中选定一项后调用（含策马失败回退后的二次选择）。</summary>
        public static void CompletePlayerGenericAttackShapePick(int optionIndex)
        {
            if (_state == null)
                return;
            if (GameUI.IsBattleMatchEnded())
                return;
            if (_state.CurrentPhase != BattlePhase.Main || _state.CurrentPhaseStep != PhaseStep.Main || !_state.IsPlayerTurn)
                return;

            var opts = GenericAttackShapes.BuildSortedOptions(_state.ActiveSide.PlayedThisPhase);
            if (optionIndex < 0 || optionIndex >= opts.Count)
                return;

            _state.PendingGenericAttackOptionIndex = optionIndex;
            _state.PendingGenericAttackShapeChoicePending = false;

            _state.PendingAttackGeneralIndex = -1;
            _state.PendingAttackSkillIndex = -1;
            _state.PendingAttackSkillKind = SelectedSkillKind.GenericAttack;
            _state.PendingAttackSkillName = "\u901a\u7528\u653b\u51fb";

            OfflineSkillEngine.ConfigureAttackSkill(_state, true, -1, -1, () =>
            {
                if (_state == null)
                    return;
                LogAttackSkillConfigured();
                NotifyActiveSideHandEmptyPassivesIfMatchActive();
                _state.CurrentPhaseStep = PhaseStep.End;
                Debug.Log("[BattlePhaseManager] 通用攻击牌型已选定，进入 Defense 阶段");
                RunPhaseStep();
                GameUI.NotifyPhaseChanged();
            });
        }

        public static void CancelPendingGenericAttackShapeChoice()
        {
            if (_state == null)
                return;
            _state.PendingGenericAttackShapeChoicePending = false;
            _state.PendingGenericAttackOptionIndex = -1;
        }

        /// <summary>从「须选通用牌型」弹窗返回上一级时：清空已部分写入的待结算攻击，以便重新选将技或通用攻击。</summary>
        public static void ResetAfterGenericShapePopupCancel()
        {
            if (_state == null)
                return;
            CancelPendingGenericAttackShapeChoice();
            _state.PendingAttackBonus = 0;
            _state.PendingBaseDamage = 0;
            _state.PendingPostResolveDrawToAttacker = 0;
            _state.PendingExtraPlayPhasesToGrant = 0;
            _state.PendingCombatNote = string.Empty;
            _state.PendingAttackSkillKind = SelectedSkillKind.None;
            _state.PendingAttackSkillName = string.Empty;
        }

        private static void TryShowDefenseDeclareBanner(bool defenderIsPlayer, int generalIndex, int skillIndex, Action onAfterBannerIfAny = null)
        {
            if (_state == null || generalIndex < 0)
            {
                onAfterBannerIfAny?.Invoke();
                return;
            }

            SideState defSide = _state.GetSide(defenderIsPlayer);
            if (generalIndex >= defSide.GeneralCardIds.Count)
            {
                onAfterBannerIfAny?.Invoke();
                return;
            }

            string cid = defSide.GeneralCardIds[generalIndex] ?? string.Empty;
            SkillRuleEntry defRule = SkillRuleLoader.GetRule(cid, skillIndex);
            if (defRule != null && string.Equals(defRule.EffectId, OfflineSkillEngine.DefenseRevealSmall8ReduceElseGainEffectId, StringComparison.Ordinal))
            {
                onAfterBannerIfAny?.Invoke();
                return;
            }

            string sk = defRule != null && !string.IsNullOrWhiteSpace(defRule.SkillName)
                ? defRule.SkillName
                : _state.PendingDefenseSkillName;
            SkillEffectBanner.Show(
                defenderIsPlayer,
                true,
                SkillEffectBanner.GetRoleNameFromCardId(cid),
                sk,
                "\u767b\u8bb0\u51cf\u4f24" + _state.PendingDefenseReduction,
                onAfterBannerIfAny);
        }

        /// <summary>【八门金锁】察势弹窗结束后补登记防御宣告战报与横幅（与 <see cref="OfflineSkillEngine.ConfigureDefenseSkill"/> 延迟返回配对）。</summary>
        public static void CompleteDefenseDeclareAfterDeferredBamen(bool defenderIsPlayer, int generalIndex, int skillIndex)
        {
            if (_state == null || GameUI.IsBattleMatchEnded())
                return;
            if (_state.CurrentPhase != BattlePhase.Defense || _state.CurrentPhaseStep != PhaseStep.Main)
                return;
            if ((_state.IsPlayerTurn ? false : true) != defenderIsPlayer)
                return;

            BattleFlowLog.Add(
                FlowTurnBracket(_state.IsPlayerTurn) + "\u9632\u5fa1\u9636\u6bb5\uff0c" + FlowDefenderActor(defenderIsPlayer) + "\u58f0\u660e\u9632\u5fa1\u6280\u3010" + _state.PendingDefenseSkillName + "\u3011\uff0c\u767b\u8bb0\u51cf\u4f24" + _state.PendingDefenseReduction + "\u3002");
            Action afterBanner = defenderIsPlayer && !GameUI.IsOnlineBattle() ? TryAutoEndPlayerDefenseAfterDeclareIfIdle : null;
            TryShowDefenseDeclareBanner(defenderIsPlayer, generalIndex, skillIndex, afterBanner);
            GameUI.NotifyPhaseChanged();
        }

        public static void NotifyDefenseSkillSelected(bool defenderIsPlayer, int generalIndex, int skillIndex, string skillName)
        {
            Debug.Log("[BattlePhaseManager] >>> NotifyDefenseSkillSelected: " + skillName);
            if (_state == null)
                return;
            if (GameUI.IsBattleMatchEnded())
                return;
            if (_state.CurrentPhase != BattlePhase.Defense || _state.CurrentPhaseStep != PhaseStep.Main)
                return;
            if ((_state.IsPlayerTurn ? false : true) != defenderIsPlayer)
                return;

            if (_state.PendingIgnoreDefenseReduction)
            {
                ToastUI.Show("\u672c\u6b21\u4f24\u5bb3\u4e0d\u53ef\u9632\u5fa1\uff0c\u65e0\u6cd5\u4f7f\u7528\u9632\u5fa1\u6280", 2.2f);
                return;
            }

            if (_state.DefenseSkillLocked)
            {
                ToastUI.Show("\u672c\u6b21\u53d7\u51fb\u5df2\u58f0\u660e\u9632\u5fa1\u6280\uff0c\u65e0\u6cd5\u518d\u9009\u62e9\u6216\u66f4\u6362\u3002", 2.2f);
                return;
            }

            _state.PendingDefenseGeneralIndex = generalIndex;
            _state.PendingDefenseSkillIndex = skillIndex;
            _state.PendingDefenseSkillName = string.IsNullOrWhiteSpace(skillName) ? "防御技" : skillName;
            _state.PendingDefenseReduction = 0;
            _state.PendingDefenseSkillKind = SelectedSkillKind.GeneralSkill;
            bool deferDefenseDeclare = OfflineSkillEngine.ConfigureDefenseSkill(_state, defenderIsPlayer, generalIndex, skillIndex);
            if (_state.PendingDefenseReduction <= 0)
                _state.PendingDefenseReduction = 1;
            _state.DefenseSkillLocked = true;
            Debug.Log("[BattlePhaseManager] 防御技能已选择");
            if (!deferDefenseDeclare)
            {
                BattleFlowLog.Add(
                    FlowTurnBracket(_state.IsPlayerTurn) + "\u9632\u5fa1\u9636\u6bb5\uff0c" + FlowDefenderActor(defenderIsPlayer) + "\u58f0\u660e\u9632\u5fa1\u6280\u3010" + _state.PendingDefenseSkillName + "\u3011\uff0c\u767b\u8bb0\u51cf\u4f24" + _state.PendingDefenseReduction + "\u3002");
                Action afterBanner = defenderIsPlayer && !GameUI.IsOnlineBattle() ? TryAutoEndPlayerDefenseAfterDeclareIfIdle : null;
                TryShowDefenseDeclareBanner(defenderIsPlayer, generalIndex, skillIndex, afterBanner);
            }

            GameUI.NotifyPhaseChanged();
        }

        /// <summary>离线玩家宣告防御技后：若无其他防御阶段可点技能，则在宣告横幅结束（或无需横幅）后自动结束防御。</summary>
        private static void TryAutoEndPlayerDefenseAfterDeclareIfIdle()
        {
            if (_state == null || GameUI.IsBattleMatchEnded())
                return;
            if (GameUI.IsOnlineBattle())
                return;
            if (_state.CurrentPhase != BattlePhase.Defense || _state.CurrentPhaseStep != PhaseStep.Main)
                return;
            if (_state.IsPlayerTurn)
                return;
            if (ToastUI.IsSkillBannerTimeFreezeActive())
                return;
            if (GameUI.PlayerHasOtherUsableSkillsInDefensePhaseMain())
                return;
            EndTurn();
        }

        public static void NotifyDiscardPhaseDone(bool isPlayer, int[] handIndices)
        {
            Debug.Log("[BattlePhaseManager] >>> NotifyDiscardPhaseDone: " + (isPlayer ? "玩家" : "对手"));
            if (_state == null)
                return;
            if (GameUI.IsBattleMatchEnded())
                return;
            if (_state.CurrentPhase != BattlePhase.Discard || _state.CurrentPhaseStep != PhaseStep.Main)
                return;

            SideState side = isPlayer ? _state.Player : _state.Opponent;
            int over = side.Hand.Count - _state.HandLimit;
            if (over > 0)
            {
                if (handIndices != null && handIndices.Length == over)
                {
                    var indexList = new List<int>(handIndices);
                    BattleState.DiscardFromHand(_state, isPlayer, indexList);
                    Debug.Log("[BattlePhaseManager] 弃牌完成，弃置 " + over + " 张");
                }
                else if (!isPlayer)
                {
                    var autoDiscard = new List<int>();
                    for (int i = 0; i < over; i++)
                        autoDiscard.Add(side.Hand.Count - 1 - i);
                    BattleState.DiscardFromHand(_state, isPlayer, autoDiscard);
                    Debug.Log("[BattlePhaseManager] 对手自动弃牌 " + over + " 张");
                }
            }

            _state.CurrentPhaseStep = PhaseStep.End;
            RunPhaseStep();
        }

        public static void NotifyDiscardPhaseSkipPopup()
        {
            if (_state == null)
                return;
            if (GameUI.IsBattleMatchEnded())
                return;
            if (_state.CurrentPhase != BattlePhase.Discard || _state.CurrentPhaseStep != PhaseStep.Main)
                return;

            _state.CurrentPhaseStep = PhaseStep.End;
            RunPhaseStep();
        }

        private static void RunPhaseStep()
        {
            if (_state == null)
                return;
            if (GameUI.IsBattleMatchEnded())
                return;

            bool attackerIsPlayer = _state.IsPlayerTurn;
            bool defenderIsPlayer = !attackerIsPlayer;

            Debug.Log("[BattlePhaseManager] RunPhaseStep: " + _state.CurrentPhase + " / " + _state.CurrentPhaseStep + " / " + (attackerIsPlayer ? "玩家" : "对手") + "行动");

            switch (_state.CurrentPhase)
            {
                case BattlePhase.Preparation:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnPreparationStart?.Invoke(attackerIsPlayer);
                        BattleFlowLog.AddRoundBeginMarker(_state.TurnNumber);
                        BattleFlowPacing.AddLogThenContinue(
                            FlowTurnBracket(attackerIsPlayer) + "\u56de\u5408\u5f00\u59cb\uff0c\u51c6\u5907\u9636\u6bb5\uff0c\u65e0\u4e8b\u53d1\u751f\u3002",
                            () =>
                            {
                                if (_state == null)
                                    return;
                                _state.CurrentPhaseStep = PhaseStep.Main;
                                RunPhaseStep();
                            });
                        return;
                    }

                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        OnPreparationMain?.Invoke(attackerIsPlayer);
                        BattleFlowPacing.AddLogThenContinue(
                            FlowTurnBracket(attackerIsPlayer) + "\u51c6\u5907\u9636\u6bb5\u8fdb\u884c\u4e2d\uff0c\u65e0\u4e8b\u53d1\u751f\u3002",
                            () =>
                            {
                                if (_state == null)
                                    return;
                                _state.CurrentPhaseStep = PhaseStep.End;
                                RunPhaseStep();
                            });
                        return;
                    }

                    OnPreparationEnd?.Invoke(attackerIsPlayer);
                    _state.CurrentPhase = BattlePhase.Income;
                    _state.CurrentPhaseStep = PhaseStep.Start;
                    RunPhaseStep();
                    return;

                case BattlePhase.Income:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnIncomeStart?.Invoke(attackerIsPlayer);
                        BattleFlowPacing.AddLogThenContinue(
                            FlowTurnBracket(attackerIsPlayer) + "\u6536\u5165\u9636\u6bb5\u5f00\u59cb\u3002",
                            () =>
                            {
                                if (_state == null)
                                    return;
                                _state.CurrentPhaseStep = PhaseStep.Main;
                                RunPhaseStep();
                            });
                        return;
                    }

                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        OnIncomeMain?.Invoke(attackerIsPlayer);
                        SideState side = _state.ActiveSide;
                        int drawCnt = side.GetFaceUpGeneralCount();
                        BattleState.Draw(side, drawCnt);

                        bool skipFirstMorale = _state.TurnNumber == 1 && _state.IsPlayerTurn == _state.PlayerGoesFirst;
                        if (!skipFirstMorale)
                            side.Morale = Mathf.Min(side.MoraleCap, side.Morale + 1);

                        string incomeLine = skipFirstMorale
                            ? FlowTurnBracket(attackerIsPlayer) + "\u6536\u5165\u9636\u6bb5\uff0c\u6478" + drawCnt + "\u5f20\u724c\uff08\u672a\u7ffb\u9762\u89d2\u8272\u6570\uff09\uff0c\u9996\u56de\u5408\u5148\u624b\u8df3\u8fc7\u58eb\u6c14\u6062\u590d\u3002"
                            : FlowTurnBracket(attackerIsPlayer) + "\u6536\u5165\u9636\u6bb5\uff0c\u6478" + drawCnt + "\u5f20\u724c\uff08\u672a\u7ffb\u9762\u89d2\u8272\u6570\uff09\uff0c\u58eb\u6c14+1\u3002";

                        BattleFlowPacing.AddLogThenContinue(incomeLine, () =>
                        {
                            if (_state == null)
                                return;
                            _state.CurrentPhaseStep = PhaseStep.End;
                            RunPhaseStep();
                        });
                        return;
                    }

                    OnIncomeEnd?.Invoke(attackerIsPlayer);
                    _state.CurrentPhase = BattlePhase.Primary;
                    _state.CurrentPhaseStep = PhaseStep.Start;
                    RunPhaseStep();
                    return;

                case BattlePhase.Primary:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnPrimaryStart?.Invoke(attackerIsPlayer);
                        BattleFlowLog.Add(FlowTurnBracket(attackerIsPlayer) + "\u4e3b\u52a8\u9636\u6bb5\uff0c\u53ef\u53d1\u52a8\u4e3b\u52a8\u6280\u6216\u7ed3\u675f\u8be5\u9636\u6bb5\u3002");
                        _state.CurrentPhaseStep = PhaseStep.Main;
                        GameUI.NotifyPhaseChanged();
                        return;
                    }

                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        OnPrimaryMain?.Invoke(attackerIsPlayer);
                        GameUI.NotifyPhaseChanged();
                        return;
                    }

                    OnPrimaryEnd?.Invoke(attackerIsPlayer);
                    _state.CurrentPlayPhaseIndex = 0;
                    _state.CurrentPhase = BattlePhase.Main;
                    _state.CurrentPhaseStep = PhaseStep.Start;
                    RunPhaseStep();
                    return;

                case BattlePhase.Main:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnPlayPhaseStart?.Invoke(attackerIsPlayer);
                        _state.ClearPendingCombat();
                        BattleFlowLog.Add(FlowTurnBracket(attackerIsPlayer) + "\u51fa\u724c\u9636\u6bb5\u5f00\u59cb\u3002");
                        void advancePlayToMain()
                        {
                            if (_state == null)
                                return;
                            if (!attackerIsPlayer)
                                AutoPlayForOpponent();
                            _state.CurrentPhaseStep = PhaseStep.Main;
                            GameUI.NotifyPhaseChanged();
                        }

                        GameUI.RunPlayPhaseStartPromptsThen(attackerIsPlayer, advancePlayToMain);
                        return;
                    }

                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        OnPlayPhaseMain?.Invoke(attackerIsPlayer);
                        GameUI.NotifyPhaseChanged();
                        return;
                    }

                    OnPlayPhaseEnd?.Invoke(attackerIsPlayer);
                    if (_state.ActiveSide.PlayedThisPhase.Count > 0)
                    {
                        if (_state.PendingAttackSkillKind == SelectedSkillKind.None)
                        {
                            SetAttackSkillSelection(-1, -1, "\u901a\u7528\u653b\u51fb", () =>
                            {
                                if (_state == null)
                                    return;
                                OfferHuBuGuanYouThenContinueMainEnd(() =>
                                {
                                    if (_state == null)
                                        return;
                                    _state.CurrentPhase = BattlePhase.Defense;
                                    _state.CurrentPhaseStep = PhaseStep.Start;
                                    RunPhaseStep();
                                    GameUI.NotifyPhaseChanged();
                                });
                            });
                            return;
                        }

                        OfferHuBuGuanYouThenContinueMainEnd(() =>
                        {
                            if (_state == null)
                                return;
                            NotifyActiveSideHandEmptyPassivesIfMatchActive();
                            _state.CurrentPhase = BattlePhase.Defense;
                            _state.CurrentPhaseStep = PhaseStep.Start;
                            RunPhaseStep();
                            GameUI.NotifyPhaseChanged();
                        });
                        return;
                    }

                    AdvanceFromPlayPhase();
                    return;

                case BattlePhase.Defense:
                    Debug.Log("[BattlePhaseManager] ========== 进入防御阶段 ==========");
                    Debug.Log("[BattlePhaseManager] 攻击方: " + (attackerIsPlayer ? "玩家" : "对手"));
                    Debug.Log("[BattlePhaseManager] 防御方: " + (defenderIsPlayer ? "玩家" : "对手"));
                    Debug.Log("[BattlePhaseManager] IsPlayerTurn: " + (_state.IsPlayerTurn ? "玩家" : "对手"));
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        BattleFlowPacing.AddLogThenContinue(
                            FlowTurnBracket(attackerIsPlayer) + (defenderIsPlayer ? "\u5df1\u65b9\u73a9\u5bb6" : "\u654c\u65b9") + "\u8fdb\u5165\u9632\u5fa1\u9636\u6bb5\u3002",
                            () =>
                            {
                                if (_state == null)
                                    return;
                                _state.DefenseSkillLocked = false;
                                OnDefenseStart?.Invoke(defenderIsPlayer);
                                _state.CurrentPhaseStep = PhaseStep.Main;
                                Debug.Log("[BattlePhaseManager] --> 防御阶段 Start -> Main");
                                Debug.Log("[BattlePhaseManager] >>> 调用 RunPhaseStep() 进入 Defense/Main");
                                RunPhaseStep();
                                Debug.Log("[BattlePhaseManager] >>> Defense/Main 返回");
                            });
                        return;
                    }

                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        Debug.Log("[BattlePhaseManager] 防御阶段 Main 步骤, defenderIsPlayer=" + defenderIsPlayer);
                        Debug.Log("[BattlePhaseManager] !defenderIsPlayer = " + (!defenderIsPlayer));
                        OnDefenseMain?.Invoke(defenderIsPlayer);
                        Debug.Log("[BattlePhaseManager] >>> OnDefenseMain 已调用");
                        if (!defenderIsPlayer)
                        {
                            Debug.Log("[BattlePhaseManager] >>> [分支1] 对手自动选择防御技能");
                            OfflineSkillEngine.MaybeAutoUseResistBeforeDefenseSkill(_state, false);
                            AutoSelectDefenseSkill(false);
                            Debug.Log("[BattlePhaseManager] >>> 设置 CurrentPhaseStep = End");
                            _state.CurrentPhaseStep = PhaseStep.End;
                            Debug.Log("[BattlePhaseManager] >>> 调用 RunPhaseStep() 进入 End 步骤");
                            RunPhaseStep();
                            Debug.Log("[BattlePhaseManager] >>> RunPhaseStep 返回");
                            return;
                        }
                        else
                        {
                            Debug.Log("[BattlePhaseManager] >>> [分支2] 玩家是防御方，等待玩家结束防御");
                        }

                        Debug.Log("[BattlePhaseManager] >>> 等待玩家结束防御 (点击结束防御按钮)");
                        Debug.Log("[BattlePhaseManager] >>> 调用 GameUI.NotifyPhaseChanged()");
                        GameUI.NotifyPhaseChanged();
                        Debug.Log("[BattlePhaseManager] >>> NotifyPhaseChanged 返回");
                        return;
                    }

                    OnDefenseEnd?.Invoke(defenderIsPlayer);
                    _state.CurrentPhase = BattlePhase.Resolve;
                    _state.CurrentPhaseStep = PhaseStep.Start;
                    RunPhaseStep();
                    return;

                case BattlePhase.Resolve:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnResolveStart?.Invoke(attackerIsPlayer);
                        BattleFlowPacing.AddLogThenContinue(
                            FlowTurnBracket(attackerIsPlayer) + "\u7ed3\u7b97\u9636\u6bb5\u5f00\u59cb\u3002",
                            () =>
                            {
                                if (_state == null)
                                    return;
                                _state.CurrentPhaseStep = PhaseStep.Main;
                                RunPhaseStep();
                            });
                        return;
                    }

            if (_state.CurrentPhaseStep == PhaseStep.Main)
            {
                OnResolveMain?.Invoke(attackerIsPlayer);
                ResolveCurrentCombat(() =>
                {
                    if (_state == null)
                        return;
                    if (GameUI.IsBattleMatchEnded())
                        return;
                    _state.CurrentPhaseStep = PhaseStep.End;
                    RunPhaseStep();
                });
                return;
            }

                    OnResolveEnd?.Invoke(attackerIsPlayer);
                    _state.FinishCurrentPlayPhaseCombat();
                    AdvanceFromPlayPhase();
                    return;

                case BattlePhase.Discard:
                    Debug.Log("[BattlePhaseManager] ========== 进入弃牌阶段 ==========");
                    Debug.Log("[BattlePhaseManager] 行动方: " + (attackerIsPlayer ? "玩家" : "对手"));
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        _state.RenZheWuDiHandledThisDiscardPhase = false;
                        OnDiscardStart?.Invoke(attackerIsPlayer);
                        BattleFlowPacing.AddLogThenContinue(
                            FlowTurnBracket(attackerIsPlayer) + "\u5f03\u724c\u9636\u6bb5\u5f00\u59cb\u3002",
                            () =>
                            {
                                if (_state == null)
                                    return;
                                void goDiscardMain()
                                {
                                    if (_state == null)
                                        return;
                                    _state.CurrentPhaseStep = PhaseStep.Main;
                                    Debug.Log("[BattlePhaseManager] --> 弃牌阶段 Start -> Main");
                                    RunPhaseStep();
                                }

                                GameUI.RunDiscardPhaseStartPromptsThen(attackerIsPlayer, goDiscardMain);
                            });
                        return;
                    }

                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        SideState side = _state.ActiveSide;
                        int over = side.Hand.Count - _state.HandLimit;
                        Debug.Log("[BattlePhaseManager] 手牌: " + side.Hand.Count + " / 上限: " + _state.HandLimit + " -> 超出: " + over);
                        if (over > 0)
                        {
                            Debug.Log("[BattlePhaseManager] >>> 需要弃置 " + over + " 张牌，弹出弃牌框");
                            OnDiscardMain?.Invoke(attackerIsPlayer, over);
                            return;
                        }

                        Debug.Log("[BattlePhaseManager] 无需弃牌，直接进入 End");
                        _state.CurrentPhaseStep = PhaseStep.End;
                        RunPhaseStep();
                        return;
                    }

                    OnDiscardEnd?.Invoke(attackerIsPlayer);
                    OfflineSkillEngine.TryTriggerHandEmptyPassive(_state, attackerIsPlayer, out _);

                    void advanceDiscardToTurnEnd()
                    {
                        if (_state == null)
                            return;
                        _state.CurrentPhase = BattlePhase.TurnEnd;
                        _state.CurrentPhaseStep = PhaseStep.Main;
                        RunPhaseStep();
                    }

                    if (!_state.RenZheWuDiHandledThisDiscardPhase)
                    {
                        bool renZheDeferred = OfflineSkillEngine.TryApplyDiscardEndRenZheWuDi(_state, attackerIsPlayer, advanceDiscardToTurnEnd, out string renZheMsg);
                        if (!renZheDeferred && !string.IsNullOrEmpty(renZheMsg))
                            BattleFlowLog.Add(FlowTurnBracket(attackerIsPlayer) + "\u5f03\u724c\u9636\u6bb5\u7ed3\u675f\uff0c\u3010\u4ec1\u8005\u65e0\u654c\u3011\uff1a" + renZheMsg);
                    }
                    else
                        advanceDiscardToTurnEnd();

                    // 同步路径下 advanceDiscardToTurnEnd 已由 TryApplyDiscardEndRenZheWuDi 内部调用，切勿再调用一次（否则会连跑两次 TurnEnd，换边错乱甚至对手无限回合）。
                    return;

                case BattlePhase.TurnEnd:
                    Debug.Log("[BattlePhaseManager] ========== 回合结束 ==========");
                    _state.CompleteCurrentTurn();
                    _state.IsPlayerTurn = !_state.IsPlayerTurn;
                    _state.TurnNumber++;
                    Debug.Log("[BattlePhaseManager] 换边 -> " + (_state.IsPlayerTurn ? "玩家" : "对手") + " 行动");
                    Debug.Log("[BattlePhaseManager] 进入第 " + _state.TurnNumber + " 回合");
                    _state.CurrentPhase = BattlePhase.Preparation;
                    _state.CurrentPhaseStep = PhaseStep.Start;

                    if (!_state.IsPlayerTurn && GameUI.IsOpponentTurnAutoEndEnabled())
                    {
                        Debug.Log("[BattlePhaseManager] >>> 对手回合自动执行");
                        RunPhaseStep();
                        TryOpponentAutoAdvanceAfterBattleFlowPacing();
                        return;
                    }

                    Debug.Log("[BattlePhaseManager] --> 进入玩家回合 Preparation");
                    RunPhaseStep();
                    GameUI.NotifyPhaseChanged();
                    return;
            }
        }

        /// <summary>对局未结束时，若当前行动方手牌已为 0，尝试结算「手牌为 0」类被动（如攻击宣言后、打出区牌仍在手牌已空）。</summary>
        private static void NotifyActiveSideHandEmptyPassivesIfMatchActive()
        {
            if (_state == null || GameUI.IsBattleMatchEnded())
                return;
            BattleState.NotifyHandMaybeBecameZero(_state, _state.IsPlayerTurn);
        }

        private static void AdvanceFromPlayPhase()
        {
            _state.ActiveSide.MovePlayedCardsToDiscard();
            // 「手牌为 0」类被动：打出区已进入弃牌堆后再判一次（与弃牌/虎步/攻击宣言等路径上的 Notify 互补；已触发键会跳过）。
            BattleState.NotifyHandMaybeBecameZero(_state, _state.IsPlayerTurn);
            _state.ClearPendingCombat();
            _state.CurrentPlayPhaseIndex++;
            if (_state.CurrentPlayPhaseIndex < _state.TotalPlayPhasesThisTurn)
            {
                _state.CurrentPhase = BattlePhase.Main;
                _state.CurrentPhaseStep = PhaseStep.Start;
                RunPhaseStep();
                return;
            }

            _state.CurrentPhase = BattlePhase.Discard;
            _state.CurrentPhaseStep = PhaseStep.Start;
            RunPhaseStep();
        }

        private static void ResolveCurrentCombat(Action onResolveFullyComplete)
        {
            int defenseReduction = _state.PendingIgnoreDefenseReduction ? 0 : _state.PendingDefenseReduction;
            int rawHit = _state.PendingBaseDamage + _state.PendingAttackBonus;
            int damageAfterDefense = rawHit - defenseReduction;
            if (_state.PendingHalveIncomingDamageWithResist)
            {
                int resistCut = Mathf.CeilToInt(damageAfterDefense / 2f);
                damageAfterDefense = Mathf.Max(0, damageAfterDefense - resistCut);
                _state.PendingHalveIncomingDamageWithResist = false;
            }

            int changHouBonus = OfflineSkillEngine.GetChangHouBonusWhenResolvingAttackDamage(_state);
            int damage = Mathf.Max(0, damageAfterDefense + changHouBonus);
            string attackName = string.IsNullOrWhiteSpace(_state.PendingAttackSkillName) ? "\u901a\u7528\u653b\u51fb" : _state.PendingAttackSkillName;
            string defenseName = string.IsNullOrWhiteSpace(_state.PendingDefenseSkillName) ? "\u672a\u9632\u5fa1" : _state.PendingDefenseSkillName;
            DamageCategory dmgCat = _state.PendingDamageCategory == DamageCategory.None ? DamageCategory.Generic : _state.PendingDamageCategory;
            DamageElement dmgEl = _state.PendingDamageElement;
            string summary = attackName + "\u7ed3\u7b97\uff1a" + DamageTypeLabels.FormatResolvedDamageLine(damage, dmgCat, dmgEl);

            if (_state.PendingDefenseReduction > 0)
            {
                if (_state.PendingIgnoreDefenseReduction)
                    summary += "\uff0c\u9632\u5fa1\u51cf\u514d\u672a\u751f\u6548";
                else
                    summary += "\uff08\u5df2\u8ba1\u5165\u9632\u5fa1\u51cf\u514d\uff09";
            }

            if (changHouBonus > 0)
                summary += "\uff0c\u3010\u957f\u543c\u3011\u52a0\u4f24+" + changHouBonus;

            string summaryToast = summary + "\uff08\u9632\u5fa1\uff1a" + defenseName + "\uff09";

            ToastUI.Show(summaryToast, 2.4f, pauseGameWhileVisible: true, () =>
            {
                if (_state == null)
                    return;

                if (damage > 0)
                {
                    if (_state.IsPlayerTurn)
                        GameUI.ApplyDamageToOpponent(damage);
                    else
                        GameUI.ApplyDamageToPlayer(damage);
                }

                if (GameUI.IsBattleMatchEnded())
                {
                    _state.ClearPendingCombat();
                    return;
                }

                var active = _state.ActiveSide;
                if (_state.PendingPostResolveDrawToAttacker > 0)
                    BattleState.Draw(active, _state.PendingPostResolveDrawToAttacker);
                if (_state.PendingPostResolveHealToAttacker > 0)
                    active.CurrentHp = Mathf.Min(active.MaxHp, active.CurrentHp + _state.PendingPostResolveHealToAttacker);
                if (_state.PendingPostResolveMoraleToAttacker > 0)
                    active.Morale = Mathf.Min(active.MoraleCap, active.Morale + _state.PendingPostResolveMoraleToAttacker);

                if (_state.PendingExtraPlayPhasesToGrant > 0)
                {
                    _state.TotalPlayPhasesThisTurn += _state.PendingExtraPlayPhasesToGrant;
                    _state.PendingExtraPlayPhasesToGrant = 0;
                }

                var logLine = new StringBuilder();
                logLine.Append(FlowTurnBracket(_state.IsPlayerTurn)).Append("\u7ed3\u7b97\u9636\u6bb5\uff0c");
                logLine.Append("\u3010").Append(attackName).Append("\u3011");
                logLine.Append("\u5bf9").Append(_state.IsPlayerTurn ? "\u654c\u65b9" : "\u5df1\u65b9\u73a9\u5bb6");
                logLine.Append(DamageTypeLabels.FormatResolvedDamageLine(damage, dmgCat, dmgEl));
                logLine.Append("\uff08\u9632\u5fa1\uff1a").Append(defenseName).Append("\uff09");
                if (changHouBonus > 0)
                    logLine.Append("\uff0c\u3010\u957f\u543c\u3011\u52a0\u4f24+").Append(changHouBonus);
                if (_state.PendingPostResolveDrawToAttacker > 0)
                    logLine.Append("\uff0c\u653b\u51fb\u65b9\u6478").Append(_state.PendingPostResolveDrawToAttacker).Append("\u5f20\u724c");
                if (_state.PendingPostResolveHealToAttacker > 0)
                    logLine.Append("\uff0c\u653b\u51fb\u65b9\u6062\u590d").Append(_state.PendingPostResolveHealToAttacker).Append("\u70b9\u751f\u547d");
                if (_state.PendingPostResolveMoraleToAttacker > 0)
                    logLine.Append("\uff0c\u653b\u51fb\u65b9\u58eb\u6c14+").Append(_state.PendingPostResolveMoraleToAttacker);
                logLine.Append("\u3002");
                BattleFlowLog.Add(logLine.ToString());

                bool attackerIsPlayer = _state.IsPlayerTurn;
                var activePlayed = _state.ActiveSide.PlayedThisPhase;
                var flippedGenerals = new HashSet<int>();
                for (int i = 0; i < activePlayed.Count; i++)
                {
                    var c = activePlayed[i];
                    if (!c.PlayedAsGeneral)
                        continue;
                    int g = c.GeneralSlotIndex;
                    if (g < 0 || flippedGenerals.Contains(g))
                        continue;
                    flippedGenerals.Add(g);
                    if (_state.TryFlipGeneral(attackerIsPlayer, g))
                    {
                        string roleName = !string.IsNullOrEmpty(c.PlayedRoleDisplayName)
                            ? c.PlayedRoleDisplayName.Trim()
                            : GameUI.GetGeneralDisplayNameForBattleLog(attackerIsPlayer, g);
                        BattleFlowLog.Add(FlowTurnBracket(attackerIsPlayer) + "\u7ed3\u7b97\u540e\uff0c\u672c\u6b21\u5f53\u4f5c\u6253\u51fa\u7684\u89d2\u8272\u3010" + roleName + "\u3011\u5df2\u7ffb\u9762\u3002");
                    }
                }

                if (flippedGenerals.Count > 0)
                    GameUI.NotifyPhaseChanged();

                onResolveFullyComplete?.Invoke();
            });
        }

        private static void ClearOpponentAttackPlan()
        {
            _opponentPlannedAttackGeneral = int.MinValue;
            _opponentPlannedAttackSkill = int.MinValue;
            _opponentPlannedAttackSkillName = string.Empty;
        }

        private static List<(int generalIndex, int skillIndex, string skillName)> BuildAttackSkillOptionsForSide(bool sideIsPlayer)
        {
            var list = new List<(int, int, string)> { (-1, -1, "\u901a\u7528\u653b\u51fb") };
            if (_state == null)
                return list;

            SideState side = _state.GetSide(sideIsPlayer);
            for (int g = 0; g < side.GeneralCardIds.Count; g++)
            {
                if (!side.IsGeneralFaceUp(g))
                    continue;

                CardData data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(side.GeneralCardIds[g]));
                if (data == null)
                    continue;

                for (int sk = 0; sk < 3; sk++)
                {
                    if (!SkillHasTag(data, sk, "\u653b\u51fb\u6280"))
                        continue;

                    string shotKey = (sideIsPlayer ? "True" : "False") + "_" + g + "_" + sk;
                    if (SkillHasTag(data, sk, "\u7834\u519b\u6280") && side.UsedOneShotSkills.Contains(shotKey))
                        continue;

                    list.Add((g, sk, GetSkillName(data, sk)));
                }
            }

            return list;
        }

        private static void AutoPlayForOpponent()
        {
            ClearOpponentAttackPlan();

            if (_state == null || _state.IsPlayerTurn)
                return;
            if (GameUI.IsBattleMatchEnded())
                return;
            if (IsAwaitingGameStartSequence())
                return;

            SideState side = _state.ActiveSide;
            if (side.Hand.Count <= 0)
                return;

            var options = BuildAttackSkillOptionsForSide(false);
            BattleAttackPreview.FindBestPlayAndAttack(_state, side, false, options, out List<int> indices, out int gen, out int sk, out string nm);

            if (indices == null || indices.Count == 0)
                return;

            foreach (int hi in indices.OrderByDescending(i => i))
            {
                side.PlayedThisPhase.Add(side.Hand[hi]);
                side.Hand.RemoveAt(hi);
            }

            _opponentPlannedAttackGeneral = gen;
            _opponentPlannedAttackSkill = sk;
            _opponentPlannedAttackSkillName = string.IsNullOrWhiteSpace(nm) ? "\u901a\u7528\u653b\u51fb" : nm;
        }

        private static void AutoSelectAttackSkill(bool attackerIsPlayer, Action onAfterDeclareBanner = null)
        {
            if (!attackerIsPlayer && _opponentPlannedAttackGeneral != int.MinValue)
            {
                MaybeAutoPickAttackPatternVariantBeforeSelection(_opponentPlannedAttackGeneral, _opponentPlannedAttackSkill, false);
                SetAttackSkillSelection(_opponentPlannedAttackGeneral, _opponentPlannedAttackSkill, _opponentPlannedAttackSkillName, onAfterDeclareBanner);
                ClearOpponentAttackPlan();
                return;
            }

            if (TryFindTaggedSkill(attackerIsPlayer, "\u653b\u51fb\u6280", out int generalIndex, out int skillIndex, out string skillName))
            {
                MaybeAutoPickAttackPatternVariantBeforeSelection(generalIndex, skillIndex, attackerIsPlayer);
                SetAttackSkillSelection(generalIndex, skillIndex, skillName, onAfterDeclareBanner);
                return;
            }

            SetAttackSkillSelection(-1, -1, "\u901a\u7528\u653b\u51fb", onAfterDeclareBanner);
        }

        private static void MaybeAutoPickAttackPatternVariantBeforeSelection(int generalIndex, int skillIndex, bool attackerIsPlayer)
        {
            if (_state == null || generalIndex < 0)
                return;

            var side = _state.GetSide(attackerIsPlayer);
            if (generalIndex >= side.GeneralCardIds.Count)
                return;

            string cardId = side.GeneralCardIds[generalIndex] ?? string.Empty;
            string skillKey = SkillRuleHelper.MakeSkillKey(cardId, skillIndex);
            if (string.Equals(skillKey, "NO002_0", StringComparison.Ordinal))
                OfflineSkillEngine.AutoPickCeMaPatternVariant(_state, _state.ActiveSide.PlayedThisPhase);
            else if (string.Equals(skillKey, "NO005_0", StringComparison.Ordinal))
                OfflineSkillEngine.AutoPickYuanShuPatternVariant(_state, _state.ActiveSide.PlayedThisPhase);
        }

        private static void OfferHuBuGuanYouThenContinueMainEnd(Action continueAfter)
        {
            if (_state == null)
            {
                continueAfter?.Invoke();
                return;
            }

            if (_state.HuBuGuanYouWindowConsumedForCurrentAttack)
            {
                continueAfter?.Invoke();
                return;
            }

            bool atkIsPlayer = _state.IsPlayerTurn;
            if (!OfflineSkillEngine.ShouldOfferHuBuGuanYouBeforeDefense(_state, atkIsPlayer) || !atkIsPlayer || GameUI.IsOnlineBattle())
            {
                _state.HuBuGuanYouWindowConsumedForCurrentAttack = true;
                continueAfter?.Invoke();
                return;
            }

            GameUI.BeginHuBuGuanYouOffer(() =>
            {
                if (_state != null)
                    _state.HuBuGuanYouWindowConsumedForCurrentAttack = true;
                continueAfter?.Invoke();
                GameUI.NotifyPhaseChanged();
            });
        }

        private static void AutoSelectDefenseSkill(bool defenderIsPlayer)
        {
            if (_state != null && _state.PendingIgnoreDefenseReduction)
            {
                _state.PendingDefenseGeneralIndex = -1;
                _state.PendingDefenseSkillIndex = -1;
                _state.PendingDefenseSkillName = string.Empty;
                _state.PendingDefenseReduction = 0;
                BattleFlowLog.Add(
                    FlowTurnBracket(_state.IsPlayerTurn) + "\u9632\u5fa1\u9636\u6bb5\uff08\u81ea\u52a8\uff09\uff0c" + FlowDefenderActor(defenderIsPlayer) + "\u672c\u6b21\u4f24\u5bb3\u4e0d\u53ef\u9632\u5fa1\uff0c\u672a\u9009\u7528\u9632\u5fa1\u6280\u3002");
                return;
            }

            if (TryFindTaggedSkill(defenderIsPlayer, "防御技", out int generalIndex, out int skillIndex, out string skillName))
            {
                _state.PendingDefenseGeneralIndex = generalIndex;
                _state.PendingDefenseSkillIndex = skillIndex;
                _state.PendingDefenseSkillName = skillName;
                _state.PendingDefenseReduction = 0;
                _state.PendingDefenseSkillKind = SelectedSkillKind.GeneralSkill;
                bool deferDefenseDeclare = OfflineSkillEngine.ConfigureDefenseSkill(_state, defenderIsPlayer, generalIndex, skillIndex);
                if (_state.PendingDefenseReduction <= 0)
                    _state.PendingDefenseReduction = 1;
                if (!deferDefenseDeclare)
                {
                    BattleFlowLog.Add(
                        FlowTurnBracket(_state.IsPlayerTurn) + "\u9632\u5fa1\u9636\u6bb5\uff08\u81ea\u52a8\uff09\uff0c" + FlowDefenderActor(defenderIsPlayer) + "\u9009\u7528\u9632\u5fa1\u6280\u3010" + skillName + "\u3011\uff0c\u767b\u8bb0\u51cf\u4f24" + _state.PendingDefenseReduction + "\u3002");
                    TryShowDefenseDeclareBanner(defenderIsPlayer, generalIndex, skillIndex);
                }
            }
            else
            {
                BattleFlowLog.Add(
                    FlowTurnBracket(_state.IsPlayerTurn) + "\u9632\u5fa1\u9636\u6bb5\uff08\u81ea\u52a8\uff09\uff0c" + FlowDefenderActor(defenderIsPlayer) + "\u672a\u5339\u914d\u9632\u5fa1\u6280\uff08\u6309\u672a\u9632\u5fa1\u7ed3\u7b97\uff09\u3002");
            }
        }

        private static void SetAttackSkillSelection(int generalIndex, int skillIndex, string skillName, Action onAfterDeclareBanner = null)
        {
            if (_state == null)
                return;

            if (generalIndex >= 0)
            {
                _state.PendingGenericAttackOptionIndex = -1;
                _state.PendingGenericAttackShapeChoicePending = false;
                _state.PendingGenericAttackShapeDisplayName = string.Empty;
            }

            _state.PendingAttackGeneralIndex = generalIndex;
            _state.PendingAttackSkillIndex = skillIndex;
            _state.PendingAttackSkillName = string.IsNullOrWhiteSpace(skillName) ? "\u901a\u7528\u653b\u51fb" : skillName;
            _state.PendingAttackSkillKind = generalIndex >= 0 ? SelectedSkillKind.GeneralSkill : SelectedSkillKind.GenericAttack;

            OfflineSkillEngine.ConfigureAttackSkill(_state, _state.IsPlayerTurn, generalIndex, skillIndex, () =>
            {
                if (_state == null || _state.PendingGenericAttackShapeChoicePending)
                    return;
                LogAttackSkillConfigured();
                NotifyActiveSideHandEmptyPassivesIfMatchActive();
                onAfterDeclareBanner?.Invoke();
            });

            if (_state.PendingGenericAttackShapeChoicePending)
                return;
        }

        private static bool TryFindTaggedSkill(bool sideIsPlayer, string tag, out int generalIndex, out int skillIndex, out string skillName)
        {
            generalIndex = -1;
            skillIndex = -1;
            skillName = string.Empty;

            if (_state == null)
                return false;

            SideState side = sideIsPlayer ? _state.Player : _state.Opponent;
            for (int cardIndex = 0; cardIndex < side.GeneralCardIds.Count; cardIndex++)
            {
                if (!side.IsGeneralFaceUp(cardIndex))
                    continue;

                CardData data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(side.GeneralCardIds[cardIndex]));
                if (data == null)
                    continue;

                for (int currentSkillIndex = 0; currentSkillIndex < 3; currentSkillIndex++)
                {
                    if (!SkillHasTag(data, currentSkillIndex, tag))
                        continue;

                    string key = (sideIsPlayer ? "True" : "False") + "_" + cardIndex + "_" + currentSkillIndex;
                    if (SkillHasTag(data, currentSkillIndex, "破军技") && side.UsedOneShotSkills.Contains(key))
                        continue;

                    generalIndex = cardIndex;
                    skillIndex = currentSkillIndex;
                    skillName = GetSkillName(data, currentSkillIndex);
                    return true;
                }
            }

            return false;
        }

        private static bool SkillHasTag(CardData data, int skillIndex, string tag)
        {
            if (data == null || string.IsNullOrWhiteSpace(tag))
                return false;

            if (!string.IsNullOrWhiteSpace(data.CardId) && SkillRuleLoader.HasTag(data.CardId, skillIndex, tag))
                return true;

            List<string> tags = skillIndex switch
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
                if (string.Equals(tags[i], tag, System.StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        private static string GetSkillName(CardData data, int skillIndex)
        {
            return skillIndex switch
            {
                0 => data.SkillName1 ?? string.Empty,
                1 => data.SkillName2 ?? string.Empty,
                2 => data.SkillName3 ?? string.Empty,
                _ => string.Empty
            };
        }

        private static void Log(string message)
        {
            Debug.Log(LogPrefix + " " + message);
        }

        private static string FlowTurnBracket(bool isPlayerTurn) =>
            isPlayerTurn ? "\u3010\u5df1\u65b9\u56de\u5408\u3011" : "\u3010\u654c\u65b9\u56de\u5408\u3011";

        /// <summary>供战报/UI 与 <see cref="FlowTurnBracket"/> 一致使用。</summary>
        public static string FormatFlowTurnBracketForBattleLog(bool isPlayerTurn) => FlowTurnBracket(isPlayerTurn);

        private static string FlowAttackerActor(bool attackerIsPlayer) =>
            attackerIsPlayer ? "\u5df1\u65b9\u73a9\u5bb6" : "\u654c\u65b9";

        private static string FlowDefenderActor(bool defenderIsPlayer) =>
            defenderIsPlayer ? "\u5df1\u65b9\u73a9\u5bb6" : "\u654c\u65b9";

        private static void LogGameStartSkillLinesThen(Action done)
        {
            if (_state == null)
            {
                done?.Invoke();
                return;
            }

            List<GameStartSkillLineEntry> entries = GameStartSkillNodeFlow.BuildSortedEntries(_state);
            GameStartSkillNodeFlow.RunSequence(entries, done);
        }

        private static string GetAttackSkillRuleDescriptionFromCard(BattleState st)
        {
            if (st == null)
                return string.Empty;

            int g = st.PendingAttackGeneralIndex;
            int s = st.PendingAttackSkillIndex;
            if (g < 0 || s < 0 || s > 2)
                return string.Empty;

            SideState side = st.ActiveSide;
            if (g >= side.GeneralCardIds.Count)
                return string.Empty;

            CardData data = CardTableLoader.GetCard(CardTableLoader.CardIdToNumber(side.GeneralCardIds[g]));
            if (data == null)
                return string.Empty;

            return s switch
            {
                0 => data.SkillDesc1 ?? string.Empty,
                1 => data.SkillDesc2 ?? string.Empty,
                2 => data.SkillDesc3 ?? string.Empty,
                _ => string.Empty,
            };
        }

        public static void LogAttackSkillConfigured()
        {
            if (_state == null)
                return;

            bool atkPlayer = _state.IsPlayerTurn;
            string turn = FlowTurnBracket(atkPlayer);
            string actor = FlowAttackerActor(atkPlayer);
            var cards = _state.ActiveSide.PlayedThisPhase;
            int announced = Mathf.Max(0, _state.PendingBaseDamage + _state.PendingAttackBonus);

            var line = new StringBuilder();
            line.Append(turn).Append("\u51fa\u724c\u9636\u6bb5\uff0c").Append(actor);

            if (_state.PendingAttackSkillKind == SelectedSkillKind.GenericAttack)
            {
                string shape = GenericAttackShapes.DescribeShapeForLog(_state, cards);
                line.Append("\u4f7f\u7528\u901a\u7528\u653b\u51fb\u6280\uff0c\u724c\u578b\u4e3a\u3010").Append(shape).Append("\u3011\uff0c\u5c06\u8981")
                    .Append(announced).Append("\u70b9\u901a\u7528\u4f24\u5bb3");
            }
            else
            {
                string skillDisp = string.IsNullOrWhiteSpace(_state.PendingAttackSkillName) ? "\u653b\u51fb\u6280" : _state.PendingAttackSkillName;
                line.Append("\u4f7f\u7528\u653b\u51fb\u6280\u3010").Append(skillDisp).Append("\u3011\u3002");
                string ruleDesc = GetAttackSkillRuleDescriptionFromCard(_state);
                if (!string.IsNullOrWhiteSpace(ruleDesc))
                    line.Append(ruleDesc.Trim());
                else
                    line.Append("\u5c06\u8981\u9020\u6210").Append(announced).Append("\u70b9").Append(DamageTypeLabels.DamageTypeNameForAmountLine(_state.PendingDamageCategory == DamageCategory.None ? DamageCategory.Generic : _state.PendingDamageCategory, _state.PendingDamageElement));
                if (_state.PendingIgnoreDefenseReduction)
                    line.Append("\uff08\u65e0\u89c6\u9632\u5fa1\u51cf\u514d\uff09");
            }

            if (_state.PendingPostResolveDrawToAttacker > 0)
                line.Append("\uff0c\u5e76\u6478").Append(_state.PendingPostResolveDrawToAttacker).Append("\u5f20\u724c");
            if (_state.PendingPostResolveHealToAttacker > 0)
                line.Append("\uff0c\u7ed3\u7b97\u540e\u6062\u590d").Append(_state.PendingPostResolveHealToAttacker).Append("\u70b9\u751f\u547d");

            if (!string.IsNullOrWhiteSpace(_state.PendingCombatNote))
                line.Append("\uff08").Append(_state.PendingCombatNote.Trim().Replace("\n", "\uff1b")).Append("\uff09");

            line.Append("\u3002");
            BattleFlowLog.Add(line.ToString());
        }

        private static string DescribeState()
        {
            if (_state == null)
                return "state=null";

            SideState activeSide = _state.ActiveSide;
            return "turn=" + _state.TurnNumber +
                   ", active=" + DescribeSide(_state.IsPlayerTurn) +
                   ", phase=" + _state.CurrentPhase +
                   ", step=" + _state.CurrentPhaseStep +
                   ", playPhase=" + (_state.CurrentPlayPhaseIndex + 1) + "/" + _state.TotalPlayPhasesThisTurn +
                   ", hand=" + activeSide.Hand.Count +
                   ", played=" + activeSide.PlayedThisPhase.Count +
                   ", morale=" + activeSide.Morale +
                   ", hp=" + activeSide.CurrentHp + "/" + activeSide.MaxHp;
        }

        private static string DescribeSide(bool isPlayerSide)
        {
            return isPlayerSide ? "Player" : "Opponent";
        }
    }
}
