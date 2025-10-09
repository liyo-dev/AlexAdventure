namespace ithappy
{
    public class IdleCharacterState : CharacterStateBase
    {
        public IdleCharacterState(CharacterBase context) : base(context)
        {
        }

        public override void Update()
        {
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
