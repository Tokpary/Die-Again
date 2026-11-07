namespace Code.Scripts.States.State
{
    public abstract class AState : IState
    {
        protected readonly GameContext _context;
        protected readonly IStateMachine _stateMachine;
        
        protected AState(GameContext context, IStateMachine stateMachine)
        {
            _context = context;
            _stateMachine = stateMachine;
        }
        
        public virtual void Enter()
        {
        }

        public virtual void Update()
        {
        }

        public virtual void Exit()
        {
        }
    }
}