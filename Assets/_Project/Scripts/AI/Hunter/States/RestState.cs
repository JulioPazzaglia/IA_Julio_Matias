using UnityEngine;

public class RestState : State
{
    private Hunter _hunter;
    private StateMachine _stateMachine;

    public RestState(Hunter hunter, StateMachine stateMachine)
    {
        _hunter = hunter;
        _stateMachine = stateMachine;
    }

    public override void Enter()
    {
        _hunter.SetTarget(null);
        _hunter.SetVelocity(Vector3.zero);
        _hunter.SetAction("Descansando y recargando energia");
    }

    public override void Update()
    {
        _hunter.SetVelocity(Vector3.zero);
        _hunter.RechargeEnergy();
        if (_hunter.Energy >= _hunter.MaxEnergy)
            _stateMachine.ChangeState(HunterStateType.Patrol);
    }
}
