using System;
using System.Collections.Generic;

namespace ithappy
{
    public class CandyKing : CharacterBase
    {
        protected Dictionary<Type, CharacterStateBase> _states;

        protected override Dictionary<Type, CharacterStateBase> States => _states;

        public override void Initialize()
        {
            base.Initialize();

            _states = new Dictionary<Type, CharacterStateBase>
            {
                {
                    typeof(CandyKingBehavior), new CandyKingBehavior(this)
                },
            };

            _states[typeof(CandyKingBehavior)].SetStatesToTransition(new List<CharacterStateBase>());

            TransitionToState(typeof(CandyKingBehavior));
        }
    }
}
