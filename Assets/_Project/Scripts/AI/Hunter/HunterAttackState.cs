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
        Debug.Log("ENTRE A ATTACK");
    }

    public override void Update()
    {
    }

    public override void Exit()
    {
    }
}