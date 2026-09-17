using UnityEngine;

public static class Steering
{
    public static Vector3 Seek(
        Agent agent,
        Transform target,
        float maxSpeed)
    {
        Vector3 direction = target.position - agent.transform.position;

        Vector3 desired =
            direction.normalized * maxSpeed;

        return desired - agent.Velocity;
    }

    public static Vector3 Flee(
        Agent agent,
        Transform target,
        float maxSpeed)
    {
        Vector3 direction = target.position - agent.transform.position;

        Vector3 desired =
            -direction.normalized * maxSpeed;

        return desired - agent.Velocity;
    }

    public static Vector3 Arrive(
        Agent agent,
        Transform target,
        float maxSpeed,
        float slowingDistance)
    {
        Vector3 direction =
            target.position - agent.transform.position;

        float distance = direction.magnitude;

        float speed = maxSpeed;

        if (distance < slowingDistance)
            speed = maxSpeed * (distance / slowingDistance);

        Vector3 desired =
            direction.normalized * speed;

        return desired - agent.Velocity;
    }

    public static Vector3 Pursuit(
        Agent agent,
        Agent target,
        float maxSpeed)
    {
        Vector3 futurePosition =
            SteeringHelpers.CalculateFuture(
                agent,
                target,
                maxSpeed
            );

        Vector3 direction =
            futurePosition - agent.transform.position;

        Vector3 desired =
            direction.normalized * maxSpeed;

        return desired - agent.Velocity;
    }

    public static Vector3 Evade(
        Agent agent,
        Agent target,
        float maxSpeed)
    {
        Vector3 futurePosition =
            SteeringHelpers.CalculateFuture(
                agent,
                target,
                maxSpeed
            );

        Vector3 direction =
            futurePosition - agent.transform.position;

        Vector3 desired =
            -direction.normalized * maxSpeed;

        return desired - agent.Velocity;
    }
}