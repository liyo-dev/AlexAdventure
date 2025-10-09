using System;
using System.Collections.Generic;

namespace ithappy
{
    public class Grummy : CharacterBase
    {
        protected Dictionary<Type, CharacterStateBase> _states;

        protected override Dictionary<Type, CharacterStateBase> States => _states;

        public override void Initialize()
        {
            base.Initialize();
            
            _states = new Dictionary<Type, CharacterStateBase>
            {
                {
                    typeof(ObstacleOvercomeCharacterState), new ObstacleOvercomeCharacterState(this, _movement)
                },
                {
                    typeof(PatrolCharacterState), new PatrolCharacterState(this, _movement)
                },
                // {
                //     typeof(ChaseCharacterState), new ChaseCharacterState(this, _movement)
                // },
            };
            
            _states[typeof(PatrolCharacterState)].SetStatesToTransition(new List<CharacterStateBase>
            {
                _states[typeof(ObstacleOvercomeCharacterState)],
                // _states[typeof(ChaseCharacterState)],
            });
            _states[typeof(ObstacleOvercomeCharacterState)].SetStatesToTransition(new List<CharacterStateBase>
            {
                _states[typeof(PatrolCharacterState)]
            });
            // _states[typeof(ChaseCharacterState)].SetStatesToTransition(new List<CharacterStateBase>()
            // {
            //     _states[typeof(ObstacleOvercomeCharacterState)],
            // });

            TransitionToState(typeof(PatrolCharacterState));
        }
    }
}