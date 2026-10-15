using System;
using GameJam.InGame.Player;

namespace GameJam.InGame.Monster
{
    // Keeps moving toward the player's center.
    public class GJMonsterStateChase : IGJMonsterState
    {
        private readonly GJMonster _monster;
        private readonly GJPlayerComponent _playerComponent;

        public GJMonsterStateChase(GJMonster monster, GJPlayerComponent playerComponent)
        {
            _monster = monster;
            _playerComponent = playerComponent;
        }

        public void OnEnter()
        {
        }

        public void OnExit()
        {
        }

        public void UpdateState(TimeSpan deltaTime)
        {
            _monster.MoveToward(_playerComponent.ColliderBounds.center, deltaTime);
        }

        public bool TryGetNextState(out IGJMonsterState nextState)
        {
            nextState = null;
            return false;
        }
    }
}
