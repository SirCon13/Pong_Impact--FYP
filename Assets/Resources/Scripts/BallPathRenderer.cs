 using System.Collections.Generic;
using UnityEngine;

public class BallPathRenderer : MonoBehaviour
{
    public float goalIntercept;
    private List<Vector3> pathPoints = new List<Vector3>();
    [SerializeField] private float simulationTime;
    [SerializeField] private float timeStep;
    [SerializeField] private float boundaryX;
    [SerializeField] private float boundaryY;
    [SerializeField] private float interceptBoundary;

    void FixedUpdate()
    {
        Vector3 currentVelocity = GetComponent<Rigidbody2D>().linearVelocity;
        Vector3 lastVelocity = Vector3.zero;

        // Update path only if velocity changes significantly
        if (pathPoints.Count == 0  || currentVelocity != lastVelocity)
        {
            lastVelocity = currentVelocity;
            CalculatePath(currentVelocity);
        }
    }

    void CalculatePath(Vector3 velocity)
    {
        Debug.Log("Calculating path with velocity: " + velocity);
        pathPoints.Clear(); // Clear previous path
        Vector3 ballPosition = transform.position;
        pathPoints.Add(ballPosition);

        for (float t = 0; t < simulationTime; t += timeStep)
        {
            Vector3 nextPosition = ballPosition + velocity * timeStep;
            
            // Find intercept point
            if (ballPosition.x < interceptBoundary && nextPosition.x >= interceptBoundary)
            {
                goalIntercept = nextPosition.y;
            }
            // Stop path beyond horizontal boundaries
            if (nextPosition.x < -boundaryX || nextPosition.x > boundaryX)
            {
                nextPosition.x = Mathf.Clamp(nextPosition.x, -boundaryX, boundaryX);
                break;
            }
            // Reflect off vertical boundaries
            if (nextPosition.y < -boundaryY || nextPosition.y > boundaryY)
            {
                velocity.y = -velocity.y; // Reflect Y
                nextPosition.y = Mathf.Clamp(nextPosition.y, -boundaryY, boundaryY);
            }

            pathPoints.Add(nextPosition);
            ballPosition = nextPosition;
        }
    }

    void OnDrawGizmos()
    {
        // Connect the pathpoints to form a line
        Gizmos.color = Color.green;
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
        }
    }
}
