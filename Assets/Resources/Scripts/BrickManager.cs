using UnityEngine;
using TMPro;

public class BrickManager : MonoBehaviour
{
    public static BrickManager instance;
    [SerializeField] private Color myColour; // Color of the brick
    [SerializeField] private int hitsLeft;
    public TextMeshProUGUI hitsLeftText; // TextMeshPro component for displaying hits left
    public float colorProbability; // Probability of the brick being blue

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Choose Blue or Red randomly
        myColour = Random.value < colorProbability ? Color.blue : Color.red; 
        GetComponent<Renderer>().material.color = myColour;
        if (myColour == Color.blue)
        {
            hitsLeftText.color = Color.red;
        }
        else
        {
            hitsLeftText.color = Color.blue;
        }
        hitsLeftText.transform.position = transform.position;
    }
    // Update is called once per frame
    void Update()
    {
        hitsLeftText.text = hitsLeft.ToString(); // Update the hits left text
    }
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Ball"))
        {
            // Check if the ball's color matches the brick's color
            BallManager ball = collision.collider.GetComponent<BallManager>();
            if (ball.myColour == myColour)
            {
                hitsLeft--; // Decrement hits left
                if (hitsLeft <= 0)
                {
                    GameManager.instance.RemoveBrick(gameObject);
                    Destroy(gameObject); // Destroy the brick when hits left is 0
                }
            }
        }
    }
}
