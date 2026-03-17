using System;
using UnityEngine;

namespace JunzhenDuijue
{
    /// <summary>
    /// 驱动回合阶段：准备→收入→出牌→弃牌→回合结束。每阶段有 Start/Main/End 三步，供技能等挂接。
    /// </summary>
    public static class BattlePhaseManager
    {
        private static BattleState _state;

        /// <summary>
        /// 准备阶段-开始时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnPreparationStart;
        /// <summary>
        /// 准备阶段-效果触发时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnPreparationMain;
        /// <summary>
        /// 准备阶段-结束时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnPreparationEnd;
        /// <summary>
        /// 收入阶段-开始时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnIncomeStart;
        /// <summary>
        /// 收入阶段-效果触发时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnIncomeMain;
        /// <summary>
        /// 收入阶段-结束时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnIncomeEnd;
        /// <summary>
        /// 弃牌阶段-开始时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnDiscardStart;
        /// <summary>
        /// 弃牌阶段-效果触发时：若手牌>上限则请求弹出弃牌选择；由 UI 在选完后调用 NotifyDiscardPhaseDone。
        /// </summary>
        public static event Action<bool, int> OnDiscardMain;
        /// <summary>
        /// 弃牌阶段-结束时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnDiscardEnd;
        /// <summary>
        /// 主要阶段-开始时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnPrimaryStart;
        /// <summary>
        /// 主要阶段-效果触发时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnPrimaryMain;
        /// <summary>
        /// 主要阶段-结束时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnPrimaryEnd;
        /// <summary>
        /// 出牌阶段-开始时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnPlayPhaseStart;
        /// <summary>
        /// 出牌阶段-效果触发时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnPlayPhaseMain;
        /// <summary>
        /// 出牌阶段-结束时（预留技能接口）。
        /// </summary>
        public static event Action<bool> OnPlayPhaseEnd;

        public static void Bind(BattleState state)
        {
            _state = state;
        }

        /// <summary>
        /// 游戏开始：双方各摸 6 张，然后进入准备阶段。
        /// </summary>
        public static void OnGameStart()
        {
            if (_state == null) return;
            BattleState.Draw(_state.Player, 6);
            BattleState.Draw(_state.Opponent, 6);
            _state.CurrentPhase = BattlePhase.Preparation;
            _state.CurrentPhaseStep = PhaseStep.Start;
            RunPhaseStep();
        }

        /// <summary>
        /// 执行当前阶段当前步骤，并自动进入下一步或下一阶段。
        /// </summary>
        private static void RunPhaseStep()
        {
            if (_state == null) return;
            bool isPlayer = _state.IsPlayerTurn;

            switch (_state.CurrentPhase)
            {
                case BattlePhase.Preparation:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnPreparationStart?.Invoke(isPlayer);
                        _state.CurrentPhaseStep = PhaseStep.Main;
                        RunPhaseStep();
                        return;
                    }
                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        OnPreparationMain?.Invoke(isPlayer);
                        _state.CurrentPhaseStep = PhaseStep.End;
                        RunPhaseStep();
                        return;
                    }
                    OnPreparationEnd?.Invoke(isPlayer);
                    _state.CurrentPhase = BattlePhase.Income;
                    _state.CurrentPhaseStep = PhaseStep.Start;
                    RunPhaseStep();
                    return;

                case BattlePhase.Income:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnIncomeStart?.Invoke(isPlayer);
                        _state.CurrentPhaseStep = PhaseStep.Main;
                        RunPhaseStep();
                        return;
                    }
                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        OnIncomeMain?.Invoke(isPlayer);
                        var side = _state.ActiveSide;
                        BattleState.Draw(side, 3);
                        side.Morale = Mathf.Min(BattleState.MaxMorale, side.Morale + 1);
                        _state.CurrentPhaseStep = PhaseStep.End;
                        RunPhaseStep();
                        return;
                    }
                    OnIncomeEnd?.Invoke(isPlayer);
                    _state.CurrentPhase = BattlePhase.Primary;
                    _state.CurrentPhaseStep = PhaseStep.Start;
                    RunPhaseStep();
                    return;

                case BattlePhase.Primary:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnPrimaryStart?.Invoke(isPlayer);
                        _state.CurrentPhaseStep = PhaseStep.Main;
                        GameUI.NotifyPhaseChanged();
                        return;
                    }
                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        OnPrimaryMain?.Invoke(isPlayer);
                        GameUI.NotifyPhaseChanged();
                        return;
                    }
                    OnPrimaryEnd?.Invoke(isPlayer);
                    _state.CurrentPlayPhaseIndex = 0;
                    _state.CurrentPhase = BattlePhase.Main;
                    _state.CurrentPhaseStep = PhaseStep.Start;
                    RunPhaseStep();
                    return;

                case BattlePhase.Main:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnPlayPhaseStart?.Invoke(isPlayer);
                        _state.CurrentPhaseStep = PhaseStep.Main;
                        GameUI.NotifyPhaseChanged();
                        return;
                    }
                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        OnPlayPhaseMain?.Invoke(isPlayer);
                        GameUI.NotifyPhaseChanged();
                        return;
                    }
                    OnPlayPhaseEnd?.Invoke(isPlayer);
                    var activeSide = _state.ActiveSide;
                    foreach (var c in activeSide.PlayedThisPhase)
                        activeSide.DiscardPile.Add(c);
                    activeSide.PlayedThisPhase.Clear();
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
                    return;

                case BattlePhase.Discard:
                    if (_state.CurrentPhaseStep == PhaseStep.Start)
                    {
                        OnDiscardStart?.Invoke(isPlayer);
                        _state.CurrentPhaseStep = PhaseStep.Main;
                        RunPhaseStep();
                        return;
                    }
                    if (_state.CurrentPhaseStep == PhaseStep.Main)
                    {
                        var side = _state.ActiveSide;
                        int over = side.Hand.Count - _state.HandLimit;
                        if (over > 0)
                        {
                            OnDiscardMain?.Invoke(isPlayer, over);
                            return;
                        }
                        _state.CurrentPhaseStep = PhaseStep.End;
                        RunPhaseStep();
                        return;
                    }
                    OnDiscardEnd?.Invoke(isPlayer);
                    _state.CurrentPhase = BattlePhase.TurnEnd;
                    _state.CurrentPhaseStep = PhaseStep.Main;
                    RunPhaseStep();
                    return;

                case BattlePhase.TurnEnd:
                    _state.IsPlayerTurn = !_state.IsPlayerTurn;
                    _state.ActiveSide.ResetMoraleUsedThisTurn();
                    _state.TotalPlayPhasesThisTurn = 1;
                    _state.CurrentPlayPhaseIndex = 0;
                    _state.CurrentPhase = BattlePhase.Preparation;
                    _state.CurrentPhaseStep = PhaseStep.Start;

                    if (!_state.IsPlayerTurn && GameUI.IsOpponentTurnAutoEndEnabled())
                    {
                        RunPhaseStep();
                        while (_state != null && !_state.IsPlayerTurn && GameUI.IsOpponentTurnAutoEndEnabled() &&
                               (_state.CurrentPhase == BattlePhase.Primary || _state.CurrentPhase == BattlePhase.Main))
                            EndTurn();
                        return;
                    }
                    RunPhaseStep();
                    GameUI.NotifyPhaseChanged();
                    return;
            }
        }

        /// <summary>
        /// 玩家点击结束按钮：主要阶段时进入主要阶段结束→无技能则到出牌阶段；出牌阶段时进入当前出牌阶段结束→无技能则下一段出牌或弃牌阶段。
        /// </summary>
        public static void EndTurn()
        {
            if (_state == null) return;
            if (_state.CurrentPhase == BattlePhase.Primary && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                _state.CurrentPhaseStep = PhaseStep.End;
                RunPhaseStep();
                return;
            }
            if (_state.CurrentPhase == BattlePhase.Main && _state.CurrentPhaseStep == PhaseStep.Main)
            {
                _state.CurrentPhaseStep = PhaseStep.End;
                RunPhaseStep();
                return;
            }
        }

        /// <summary>
        /// 弃牌阶段中，玩家在 UI 完成弃牌选择并确认后调用；indices 为手牌中要弃掉的索引列表。对手传 null 表示自动弃掉前 N 张。
        /// </summary>
        public static void NotifyDiscardPhaseDone(bool isPlayer, int[] handIndices)
        {
            if (_state == null) return;
            if (_state.CurrentPhase != BattlePhase.Discard || _state.CurrentPhaseStep != PhaseStep.Main)
                return;
            var side = isPlayer ? _state.Player : _state.Opponent;
            int over = side.Hand.Count - _state.HandLimit;
            if (over > 0)
            {
                if (handIndices != null && handIndices.Length == over)
                {
                    var list = new System.Collections.Generic.List<int>(handIndices);
                    BattleState.DiscardFromHand(side, list);
                }
                else if (!isPlayer)
                {
                    var autoList = new System.Collections.Generic.List<int>();
                    for (int i = 0; i < over; i++)
                        autoList.Add(side.Hand.Count - 1 - i);
                    BattleState.DiscardFromHand(side, autoList);
                }
            }
            _state.CurrentPhaseStep = PhaseStep.End;
            RunPhaseStep();
        }

        /// <summary>
        /// 弃牌阶段 Main 中不需要弹窗（手牌已<=上限）或已跳过弹窗时，由 UI 调用以继续。
        /// </summary>
        public static void NotifyDiscardPhaseSkipPopup()
        {
            if (_state == null) return;
            if (_state.CurrentPhase != BattlePhase.Discard || _state.CurrentPhaseStep != PhaseStep.Main)
                return;
            _state.CurrentPhaseStep = PhaseStep.End;
            RunPhaseStep();
        }
    }
}
