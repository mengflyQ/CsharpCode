﻿using System;
using Core.Components;
using Core.EntityComponent;
using Core.SnapshotReplication.Serialization.NetworkProperty;
using Core.SyncLatest;
using Core.Utils;
using Entitas.CodeGeneration.Attributes;
using Core.Playback;

namespace App.Shared.Components.Player
{
    public enum EPlayerLifeState
    {
        Alive = 0,
        Dying = 1,
        Dead = 2,
    }

    public enum EVehicleCollisionState
    {
        None = 0,
        Enter = 1,
        Stay = 2,
    }
    

    [Player]
    
    public class GamePlayComponent : ISelfLatestComponent, IPlaybackComponent
    {
        public static string[] LifeStateString = new[]
        {
            "Alive",
            "Dying",
            "Dead"
        };
        private static readonly LoggerAdapter Logger = new LoggerAdapter(typeof(GamePlayComponent));

        //最大血量有需求再同步
        [NetworkProperty] public int MaxHp;

        [NetworkProperty] public float CurHp;

        [DontInitilize] [NetworkProperty] public float InHurtedHp;
        [DontInitilize] [NetworkProperty] public int RecoverHp;
        [DontInitilize] [NetworkProperty] public int InHurtedCount;
        [DontInitilize] [NetworkProperty] public int BuffRemainTime;

        [DontInitilize] public int LastLifeState; // PlayerLifeState

        [DontInitilize] [NetworkProperty] public int LifeState; // PlayerLifeState

        [DontInitilize] [NetworkProperty] public int LifeStateChangeTime; // ServerTime

        [DontInitilize] public EVehicleCollisionState VehicleCollisionState;
        [DontInitilize] public EntityKey CollidedVehicleKey;

        [DontInitilize] [NetworkProperty] public int GameState;
        [DontInitilize] public int ClientState;

        [DontInitilize] [NetworkProperty] public int PlayerState;
        [DontInitilize] [NetworkProperty] public int UIState;
        [DontInitilize] [NetworkProperty] public int CastState;

        [DontInitilize] [NetworkProperty] public int CameraEntityId;

        [DontInitilize] [NetworkProperty] public int Energy;
        [DontInitilize] [NetworkProperty] public int BagWeight;

        [DontInitilize] [NetworkProperty] public int CurArmor;
        [DontInitilize] [NetworkProperty] public int MaxArmor;
        [DontInitilize] [NetworkProperty] public int CurHelmet;
        [DontInitilize] [NetworkProperty] public int MaxHelmet;
        [DontInitilize] [NetworkProperty] public int ArmorLv;
        [DontInitilize] [NetworkProperty] public int HelmetLv;

        [DontInitilize] [NetworkProperty] public bool IsSave;
        [DontInitilize] [NetworkProperty] public bool IsBeSave;
        [DontInitilize] [NetworkProperty] public int SaveTime;
        [DontInitilize] [NetworkProperty] public EntityKey SavePlayerKey;
        [DontInitilize] [NetworkProperty] public int SaveEnterState;
        [DontInitilize] [NetworkProperty] public bool IsInteruptSave;

        public void ChangeLifeState(EPlayerLifeState state, int time)
        {
            if (LifeState != (int) state)
            {
                LastLifeState = LifeState;
                LifeState = (int) state;
                LifeStateChangeTime = time;
                if (state == EPlayerLifeState.Dying)
                {
                    InHurtedHp = 100;
                    InHurtedCount ++;
                    BuffRemainTime = 0;
                }
            }
        }

        public string GetLifeStateString()
        {
            return LifeStateString[LifeState];
        }

        /// <summary>
        /// 更新状态后需要调用
        /// </summary>
        public void ClearLifeStateChangedFlag()
        {
            LastLifeState = LifeState;
        }

        public bool IsLifeChangeOlderThan(int time)
        {
            return LifeStateChangeTime < time;
        }
        public bool IsLifeState(EPlayerLifeState state)
        {
            return LifeState == (int) state;
        }

        public bool IsLastLifeState(EPlayerLifeState state)
        {
            return LastLifeState == (int)state;
        }
        
        public bool HasLifeStateChangedFlag()
        {
             return LastLifeState != LifeState; 
        }

        public bool IsVehicleCollisionState(EVehicleCollisionState state)
        {
            return VehicleCollisionState == state;
        }

        public bool IsDead()
        {
            return LifeState == (int) EPlayerLifeState.Dead;
        }

        public bool IsHitDown()
        {
            return LifeState == (int) EPlayerLifeState.Dying && LastLifeState == (int) EPlayerLifeState.Alive;
        }

        public float CurModeHp
        {
            get
            {
                if (IsLifeState(EPlayerLifeState.Dying))
                    return InHurtedHp;
                else
                    return CurHp;
            }
        }

        public int GetComponentId()
        {
            return (int) EComponentIds.PlayerGamePlay;
        }

        public void CopyFrom(object target)
        {
            var hp = target as GamePlayComponent;
            CurHp = hp.CurHp;
            LifeState = hp.LifeState;
            LifeStateChangeTime = hp.LifeStateChangeTime;
            MaxHp = hp.MaxHp;
            GameState = hp.GameState;
            InHurtedHp = hp.InHurtedHp;
            RecoverHp = hp.RecoverHp;
            InHurtedCount = hp.InHurtedCount;
            BuffRemainTime = hp.BuffRemainTime;
            PlayerState = hp.PlayerState;
            UIState = hp.UIState;
            CastState = hp.CastState;
            Energy = hp.Energy;
            CameraEntityId = hp.CameraEntityId;

            ArmorLv = hp.ArmorLv;
            HelmetLv = hp.HelmetLv;
            CurArmor = hp.CurArmor;
            MaxArmor = hp.MaxArmor;
            CurHelmet = hp.CurHelmet;
            MaxHelmet = hp.MaxHelmet;
            IsSave = hp.IsSave;
            IsBeSave = hp.IsBeSave;
            SaveTime = hp.SaveTime;
            SavePlayerKey = hp.SavePlayerKey;
            SaveEnterState = hp.SaveEnterState;
            IsInteruptSave = hp.IsInteruptSave;
        }

        public float DecreaseHp(float damage, int time = 0)
        {
            float ret = 0;
            float oldHp = 0;
            if (IsLifeState(EPlayerLifeState.Alive))
            {
                oldHp = CurHp;
                CurHp = Math.Max(CurHp - damage, 0);
                ret = oldHp - CurHp;
            }
            else if (IsLifeState(EPlayerLifeState.Dying))
            {
                oldHp = InHurtedHp;
                InHurtedHp = Math.Max(InHurtedHp - damage, 0);
                ret = oldHp - InHurtedHp;
            }
            return ret;
        }
        public bool IsInterpolateEveryFrame(){ return false; }
        public void Interpolate(object left, object right, IInterpolationInfo interpolationInfo)
        {
            CopyFrom(left);
            GamePlayComponent leftGamePlay = (GamePlayComponent)left;
            GamePlayComponent rightGamePlay = (GamePlayComponent)right;
            // 复活的时候延时赋值，其他情况立即赋值
            if(leftGamePlay.LifeState != rightGamePlay.LifeState && rightGamePlay.LifeState != (int)EPlayerLifeState.Alive)
            {
                this.LifeState = rightGamePlay.LifeState;
                this.LastLifeState = rightGamePlay.LastLifeState;
                this.CurHp = rightGamePlay.CurHp;
            }
        }

        public void SyncLatestFrom(object rightComponent)
        {
            CopyFrom(rightComponent);
        }

      
    }
}