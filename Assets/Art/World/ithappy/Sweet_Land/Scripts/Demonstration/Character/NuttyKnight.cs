using System;
using System.Collections.Generic;
using UnityEngine;

namespace ithappy
{
    public class NuttyKnight : CharacterBase
    {
        [SerializeField] private List<Transform> _waypoints;
        
        protected Dictionary<Type, CharacterStateBase> _states;

        protected override Dictionary<Type, CharacterStateBase> States => _states;

        public override void Initialize()
        {
            base.Initialize();

            _states = new Dictionary<Type, CharacterStateBase>
            {
                {
                    typeof(NuttyKnightBehavior), new NuttyKnightBehavior(this, _movement)
                },
                {
                    typeof(PatrolByPointsCharacterState), new PatrolByPointsCharacterState(this, _movement, _waypoints)
                },
            };

            _states[typeof(NuttyKnightBehavior)].SetStatesToTransition(new List<CharacterStateBase>());
            _states[typeof(PatrolByPointsCharacterState)].SetStatesToTransition(new List<CharacterStateBase>()
            {
                _states[typeof(NuttyKnightBehavior)],
            });

            TransitionToState(typeof(PatrolByPointsCharacterState));
        }
    }
}
