using UnityEngine;

public class EvadeState : State
{
    private Boid _boid;
    private StateMachine _stateMachine;

    public EvadeState(Boid boid, StateMachine stateMachine)
    {
        _boid = boid;
        _stateMachine = stateMachine;
    }

    public override void Enter()
    {
        _boid.ClearTargetTrap();
        _boid.SetStateColor(Color.cyan);
    }

    public override void Update()
    {
        Hunter hunter = _boid.Perception != null ? _boid.Perception.DetectHunter() : null;
        if (hunter == null)
        {
            _stateMachine.ChangeState(BoidStateType.Flocking);
            return;
        }

        // Evade usa prediccion (Steering.Evade), con Separation como apoyo.
        Vector3 steering =
            Steering.Evade(_boid, hunter, _boid.EvadeSpeed) + _boid.CalculateEvadeSeparation();
        _boid.SetSteering(steering);
    }
}
