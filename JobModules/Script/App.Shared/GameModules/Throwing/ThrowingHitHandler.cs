﻿using App.Shared.GameModules.Bullet;
using App.Shared.GameModules.Vehicle;
using Core.Prediction.VehiclePrediction.Cmd;
using Core.Utils;

namespace App.Shared.GameModules.Throwing
{
    public interface IThrowingHitHandler
    {
        void OnPlayerDamage(PlayerEntity sourcePlayer, PlayerEntity targetPlayer, PlayerDamageInfo damage);
        void OnVehicleDamage(VehicleEntity vehicle, float damage);
    }

    public class ThrowingHitHandler : IThrowingHitHandler
    {
        private static readonly LoggerAdapter Logger = new LoggerAdapter(typeof(ThrowingHitHandler));
        private IPlayerDamager _damager;

        public ThrowingHitHandler(IPlayerDamager damager)
        {
            _damager = damager;
        }

        public void OnPlayerDamage(PlayerEntity sourcePlayer, PlayerEntity targetPlayer, PlayerDamageInfo damage)
        {
            BulletPlayerUtility.ProcessPlayerHealthDamage(_damager, sourcePlayer, targetPlayer, damage);
        }

        public void OnVehicleDamage(VehicleEntity vehicle, float damage)
        {
            var gameData = vehicle.GetGameData();
            gameData.DecreaseHp(VehiclePartIndex.Body, damage);
        }
    }
}