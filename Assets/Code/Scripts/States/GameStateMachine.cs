namespace Code.Scripts.States
{
    public class GameStateMachine : IStateMachine
    {
        public IState CurrentState { get; private set; }
        
        public void ChangeState(IState newState)
        {
            CurrentState?.Exit();
            CurrentState = newState;
            CurrentState.Enter();
        }
        
        
    }
}