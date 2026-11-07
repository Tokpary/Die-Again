namespace Code.Scripts.States
{
    public interface IStateMachine
    {
        IState CurrentState { get; }
        
        void ChangeState(IState newState);
        
    }
}