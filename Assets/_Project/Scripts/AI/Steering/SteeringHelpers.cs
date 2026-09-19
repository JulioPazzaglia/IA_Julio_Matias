using UnityEngine;

public static class SteeringHelpers
{
    public static Vector3 CalculateFuture(Agent agent, Agent target, float maxSpeed)
    {
        float distance = Vector3.Distance(agent.transform.position, target.transform.position);

        float prediction = distance / maxSpeed;

        return target.transform.position + target.Velocity * prediction;
    }
}
