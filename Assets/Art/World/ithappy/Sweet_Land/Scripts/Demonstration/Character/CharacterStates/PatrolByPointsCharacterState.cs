using System.Collections.Generic;
using UnityEngine;

namespace ithappy
{
    public class PatrolByPointsCharacterState : CharacterStateBase
    {
        private List<Transform> _points;
        private MovementBase _movement;
        private int _currentIndex = 0;
        private bool _isMoving = false;
        
        public PatrolByPointsCharacterState(CharacterBase context, MovementBase movement, List<Transform> points) : base(context)
        {
            _points = points;
            _movement = movement;
        }

        public override void Enter()
        {
            base.Enter();

            MoveToPoint();
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
            
            if (!_isMoving)
            {
                _currentIndex++;
                if (_currentIndex >= _points.Count)
                {
                    _currentIndex = 0;
                }

                MoveToPoint();
            }
        }

        public override void Exit()
        {
            _isMoving = false;
        }

        public override bool ShouldEnter()
        {
            return true;
        }

        private void MoveToPoint()
        {
            _isMoving = true;
            _movement.NavMeshMoveToTarget(_points[_currentIndex].position, (isSuccess) =>
            {
                _isMoving = false;
            });
        }

        private int FindNearestPoint()
        {
            int index = 0;
            float minDist = float.MaxValue;
            float currentDist = float.MaxValue;
            
            for (int i = 0; i < _points.Count; i++)
            {
                currentDist = Vector3.Distance(_points[i].position, _movement.MoveParent.position);
                if (currentDist <= minDist)
                {
                    index = i;
                    minDist = currentDist;
                }
            }
            
            return index;
        }
    }
}
