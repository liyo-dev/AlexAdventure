using UnityEngine;

namespace ithappy
{
    public class OnDutyGrummy : CharacterStateBase
    {
        private Vector2 _sleepRandomRange = new Vector2(15f, 60f);
        private float _currentTimer = 0;
        private KingsGrummy _kingsGrummy;
        
        public bool IsSleeping {get; private set;}
        
        public OnDutyGrummy(CharacterBase context) : base(context)
        {
            _kingsGrummy = (KingsGrummy)context;
            _kingsGrummy.OnWakeUp += KingsGrummyOnOnWakeUp;
        }

        public override void Enter()
        {
            base.Enter();
            
            _currentTimer = GetRandomTimer();
        }

        public override void Update()
        {
            if (!IsSleeping)
            {
                _currentTimer -= Time.deltaTime;
            }

            if (_currentTimer <= 0)
            {
                _currentTimer = GetRandomTimer();
                IsSleeping = true;
                CharacterBase.CharacterAnimator.SleepingOnDuty(true);
            }
        }

        public override void Exit()
        {
        }

        public override bool ShouldEnter()
        {
            return true;
        }
        
        private void KingsGrummyOnOnWakeUp()
        {
            if (IsSleeping)
            {
                IsSleeping = false;
                CharacterBase.CharacterAnimator.SleepingOnDuty(false);
            }
        }

        private float GetRandomTimer()
        {
            return Random.Range(_sleepRandomRange.x, _sleepRandomRange.y);
        }
    }
}
