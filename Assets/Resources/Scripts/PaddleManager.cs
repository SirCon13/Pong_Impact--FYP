using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class PaddleManager : MonoBehaviour
{
    public static PaddleManager instance;
    private bool two_player;
    private Color myColour;
    private float lastSpeed;
    [SerializeField] private bool frozen;
    [SerializeField] private float speed;
    [SerializeField] private float boundary;
    [SerializeField] private bool isPlayerL;
    [SerializeField] private float targetIntercept;

    void Update()
    {
        GetComponent<Renderer>().material.color = myColour; // Set paddle color

        if (isPlayerL)
        {
            // Set Player L paddle to blue
            myColour = Color.blue; 
            if (Keyboard.current[Key.W].isPressed && transform.position.y < boundary)
            {
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, transform.position.y + 1, 0), speed * Time.deltaTime);
            }
            if (Keyboard.current[Key.S].isPressed && transform.position.y > -boundary)
            {
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, transform.position.y - 1, 0), speed * Time.deltaTime);
            }
        }
        else
        {
            if (!two_player)
            {                
                FindTargetIntercept();
            }
            // Set Player R paddle to red
            myColour = Color.red;   
            if (Keyboard.current[Key.UpArrow].isPressed && transform.position.y < boundary)
            {
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, transform.position.y + 1, 0), speed * Time.deltaTime);
            }
            if (Keyboard.current[Key.DownArrow].isPressed && transform.position.y > -boundary)
            {
                transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, transform.position.y - 1, 0), speed * Time.deltaTime);
            }
        }
    }
    public void FindTargetIntercept()
    {
        // Pick intercept position of the closest ball  
        GameObject[] ballsInScene = GameObject.FindGameObjectsWithTag("Ball");
        float smallestDistance = ballsInScene[0].transform.position.x - transform.position.x;
        int i = 0;
        foreach(GameObject ball in ballsInScene)
        {
            if (ball.transform.position.x - transform.position.x <= smallestDistance)
            {
                smallestDistance = ball.transform.position.x - transform.position.x;
                targetIntercept = ballsInScene[i].GetComponent<BallPathRenderer>().goalIntercept;
            }
            i++;
        }
        transform.position = Vector3.MoveTowards(transform.position, new Vector3(transform.position.x, targetIntercept, 0), speed * Time.deltaTime);
    }
    public void FreezePaddle(Color freeze_colour)
    {
        // Freeze paddle for 4 seconds
        if (myColour == freeze_colour)
        {
            lastSpeed = speed;
            speed = 0;
            frozen = true;
            Debug.Log($"{Time.time} Paddle frozen.");
            StartCoroutine(UnFreezePaddle());
        }
    }
    public void changePaddleSize(bool adv)
    {
        if(adv)
        {
            transform.localScale = new Vector3(0.5f, 3, 1);
        }
        else
        {
            transform.localScale = new Vector3(0.5f, 2, 1);
        } 
    }
    public void changePaddleSpeed(bool adv)
    {
        if (!frozen)
        {
            if(adv)
            {
                speed = 7.5f;
            }
            else
            {
                speed = 5f;
            }
        }  
    }
    public void setToPL() 
    { 
        isPlayerL = true;
    }
    public void setToPR() 
    { 
        isPlayerL = false;
        two_player = true;
    }
    public void setToCPU() 
    {
        isPlayerL = false;
        two_player = false;
    }
    IEnumerator UnFreezePaddle()
    {
        yield return new WaitForSeconds(4f);
        frozen = false;
        Debug.Log($"{Time.time} Paddle un-frozen.");
        speed = lastSpeed;
    }
}
