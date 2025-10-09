using System;
using System.Collections.Generic;

namespace ithappy
{
    public class BonusSeeker : CharacterBase
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
                    typeof(BonusSeekerCharacterState), new BonusSeekerCharacterState(this, _movement)
                },
                {
                    typeof(HelloCharacterState), new HelloCharacterState(this, _movement)
                },
            };
            
            _states[typeof(BonusSeekerCharacterState)].SetStatesToTransition(new List<CharacterStateBase>
            {
                _states[typeof(ObstacleOvercomeCharacterState)],
                _states[typeof(HelloCharacterState)],
            });
            _states[typeof(ObstacleOvercomeCharacterState)].SetStatesToTransition(new List<CharacterStateBase>());

            TransitionToState(typeof(BonusSeekerCharacterState));
        }
    }
}
