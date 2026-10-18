using System;
//using UnityEngine;
using GameJam.InGame.Player;
using System.Threading;

namespace GameJam.InGame.Monster
{
    // Keeps moving toward the player's center.
    public class GJMonsterStateChase : IGJMonsterState
    {
        private readonly GJMonster _monster;
        public readonly GJPlayerComponent _playerComponent;
        float ShootTimer = 1f;

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
            //_monster.MoveToward(_playerComponent.ColliderBounds.center, deltaTime);

            if (ShootTimer > 0)
            {
                ShootTimer -= (float)deltaTime.TotalSeconds;
            } else
            {
                ShootTimer = 1f + UnityEngine.Random.Range(1f, 3f);
                _monster.SpawnProjectile(_playerComponent.ColliderBounds.center, deltaTime);
            }   
        }

        public bool TryGetNextState(out IGJMonsterState nextState)
        {
            nextState = null;
            return false;
        }

        void Update()
        {
             
        }
    }
}
