using System;
using UnityEngine;
using GameJam.InGame.Player;
using System.Threading;
using System.Collections;
using UnityEditor.Rendering.LookDev;
using UnityEngine.Splines;
using System.Threading.Tasks;

namespace GameJam.InGame.Monster
{
    // Keeps moving toward the player's center.
    public class GJMonsterStateChase : IGJMonsterState
    {
        private readonly GJMonster _monster;
        public readonly GJPlayerComponent _playerComponent;
        float ShootTimer = 0f;
        float whyisntthisamonobehaviorscript = 0;
        int chancetocharge = 0;
        UnityEngine.Vector2 target;
        float offsetx = 0f;
        float offsety = 0f;
        float mult = 1f;
        float chargeTimer = 0f;
        bool charging = false;
        bool cooldown = false;
        private readonly GJMonsterComponent _monsterComponent;

        public GJMonsterStateChase(GJMonster monster, GJPlayerComponent playerComponent, GJMonsterComponent monsterComponent)
        {
            _monster = monster;
            _playerComponent = playerComponent;
            _monsterComponent = monsterComponent;
            RandomizeTarget();
        }

        public void OnEnter()
        {
        }

        public void OnExit()
        {
        }

        public void UpdateState(TimeSpan deltaTime)
        {
            //
            //target = _playerComponent.ColliderBounds.center;
            _monster.MoveToward(target, deltaTime, mult);
            if (ShootTimer > 1)
            {
                ShootTimer -= (float)deltaTime.TotalSeconds;
            } else if (ShootTimer > 0 && ShootTimer <= 1)
            {
                _monsterComponent.m_Animator.ResetTrigger("explode");
                cooldown = false;
                if (whyisntthisamonobehaviorscript > 3){
                    ShootTimer = 0f;
                    whyisntthisamonobehaviorscript = 0;
                    
                    if (UnityEngine.Random.Range(chancetocharge, 5) == 5)
                    {
                        chargeTimer = 2f; // windup animation
                    }
                    else
                    {
                        RandomizeTarget();
                        chancetocharge++;
                    }                  
                } else if(whyisntthisamonobehaviorscript > 2)
                {
                    ShootTimer = 1.7f;
                    whyisntthisamonobehaviorscript++;
                    
                    Debug.Log("2");
                }
                else
                {
                    //should loop 3 times
                    _monster.SpawnProjectile(_playerComponent.ColliderBounds.center, deltaTime);
                    whyisntthisamonobehaviorscript++;
                    ShootTimer = 1.3f;

                }
            }
            if (chargeTimer > 1)
            {
                chargeTimer -= (float)deltaTime.TotalSeconds;
            } else if (chargeTimer > 0 && chargeTimer <= 1)
            {
                _monsterComponent.m_Animator.SetTrigger("charging");
                charging = true;
                mult = 3f;
                target = _playerComponent.ColliderBounds.center;
                chargeTimer = 0f;
            }
            
            if (_monster.getPosition() == target && whyisntthisamonobehaviorscript == 0 && charging == true)
            {
                _monsterComponent.m_Animator.ResetTrigger("charging");
                _monsterComponent.m_Animator.SetTrigger("explode");
                //await Awaitable.WaitForSecondsAsync(0.3f);
                Vector2[] directions = {
                    new Vector2(0, 1),    // North
                    new Vector2(1, 1).normalized,  // Northeast
                    new Vector2(1, 0),    // East
                    new Vector2(1, -1).normalized, // Southeast
                    new Vector2(0, -1),   // South
                    new Vector2(-1, -1).normalized, // Southwest
                    new Vector2(-1, 0),   // West
                    new Vector2(-1, 1).normalized  // Northwest
                };

                foreach (Vector2 direction in directions)
                {

                    _monster.SpawnProjectile(_monster.getPosition() + direction, deltaTime);
                } 
                ShootTimer = 2f;
                charging = false;
                cooldown = true;
                mult = 1f;
                chancetocharge = 0;
            } else if (_monster.getPosition() == target && whyisntthisamonobehaviorscript == 0 && cooldown == false)
            {
                ShootTimer = 1f;    
            }
        }

        void RandomizeTarget()
        {
            offsetx = UnityEngine.Random.Range(-2f, 2f);
            offsety = UnityEngine.Random.Range(-2f, 2f);
            if (offsetx >= 0)
            {
                offsetx += 5f;
            }else
            {
                offsetx -= 5f;
            }
            if (offsety >= 0)
            {
                offsety += 3f;
            }else
            {
                offsety -= 3f;
            }
            Vector2 targ= new Vector2 (_playerComponent.ColliderBounds.center.x + offsetx, _playerComponent.ColliderBounds.center.y + offsety);
            if (targ.x < 68f)
            {
                targ.x = 68f;
            } else if (targ.x > 118f)
            {
                targ.x = 118f;
            }
            if (targ.y > -14f)
            {
                targ.y = -14f;
            } else if (targ.y < -55)
            {
                targ.y = -55f;
            }
            target = targ;
            Debug.Log(mult);
            
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
