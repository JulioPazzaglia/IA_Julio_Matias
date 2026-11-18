using UnityEngine;

public class GatherState : State
{
    private Hunter _hunter;
    private StateMachine _stateMachine;
    private float _gatherTimer;

    public GatherState(Hunter hunter, StateMachine stateMachine)
    {
        _hunter = hunter;
        _stateMachine = stateMachine;
    }

    public override void Enter()
    {
        _gatherTimer = 0f;
        _hunter.SetAction("Buscando cuerpo para recolectar");
    }

    public override void Update()
    {
        Boid target = _hunter.Target;
        if (
            target == null
            || !target.isActiveAndEnabled
            || target.IsAlive
            || target.IsRespawning
            || target.RespawnRequested
        )
        {
            _stateMachine.ChangeState(HunterStateType.Patrol);
            return;
        }

        Vector3 delta = target.transform.position - _hunter.transform.position;
        delta.y = 0f;
        float distance = delta.magnitude;
        if (distance <= _hunter.GatherDistance)
        {
            _hunter.SetVelocity(Vector3.zero);
            _hunter.SetAction("Recolectando boid...");
            target.ShowGatherFeedback();
            _gatherTimer += Time.deltaTime;

            if (_gatherTimer >= _hunter.GatherDuration)
            {
                target.StartRespawn(); // Solicitud, transicion realizada por la FSM del Boid.
                _stateMachine.ChangeState(HunterStateType.Patrol);
            }
            return;
        }

        _gatherTimer = 0f;
        _hunter.SetAction("Yendo al cuerpo con Seek");
        _hunter.SetSteering(Steering.Seek(_hunter, target.transform, _hunter.MaxSpeed));
    }
}
