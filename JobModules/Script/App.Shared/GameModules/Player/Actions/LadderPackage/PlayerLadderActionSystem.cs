using App.Shared.Components.Player;
using App.Shared.GameModules.Player.CharacterState;
using App.Shared.Player;
using Core.GameModule.Interface;
using Core.Prediction.UserPrediction.Cmd;

namespace App.Shared.GameModules.Player.Actions.LadderPackage
{
    public class PlayerLadderActionSystem : IUserCmdExecuteSystem
    {
        private readonly FsmOutputBaseSystem _fsmOutputs = new FsmOutputBaseSystem();
        public void ExecuteUserCmd(IUserCmdOwner owner, IUserCmd cmd)
        {
            var player = (PlayerEntity)owner.OwnerEntity;
            if(null == player || null == player.RootGo() || !player.hasLadderActionInterface) return;
            
            CheckPlayerLifeState(player);
            
            var ladderAction = player.ladderActionInterface.LadderAction;
            ladderAction.Update(player, cmd.MoveVertical);
            
            _fsmOutputs.ResetOutput();
            ladderAction.Execute(player, _fsmOutputs.AddOutput);
            _fsmOutputs.SetOutput(player);
        }
        
        #region LifeState

        private void CheckPlayerLifeState(PlayerEntity player)
        {
            if (null == player || null == player.playerGameState) return;
            var gameState = player.playerGameState;
            switch (gameState.CurrentPlayerLifeState)
            {
                case PlayerLifeStateEnum.Reborn:
                    Reborn(player);
                    break;
                case PlayerLifeStateEnum.Dead:
                    Dead(player);
                    break;
            }
        }

        private void Reborn(PlayerEntity player)
        {
            if (null == player) return;
            var ladderAction = player.ladderActionInterface.LadderAction;
            if (null == ladderAction) return;
            ladderAction.PlayerReborn(player);
        }
        
        private void Dead(PlayerEntity player)
        {
            if (null == player) return;
            var ladderAction = player.ladderActionInterface.LadderAction;
            if (null == ladderAction) return;
            ladderAction.PlayerDead(player);
        }

        #endregion
    }
}