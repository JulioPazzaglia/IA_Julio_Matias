using UnityEngine;

public class ConsumeState : State
{
    private Boid _boid;
    private StateMachine _stateMachine;
    private float _consumeTimer;

    public ConsumeState(Boid boid, StateMachine stateMachine)
    {
        _boid = boid;
        _stateMachine = stateMachine;
    }

    public override void Enter()
    {
        _consumeTimer = 0f;
        _boid.SetVelocity(Vector3.zero);
        _boid.SetStateColor(Color.magenta);
    }

    public override void Update()
    {
        _boid.SetVelocity(Vector3.zero);
        Trap trap = _boid.TargetTrap;
        if (trap == null || !trap.isActiveAndEnabled || !trap.IsActive)
        {
            _boid.ClearTargetTrap();
            _stateMachine.ChangeState(BoidStateType.Flocking);
            return;
        }

        _consumeTimer += Time.deltaTime;
        if (_consumeTimer >= Mathf.Max(0.1f, _boid.TrapAttackInterval))
        {
            _consumeTimer = 0f;
            trap.TakeDamage(_boid.TrapDamage);
            if (!trap.IsActive)
            {
                _boid.ClearTargetTrap();
                _stateMachine.ChangeState(BoidStateType.Flocking);
            }
        }
    }
}
