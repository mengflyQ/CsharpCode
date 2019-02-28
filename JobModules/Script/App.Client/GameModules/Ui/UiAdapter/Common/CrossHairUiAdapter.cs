﻿using System.Collections.Generic;
using App.Shared;
using App.Shared.Components.Ui;
using App.Shared.GameModules.Weapon;
using App.Shared.WeaponLogic;
using Core.CameraControl.NewMotor;
using Utils.Configuration;
using Utils.Singleton;
using WeaponConfigNs;
using XmlConfig;

namespace App.Client.GameModules.Ui.UiAdapter
{
    public class CrossHairUiAdapter : UIAdapter,ICrossHairUiAdapter
    {
        private Contexts _contexts;
        private PlayerContext _playerContext;
        private UiContext _uiContext;
        public CrossHairUiAdapter(Contexts contexts)
        {
            _contexts = contexts;
            _playerContext = contexts.player;
            _uiContext = contexts.ui;
        }

        //准心组
        private CrossHairType type;
        private bool isOpenCrossHairMotion = true;      //常态类型准心 是否开启了准心运动
        public CrossHairType Type //准心类型
        {
            get
            {
                var type = CrossHairType.Normal;
                if(null != _playerContext.flagSelfEntity)
                {
                    var player = _playerContext.flagSelfEntity;
                    if (CrossShouldBeHidden(player))
                    {
                        type = CrossHairType.Novisible;
                    }
                    else if(FocusTargetIsTeammate())
                    {
                        type = CrossHairType.AddBlood;
                    }
                }

                return type;
            }
        }

        private bool CrossShouldBeHidden(PlayerEntity player)
        {
            if(!player.hasCameraStateNew)
            {
                return false;
            }
            switch((ECameraViewMode)player.cameraStateNew.ViewNowMode)
            {
                case ECameraViewMode.GunSight:
                    return true;
                default:
                    break;
            }
            switch((ECameraPoseMode)player.cameraStateNew.MainNowMode)
            {
                case ECameraPoseMode.AirPlane:
                case ECameraPoseMode.DriveCar:
                case ECameraPoseMode.Dead:
                case ECameraPoseMode.Dying:
                case ECameraPoseMode.Gliding:
                case ECameraPoseMode.Parachuting:
                    return true;
                default:
                    break;
            }
            return false;
        }

        private bool FocusTargetIsTeammate()
        {
            return false;
        }

        public CrossHairNormalTypeStatue Statue //常态类型准心 当前状态
        {   get
            {
                var player = _playerContext.flagSelfEntity;
                if(null == player)
                {
                    return CrossHairNormalTypeStatue.None;
                }
                if(PlayerIsFiring(player))
                {
                    return CrossHairNormalTypeStatue.Shot;
                }
                else if(PlayerIsMoving(player))
                {
                    return CrossHairNormalTypeStatue.Move;
                }
                else
                {
                    return CrossHairNormalTypeStatue.StopShot;
                }
            }
        }

        private bool PlayerIsFiring(PlayerEntity player)
        {
            if(!player.hasPlayerStateProvider)
            {
                return false;
            }
            var states = player.playerStateProvider.Provider.GetCurrentStates();
            return states.Contains(XmlConfig.EPlayerState.MeleeAttacking) ||
                states.Contains(XmlConfig.EPlayerState.Firing);
        }

        private bool PlayerIsMoving(PlayerEntity player)
        {
            if(!player.hasPlayerMove)
            {
                return false;
            }
            return player.playerMove.Velocity.magnitude > 0.01f;
        }


        //攻击组
        public int ShootNum //常态类型准心 射击状态 当前发射的子弹数
        {
            get
            {
                var player = _playerContext.flagSelfEntity;
                if(null == player)
                {
                    return 0;
                }
                var weaponData = player.GetWeaponRunTimeInfo(_contexts);
                return weaponData.ContinuesShootCount;
            }
        }
        public float AttackNum
        {
            get    
            {
                var player = _playerContext.flagSelfEntity;
                if (null == player)
                {
                    return 0;
                }
                if (!player.hasAttackDamage)
                {
                    return 0;
                }
                var damage = player.attackDamage.GetAndResetDamage();
                return damage;
            }            
        }

        private bool isBurstHeart;  //爆头
        public bool IsBurstHeart
        {
            get
            {
                var player = _playerContext.flagSelfEntity;
                if(null == player)
                {
                    return false;
                }
                if(!player.hasAttackDamage)
                {
                    return false;
                }
                var part = player.attackDamage.GetAndResetHitPart();
                var critical = (EBodyPart)part == EBodyPart.Head;
                return critical;
            }
        }
      
        public bool IsOpenCrossHairMotion
        {
            get
            {
                return isOpenCrossHairMotion;
            }

            set
            {
                isOpenCrossHairMotion = value;
            }
        }

        public int WeaponAvatarId
        {
            get
            {
                var player = _contexts.player.flagSelfEntity;
                if (player.HasWeapon(_contexts))
                {
                    var weaponInfo = player.GetController<PlayerWeaponController>().CurrSlotWeaponInfo(_contexts); 
                    var avatarId = weaponInfo.AvatarId;
                    if(avatarId > 0)
                    {
                        return avatarId;
                    }
                    var defaultAvatar = weaponInfo.Id;
                    if(defaultAvatar > 0)
                    {
                        return defaultAvatar;
                    }
                }
                return SingletonManager.Get<WeaponAvatarConfigManager>().GetEmptyHandId();
            }
        }      

        public bool IsShowCrossHair
        {
            get { return _uiContext.uI.IsShowCrossHair; }
        }
    }
}