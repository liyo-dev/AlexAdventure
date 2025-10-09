using System;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace ithappy
{
    public class PatrolCharacterState : CharacterStateBase
    {
        private Action<bool> _onReached;
        
        private NavMeshTriangulation _navMeshData;
        private MovementBase _movement;
        private bool _movingToRandomPoint;
        private Vector3 _randomPoint;
        
        public PatrolCharacterState(CharacterBase context, MovementBase movement) : base(context)
        {
            _movement = movement;
            _navMeshData = NavMesh.CalculateTriangulation();
        }
        
        public override void Enter()
        {
            base.Enter();
            
            if (_movingToRandomPoint)
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

            if (!_movingToRandomPoint && GetRandomPointOnNavMesh())
            {
                MoveToRandomPoint();
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
