using UnityEngine;

public class HunterAttackState : HunterState
{
    public HunterAttackState(
    Hunter hunter,
    StateMachine stateMachine
) : base(hunter, stateMachine)
    {
    }
    public override void Enter()
    {
    }

    public override void Update()
    {
        if (_hunter.Target == null)
            return;

        float distance = Vector3.Distance(
            _hunter.transform.position,
            _hunter.Target.transform.position
        );

        if (distance <= _hunter.MeleeAttackRadius)
        {
            return;
        }

        if (distance <= _hunter.RangeAttackRadius)
        {
            return;
        }

        Vector3 steering = Pursuit();

        _hunter.SetVelocity(
            Vector3.ClampMagnitude(
                _hunter.Velocity + steering,
                _hunter.MaxSpeed
            )
        );
    }

    private Vector3 Pursuit()
    {
        Vector3 direction =
            _hunter.Target.transform.position - _hunter.transform.position;

        float distance = direction.magnitude;

        float prediction = distance / _hunter.MaxSpeed;

        Vector3 futurePosition =
            _hunter.Target.transform.position
            + _hunter.Target.Velocity * prediction;

        Vector3 desired =
            (futurePosition - _hunter.transform.position).normalized
            * _hunter.MaxSpeed;

        return desired - _hunter.Velocity;
    }

    public override void Exit()
    {
    }
}