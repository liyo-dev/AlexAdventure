using System.Collections;
using UnityEngine;

namespace ithappy
{
    public class NuttyKnightBehavior : CharacterStateBase
    {
        private float _searchRadius = 15;
        private KingsGrummy _kingsGrummy;
        private bool _isSearching;
        private MovementBase _movement;
        private float _wakeUpTime = 1f;
        private Coroutine _walkToStartPosCoroutine;
        
        public NuttyKnightBehavior(CharacterBase context, MovementBase movement) : base(context)
        {
            _movement = movement;
        }

        public override void Enter()
        {
            base.Enter();
            
            _isSearching = true;
        }

        public override void Update()
        {
            if (!_isSearching)
            {
                return;
            }
            
            if (!TryFindSleepingGrummy())
            {
                CharacterBase.NextState();
            }
            else
            {
                if (_walkToStartPosCoroutine != null)
                {
                    CharacterBase.StopCoroutine(_walkToStartPosCoroutine);
                }
                    
                _isSearching = false;
                CharacterBase.StartCoroutine(WakeUpGrummyProcess());
            }
        }

        public override void Exit()
        {
            
        }

        public override bool ShouldEnter()
        {
            return TryFindSleepingGrummy();
        }

        private bool TryFindSleepingGrummy()
        {
            Collider[] hitColliders = Physics.OverlapSphere(_movement.MoveParent.position, _searchRadius);
            for (int index = 0; index < hitColliders.Length; index++)
            {
                KingsGrummy grummy = hitColliders[index].GetComponent<KingsGrummy>();
                if (grummy != null && grummy.IsSleeping)
                {
                    _kingsGrummy = grummy;
                    return true;
                }
            }
            return false;
        }

        private IEnumerator WakeUpGrummyProcess()
        {
            bool result = false;
            bool inProcess = true;
            
            Vector3 targetPosition = _kingsGrummy.transform.position + _kingsGrummy.transform.forward * 2f;
            _movement.RotateToTarget(targetPosition, (isComplete) =>
            {
                result = isComplete;
                inProcess = false;
            });
            yield return new WaitUntil(() => !inProcess);
            
            inProcess = true;
            
            _movement.NavMeshMoveToTarget(targetPosition, (isComplete) =>
            {
                result = isComplete;
                inProcess = false;
            });
            yield return new WaitUntil(() => !inProcess);
            
            inProcess = true;
            
            _movement.RotateToTarget(_kingsGrummy.transform.position, (isComplete) =>
            {
                result = isComplete;
                inProcess = false;
            });
            yield return new WaitUntil(() => !inProcess);
            
            _kingsGrummy.WakeUp();
            yield return new WaitForSeconds(_wakeUpTime);
            
            _isSearching = true;
        }
    }
}
