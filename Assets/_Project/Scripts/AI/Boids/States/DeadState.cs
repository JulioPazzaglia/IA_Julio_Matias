using UnityEngine;

public class DeadState : State
{
    private Boid _boid;
    private StateMachine _stateMachine;

    public DeadState(Boid boid, StateMachine stateMachine)
    {
        _boid = boid;
        _stateMachine = stateMachine;
    }

    public override void Enter()
    {
        _boid.SetVelocity(Vector3.zero);
        _boid.ClearTargetTrap();
        _boid.SetStateColor(Color.gray);
    }

    public override void Update()
    {
        _boid.SetVelocity(Vector3.zero);
        if (_boid.RespawnRequested)
            _stateMachine.ChangeState(BoidStateType.Respawn);
    }
}
