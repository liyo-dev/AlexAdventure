using System.Collections;
using UnityEngine;

namespace ithappy
{
    public class CandyKingBehavior : CharacterStateBase
    {
        private float _angryCooldown = 10f;
        private float _currentAngryCooldown;
        
        public CandyKingBehavior(CharacterBase context) : base(context)
        {
        }

        public override void Update()
        {
            _currentAngryCooldown -= Time.deltaTime;
            if (_currentAngryCooldown <= 0)
            {
                _currentAngryCooldown = _angryCooldown;
                CharacterBase.CharacterAnimator.Angry();
            }
        }

        public override void Exit()
        {
            
        }

        public override bool ShouldEnter()
        {
            return true;
        }
    }
}
