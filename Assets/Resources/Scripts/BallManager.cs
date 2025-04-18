using System.Collections;
using UnityEngine;

public class BallManager : MonoBehaviour
{
    public static BallManager instance; 
    
    [Header("General")]
    public Color myColour;    
    private bool stuck;
    private Vector2 lastVelocity;
    private Rigidbody2D rb;

    [Header("Object contact")]
    [SerializeField] private float contactDuration;
    [SerializeField] private float contactDurationThreshold;
    [SerializeField] private int contactCount;
    [SerializeField] private int contactCountLimit;
    [SerializeField] private bool isColliding;
    [SerializeField] private float mySpeed;

    private void Awake()
    {
        instance = this;
        rb = GetComponent<Rigidbody2D>();
        stuck = false;
    }
    void FixedUpdate()
    {
        gameObject.GetComponent<Renderer>().material.color = myColour;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        mySpeed = rb.linearVelocity.magnitude;
        if (mySpeed < 3f && !stuck)
        {
            destoryBall();
            Debug.Log("Ball is too slow, destroying it.");
        }
         if (contactCount > contactCountLimit || contactDuration > contactDurationThreshold)
        {
            destoryBall();
        }
    }
    public void setBallColour(Color set_color)
    {
        myColour = GetComponent<Renderer>().material.color = set_color;
    }
    public void createBall(Vector2 position, Vector2 velocity, Color color)
    {
        GameObject ballPrefab = Resources.Load<GameObject>("Prefabs/Ball");
        GameObject newBall = Instantiate(ballPrefab, position, Quaternion.identity);
        newBall.GetComponent<BallManager>().setBallColour(color);
        GameManager.instance.addBallToList(newBall);
        launchBall(newBall, position, velocity);
    }
    public void launchBall(GameObject ball, Vector2 launch_position, Vector2 launch_velocity)
    {
        ball.transform.position = launch_position;
        Rigidbody2D rb = ball.GetComponent<Rigidbody2D>();
        rb.linearVelocity = launch_velocity;
    }
    private void destoryBall()
    {
        Destroy(gameObject);
        GameManager.instance.RemoveBall(gameObject);
        int ballCount = GameManager.instance.CountBalls();
        if (ballCount <= 0)
        {
            GameManager.instance.ResetGame();
        }
    }
    public void duplicateBall()
    {
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        Vector2 copyVelocity = -1 * rb.linearVelocity;
        Color copyColour;
        if (myColour == Color.blue)
        {
            copyColour = Color.red;
        }
        else if (myColour == Color.red)
        {
            copyColour = Color.blue;
        }
        else
        {
            copyColour = myColour;
        }
        createBall(transform.position, copyVelocity, copyColour);
    }
    private void OnTriggerEnter2D(Collider2D collider)
    {
        // Goal triggers
        if (collider.CompareTag("GoalL"))
        {
            GameManager.instance.UpdateScore("GoalL");
            if (GameManager.instance.co_opMode)
            {
                GameManager.instance.UpdateScoreCoOp(false);
            } 
            destoryBall();     
        }
        if (collider.CompareTag("GoalR"))
        {
            GameManager.instance.UpdateScore("GoalR");
            if (GameManager.instance.co_opMode)
            {
                GameManager.instance.UpdateScoreCoOp(false);
            } 
            destoryBall();
        }
        // Item triggers
        if (collider.CompareTag("Duplicator"))
        {
            duplicateBall();
            GameManager.instance.RemoveDuplicator(collider.gameObject);
            Destroy(collider.gameObject);
        }
        if (collider.CompareTag("Comb"))
        {
            // Check if the ball's color matches the comb's color
            Color combColour = collider.GetComponent<Renderer>().material.color;
            if (myColour == combColour)
            {
                stuck = true;
                lastVelocity = rb.linearVelocity;
                rb.linearVelocity = Vector2.zero; 
                StartCoroutine(combTimer(collider.gameObject));
            }   
        }
        if (collider.CompareTag("Freeze"))
        {
            // Check if the ball's color matches the freeze's color
            Color freezeColour = collider.GetComponent<Renderer>().material.color;
            PaddleManager[] paddles = FindObjectsByType<PaddleManager>(FindObjectsSortMode.None);
            if (myColour == freezeColour)
            {
                foreach (PaddleManager paddle in paddles)
                {
                    paddle.FreezePaddle(freezeColour);
                } 
                GameManager.instance.RemoveFreeze(collider.gameObject);
                Destroy(collider.gameObject);
            }   
        }
    }
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Track wall contact
        if (collision.collider.CompareTag("Wall"))
        {
            isColliding = true;
            contactCount++;          
        }
        // Track paddle contact
        if (collision.collider.CompareTag("Paddle"))
        {
            contactCount = 0;
            myColour = collision.collider.GetComponent<Renderer>().material.color;
            if(GameManager.instance.co_opMode)
            {
                GameManager.instance.UpdateScoreCoOp(true);
            }
        }
    }
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (isColliding)
        {
            contactDuration += Time.deltaTime;
        }
    }
    IEnumerator combTimer(GameObject comb)
    {
        yield return new WaitForSeconds(4f);
        stuck = false;
        GameManager.instance.RemoveComb(comb);
        Destroy(comb);
        rb.linearVelocity = lastVelocity;
    }
}
