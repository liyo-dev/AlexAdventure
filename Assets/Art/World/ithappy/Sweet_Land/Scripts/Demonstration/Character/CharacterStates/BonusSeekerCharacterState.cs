using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace ithappy
{
    public class BonusSeekerCharacterState : CharacterStateBase
    {
        private Action<bool> _onReached;
        
        private NavMeshTriangulation _navMeshData;
        private BonusItem _foundedBonusItem;
        private MovementBase _movement;
        private float _seekRadius = 15f;
        private bool _seekBonus = true;
        private float _seekTimer = 1f;
        private float _currentSeekTimer = 0;
        private bool _movingToRandomPoint;
        private Vector3 _randomPoint;
        
        public BonusSeekerCharacterState(CharacterBase context, MovementBase movement) : base(context)
        {
            _movement = movement;
            _navMeshData = NavMesh.CalculateTriangulation();
        }

        public override void Enter()
        {
            base.Enter();
            
            if (_foundedBonusItem != null)
            {
                MoveToBonus();
            }
            else if (_movingToRandomPoint)
            {
                MoveToRandomPoint();
            }
        }

        public override void Update()
        {
            for (int i = 0; i < _statesToTransition.Count; i++)
            {
                if (_statesToTransition[i].ShouldEnter())
                {
                    CharacterBase.TransitionToState(_statesToTransition[i]);
                    break;
                }
            }

            if (_seekBonus)
            {
                _currentSeekTimer -= Time.deltaTime;
                if (_currentSeekTimer <= 0)
                {
                    _currentSeekTimer = _seekTimer;
                    FindBonusItem();
                }
                
                if (_seekBonus && !_movingToRandomPoint && GetRandomPointOnNavMesh())
                {
                    MoveToRandomPoint();
                }
            } 
        }

        public override void Exit()
        {
            _onReached = null;
        }

        public override bool ShouldEnter()
        {
            return true;
        }

        private void FindBonusItem()
        {
            Collider[] hitColliders = Physics.OverlapSphere(_movement.MoveParent.position, _seekRadius);
            for (int index = 0; index < hitColliders.Length; index++)
            {
                BonusItem bonusItem = hitColliders[index].GetComponent<BonusItem>();
                if (bonusItem != null && bonusItem.enabled)
                {
                    _foundedBonusItem = bonusItem;
                    _seekBonus = false;
                    _movingToRandomPoint = false;

                    MoveToBonus();
                    break;
                }
            }
        }

        private void MoveToBonus()
        {
            _onReached = (isReached) =>
            {
                if (isReached)
                {
                    _foundedBonusItem = null;
                    _seekBonus = true;
                }
            };
            
            _movement.NavMeshMoveToTarget(_foundedBonusItem.transform.position, _onReached);
        }
        
        private bool GetRandomPointOnNavMesh()
        {
            if (_navMeshData.vertices.Length == 0)
            {
                return false;
            }
            
            int triangleIndex = Random.Range(0, _navMeshData.indices.Length / 3);
            
            Vector3 a = _navMeshData.vertices[_navMeshData.indices[triangleIndex * 3]];
            Vector3 b = _navMeshData.vertices[_navMeshData.indices[triangleIndex * 3 + 1]];
            Vector3 c = _navMeshData.vertices[_navMeshData.indices[triangleIndex * 3 + 2]];
            
            Vector3 randomPoint = GetRandomPointInTriangle(a, b, c);
            
            if (!NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
            {
                return false;
            }
            
            NavMeshPath path = new NavMeshPath();
            if (!NavMesh.CalculatePath(_movement.MoveParent.position, hit.position, NavMesh.AllAreas, path))
            {
                return false;
            }

            if (path.status != NavMeshPathStatus.PathComplete)
            {
                return false;
            }
            
            _randomPoint = hit.position;
            return true;
        }

        private void MoveToRandomPoint()
        {
            _onReached = (isReached) =>
            {
                if (isReached)
                {
                    _movingToRandomPoint = false;
                }
            };
            
            _movingToRandomPoint = true;
            _movement.NavMeshMoveToTarget(_randomPoint, _onReached);
        }

        private Vector3 GetRandomPointInTriangle(Vector3 a, Vector3 b, Vector3 c)
        {
            float r1 = Random.Range(0f, 1f);
            float r2 = Random.Range(0f, 1f);
            
            if (r1 + r2 > 1f)
            {
                r1 = 1f - r1;
                r2 = 1f - r2;
            }
            
            return a + r1 * (b - a) + r2 * (c - a);
        }
    }
}
