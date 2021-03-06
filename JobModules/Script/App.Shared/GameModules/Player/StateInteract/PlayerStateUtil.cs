﻿using App.Shared.Components.Player;
using XmlConfig;

namespace App.Shared.GameModules.Player
{
    public static class PlayerStateUtil
    {
        /// <summary>
        /// 打开UI时
        /// </summary>
        /// <param name="state"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool HasUIState(EPlayerUIState state, GamePlayComponent player)
        {
            return (player.UIState & (1 << (int)state)) > 0;
        }

        public static void AddUIState(EPlayerUIState state, GamePlayComponent player)
        {
            player.UIState |= 1 << (int)state;
            player.UIStateUpdate = true;
        }

        public static bool HasUIState(GamePlayComponent player)
        {
            return HasUIState(EPlayerUIState.BagOpen, player) || HasUIState(EPlayerUIState.ExitOpen, player) ||
                   HasUIState(EPlayerUIState.MapOpen, player) || HasUIState(EPlayerUIState.ChatOpen, player);
        }
        
        public static void RemoveUIState(EPlayerUIState state, GamePlayComponent player)
        {
            player.UIState &= ~(1 << (int)state);
            player.UIStateUpdate = true;
        }
        
        public static bool HasPlayerState(EPlayerGameState state, GamePlayComponent player)
        {
            return (player.PlayerState & (1 << (int)state)) > 0;
        }

        public static void AddPlayerState(EPlayerGameState state, GamePlayComponent player)
        {
            player.PlayerState |= 1 << (int)state;
        }

        public static void RemoveGameState(EPlayerGameState state, GamePlayComponent player)
        {
            player.PlayerState &= ~(1 << (int)state);
        }

        public static bool HasCastState(EPlayerCastState state, GamePlayComponent player)
        {
            return (player.CastState & (1 << (int)state)) > 0;
        }

        public static void AddCastState(EPlayerCastState state, GamePlayComponent player)
        {
            player.CastState |= 1 << (int)state;
        }

        public static void RemoveCastState(EPlayerCastState state, GamePlayComponent player)
        {
            player.CastState &= ~(1 << (int)state);
        }
        public static EPlayerInput ToEPlayerInput(this InterruptConfigType interrupt)
        {
            switch (interrupt)
            {
                case InterruptConfigType.PullboltInterrupt:
                    return EPlayerInput.IsPullboltInterrupt;
                case InterruptConfigType.ThrowInterrupt:
                    return EPlayerInput.IsThrowingInterrupt;
                default: return EPlayerInput.None;
            }
        }
    }

    public enum EPlayerUIState
    {
        BagOpen = 1,
        MapOpen = 2,
        ExitOpen = 3,
        PaintOpen = 4,
        ChatOpen = 5
    }

    public enum EPlayerGameState
    {
        SpeedUp = 1,
        CanJump = 2,
        HasArmor = 3,
        HasHelmet = 4,
        DivingChok = 5,
        NotMove = 6,
        InterruptItem=7,
        Invincible = 8,
        
        PlayerReborn,
        PlayerRevive,
        PlayerDead,
        PlayerDying,
        TurnOver,
        TurnStart,
        OnPlane,
        CanDefuse,
        UseItem
    }

    public enum EPlayerCastState
    {
        C4Pickable = 1,
        C4Defuse = 2,
    }
}
