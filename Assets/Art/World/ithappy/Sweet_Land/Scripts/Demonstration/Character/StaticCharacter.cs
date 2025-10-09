using System;
using System.Collections.Generic;

namespace ithappy
{
    public class StaticCharacter : CharacterBase
    {
        protected Dictionary<Type, CharacterStateBase> _states;

        protected override Dictionary<Type, CharacterStateBase> States => _states;

        public override void Initialize()
        {
            base.Initialize();
            
            _states = new Dictionary<Type, CharacterStateBase>
            {
                {
                    typeof(IdleCharacterState), new IdleCharacterState(this)
                },
            };
            
            _states[typeof(IdleCharacterState)].SetStatesToTransition(new List<CharacterStateBase>());

            TransitionToState(typeof(IdleCharacterState));
        }
    }
}
