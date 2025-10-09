using System;
using System.Collections.Generic;

namespace ithappy
{
    public class KingsGrummy : CharacterBase
    {
        public event Action OnWakeUp;
        
        protected Dictionary<Type, CharacterStateBase> _states;

        protected override Dictionary<Type, CharacterStateBase> States => _states;

        public bool IsSleeping => ((OnDutyGrummy)_currentState).IsSleeping;

        public override void Initialize()
        {
            base.Initialize();

            _states = new Dictionary<Type, CharacterStateBase>
            {
                {
                    typeof(OnDutyGrummy), new OnDutyGrummy(this)
                },
            };

            _states[typeof(OnDutyGrummy)].SetStatesToTransition(new List<CharacterStateBase>());

            TransitionToState(typeof(OnDutyGrummy));
        }

        public void WakeUp()
        {
            OnWakeUp?.Invoke();
        }
    }
}