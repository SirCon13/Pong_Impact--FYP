using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [Header("General")]
    public static GameManager instance;
    Vector2[] itemSpawnPositions =
    {
        new Vector2(-3, -2), new Vector2(0, -2), new Vector2(3, -2),
        new Vector2(-3, 0),  new Vector2(3, 0),
        new Vector2(-3, 2),  new Vector2(0, 2), new Vector2(3, 2)
    };
    List<Vector2> availablePositions = new List<Vector2>();
    public bool co_opMode = false;
    public float coOpTimeLimit;
    private Coroutine countdownCoroutine;
    public int scoreThreshold;
    public bool in_game = false;
    
    [Header("Items")]
    public List<GameObject> bricks = new List<GameObject>();    
    [SerializeField] private int brickNum;
    public List<GameObject> duplicators = new List<GameObject>();        
    List<GameObject> combs = new List<GameObject>();
    List<GameObject> freezes = new List<GameObject>();
    [SerializeField] private float colorProbability; // Probability of item being blue
    [SerializeField] private bool isDuplicatorEnabled;
    [SerializeField] private bool isBricksEnabled;
    [SerializeField] private bool isCombEnabled;
    [SerializeField] private bool isFreezeEnabled;

    [Header("Wait times")]
    [SerializeField] private float duplicatorRespawnWaitTime;
    [SerializeField] private float brickRespawnWaitTime;
    [SerializeField] private float combRespawnWaitTime;
    [SerializeField] private float freezeRespawnWaitTime;

    [Header("Ball-related")]
    public List<GameObject> balls = new List<GameObject>();
    private BallManager ballManagerScript;
    [SerializeField] private float ballLaunchSpeed;

    [Header("Paddle-related")]
    private GameObject playerL;
    private GameObject playerR;
    private bool two_player;    
    
    [Header("UI")]
    public GameObject pauseMenu;
    public GameObject mainMenu;
    public GameObject modeMenu;
    public GameObject settingsMenu;
    public GameObject scoreBoard;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI headerText;
    public TextMeshProUGUI countdownText;
    public Button mainMenuButton;
    public int pL_score, pR_score;
    public int co_opScore;

    /*----------------------------------------------
                        GENERAL
    ----------------------------------------------*/
    void Awake()
    {
        instance = this;
        availablePositions.AddRange(itemSpawnPositions); // Copy all possible positions to available positions
    }
    void Start()
    {
        // Show main menu
        mainMenu.SetActive(true);
        modeMenu.SetActive(false);
        settingsMenu.SetActive(false);
        pauseMenu.SetActive(false);
        scoreBoard.SetActive(false);
        mainMenuButton.gameObject.SetActive(false);

        // Enable default settings
        enableBricks(true);
        enableDuplicator(true);
        enableComb(true);
        enableFreeze(true);
        SetBallSpeed(1); // Default to medium speed
        SetCoOpTimeLimit(1); // Default to 2 minutes
        SetPointsToWin("10"); // Default to 10 points to win

        Time.timeScale = 1; // Keep game unpaused
    }
    void Update()
    {
        if (in_game)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (mainMenu.activeSelf) return;

                if (pauseMenu.activeSelf)
                {
                    ResumeGame();
                }  
                else
                {
                    PauseGame();
                }      
            }
            if (!co_opMode)
            {
                if (pL_score >= scoreThreshold)
                {
                    leftWon();
                }
                else if (pR_score >= scoreThreshold)
                {
                    rightWon();
                }
            }
        }     
    }

    private IEnumerator StartCountdown()
    {
        float countdown = coOpTimeLimit;
        while (countdown > 0f)
        {
            int minutes = (int)(countdown / 60);
            int seconds = (int)(countdown % 60);
            countdownText.text = string.Format("{0}:{1:00}", minutes, seconds);
            yield return new WaitForSeconds(1f);
            countdown--;   
        }
        gameOver();
    }
    public void UpdateScore(string goalScored)
    {
        if (goalScored == "GoalL")
        {
            pR_score++;
        }
        else
        {
            pL_score++;
        }
        scoreText.text = $"{pL_score} - {pR_score}";
    }
    public void UpdateScoreCoOp(bool increaseScore)
    {
        if (increaseScore)
        {
            co_opScore++;
        }
        else
        {
            co_opScore--;
            if (co_opScore < 0)
            {
                co_opScore = 0;
            }
        }
        scoreText.text = $"{co_opScore}";
    }
    private void balanceScore()
    {
        int scoreGap = Mathf.Abs(pL_score - pR_score);

        if (scoreGap > 5)
        {
            if (pL_score > pR_score)
            {
                playerR.GetComponent<PaddleManager>().changePaddleSize(true);
            }
            else
            {
                playerL.GetComponent<PaddleManager>().changePaddleSize(true);
            }
        }
        else if (scoreGap > 3 && scoreGap <= 5)
        {
            playerL.GetComponent<PaddleManager>().changePaddleSize(false);
            playerR.GetComponent<PaddleManager>().changePaddleSize(false);
        }
        else if (scoreGap >= 3)
        {
            if (pL_score > pR_score)
            {
                itemColourBias("Red");
                playerR.GetComponent<PaddleManager>().changePaddleSpeed(true);
            }
            else
            {
                itemColourBias("Blue");
                playerL.GetComponent<PaddleManager>().changePaddleSpeed(true);
            }
        }
        else
        {
            itemColourBias("None");
            playerL.GetComponent<PaddleManager>().changePaddleSpeed(false);
            playerR.GetComponent<PaddleManager>().changePaddleSpeed(false);
        }
    }
    private void itemColourBias(string colour)
    {
        if(colour == "Blue")
        {
            // 80% chance of blue, 20% chance of red
            colorProbability = 0.8f;
            Debug.Log("Blue bias");
        }
        else if (colour == "Red")
        {
            // 20% chance of blue, 80% chance of red
            colorProbability = 0.2f;
            Debug.Log("Red bias");
        }
        else
        {
            // Equal chance of either colour
            colorProbability = 0.5f;
            Debug.Log("No bias");
        }
    }
    private void spawnPaddles()
    {
        // Spawn paddles into the scene
        GameObject paddlePrefab = Resources.Load<GameObject>("Prefabs/Paddle");
        Vector2 pL_startPos = new Vector2(-7.75f, 0);
        Vector2 pR_startPos = new Vector2(7.75f, 0);
        playerL = Instantiate(paddlePrefab, pL_startPos, Quaternion.identity);
        playerR = Instantiate(paddlePrefab, pR_startPos, Quaternion.identity);
        
        playerL.GetComponent<PaddleManager>().setToPL();
        if (two_player)
        {
            playerR.GetComponent<PaddleManager>().setToPR();
        }
        else
        {
            playerR.GetComponent<PaddleManager>().setToCPU();
        }
        playerL.GetComponent<PaddleManager>().changePaddleSize(false);
        playerL.GetComponent<PaddleManager>().changePaddleSpeed(false);
        playerR.GetComponent<PaddleManager>().changePaddleSize(false);
        playerR.GetComponent<PaddleManager>().changePaddleSpeed(false); 
    }
/*----------------------------------------------
                   ITEMS
----------------------------------------------*/    
    private void spawnBricks()
    {
        if(bricks.Count > 0 || availablePositions.Count == 0) return; // If bricks are already in the scene, don't spawn more
        GameObject brickPrefab = Resources.Load<GameObject>("Prefabs/Brick");
        for (int i = 0; i < brickNum; i++) // Pick 4 unique positions
        {
            int randomIndex = Random.Range(0, availablePositions.Count);
            Vector2 randomPosition = availablePositions[randomIndex];
            GameObject newBrick = Instantiate(brickPrefab, randomPosition, Quaternion.identity);
            newBrick.GetComponent<BrickManager>().colorProbability = colorProbability; // Set the color probability for the brick
            availablePositions.RemoveAt(randomIndex); // Position is now taken
            bricks.Add(newBrick);
        }
    }
    public void RemoveBrick(GameObject brick)
    {
        if (bricks.Contains(brick))
        {
            availablePositions.Add(brick.transform.position); // Add position back to available positions
            bricks.Remove(brick);
        }
    }
    private void spawnDuplicator()
    {
        if(duplicators.Count >= 2 || availablePositions.Count == 0) return; // Limit to 2 duplicators in the scene
        int randomIndex = Random.Range(0, availablePositions.Count);
        GameObject duplicatorPrefab = Resources.Load<GameObject>("Prefabs/Duplicator");
        Vector2 randomPosition = availablePositions[randomIndex];
        GameObject duplicate = Instantiate(duplicatorPrefab, randomPosition, Quaternion.identity);
        availablePositions.RemoveAt(randomIndex); // Position is now taken
        duplicators.Add(duplicate);
    }
    public void RemoveDuplicator(GameObject duplicator)
    {
        if (duplicators.Contains(duplicator))
        {
            availablePositions.Add(duplicator.transform.position); // Add position back to available positions
            duplicators.Remove(duplicator);
        }
    }
    private void spawnComb()
    {
        if(combs.Count >= 2 || availablePositions.Count == 0) return; // Limit to 2 combs in the scene
        int randomIndex = Random.Range(0, availablePositions.Count);
        GameObject combPrefab = Resources.Load<GameObject>("Prefabs/Honeycomb");
        Vector2 randomPosition = availablePositions[randomIndex];
        GameObject comb = Instantiate(combPrefab, randomPosition, Quaternion.identity);
        availablePositions.RemoveAt(randomIndex); // Position is now taken
        combs.Add(comb);
        comb.GetComponent<Renderer>().material.color = Random.value < colorProbability ? Color.blue : Color.red;
    }
    public void RemoveComb(GameObject comb)
    {
        if (combs.Contains(comb))
        {
            availablePositions.Add(comb.transform.position); // Add position back to available positions
            combs.Remove(comb);
        }
    }
    private void spawnFreeze()
    {
        if (freezes.Count >= 2 || availablePositions.Count == 0) return; // Limit to 2 combs in the scene
        int randomIndex = Random.Range(0, availablePositions.Count);
        GameObject freezePrefab = Resources.Load<GameObject>("Prefabs/Freeze");
        Vector2 randomPosition = availablePositions[randomIndex];
        GameObject freeze = Instantiate(freezePrefab, randomPosition, Quaternion.identity);
        availablePositions.RemoveAt(randomIndex); // Position is now taken
        freezes.Add(freeze);
        freeze.GetComponent<Renderer>().material.color = Random.value < colorProbability ? Color.blue : Color.red;
    }
    public void RemoveFreeze(GameObject freeze)
    {
        if (freezes.Contains(freeze))
        {
            availablePositions.Add(freeze.transform.position); // Add position back to available positions
            freezes.Remove(freeze);
        }
    }
/*----------------------------------------------
                   BALLS
----------------------------------------------*/
    private void spawnBall()
    {
        // Spawn a ball into the scene
        Vector2 startPosition = new Vector2(0, 0);
        float angle = Random.Range(135f, 225f);
        Vector2 startVelocity = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * ballLaunchSpeed;
        ballManagerScript.createBall(startPosition, startVelocity, Color.white);
    }
    public void addBallToList(GameObject newBall)
    {
        balls.Add(newBall);
    }
    private void ClearAll()
    {
        GameObject[] ballsInScene = GameObject.FindGameObjectsWithTag("Ball");
        foreach (GameObject ball in ballsInScene)
        {
            Destroy(ball);
        }
        balls.Clear();
        GameObject[] paddlesInScene = GameObject.FindGameObjectsWithTag("Paddle");
        foreach (GameObject paddle in paddlesInScene)
        {
            Destroy(paddle);
        }
        GameObject[] bricksInScene = GameObject.FindGameObjectsWithTag("Brick");
        foreach (GameObject brick in bricksInScene)
        {
            Destroy(brick);
        }
        GameObject[] duplicatorsInScene = GameObject.FindGameObjectsWithTag("Duplicator");
        foreach (GameObject duplicator in duplicatorsInScene)
        {
            Destroy(duplicator);
        }
        GameObject[] combsInScene = GameObject.FindGameObjectsWithTag("Comb");
        foreach (GameObject comb in combsInScene)
        {
            Destroy(comb);
        }
        GameObject[] freezesInScene = GameObject.FindGameObjectsWithTag("Freeze");
        foreach (GameObject freeze in freezesInScene)
        {
            Destroy(freeze);
        }   
    }
    public void RemoveBall(GameObject ball)
    {
        if (balls.Contains(ball))
        {
            balls.Remove(ball);
        }
    }
    public int CountBalls()
    {
        return balls.Count;
    }
    public void ResetGame()
    {
        balls.Clear();
        spawnBall();
    }

/*----------------------------------------------
                   MENUS
----------------------------------------------*/
    public void StartGame()
    {
        scoreBoard.SetActive(true);
        
        // Load ball script
        GameObject ball = Resources.Load<GameObject>("Prefabs/Ball");
        ballManagerScript = ball.GetComponent<BallManager>();
        // Reset score
        pL_score = 0;
        pR_score = 0;
        co_opScore = 0;
        in_game = true;
        if (co_opMode)
        {
            scoreText.text = $"{co_opScore}";
            Vector2 scorePos = scoreText.GetComponent<RectTransform>().anchoredPosition;
            scorePos = new Vector2(scorePos.x + 100f, scorePos.y); // Center the score text
            countdownCoroutine = StartCoroutine(StartCountdown());
            countdownText.enabled = true;
        }
        else
        {
            scoreText.text = $"{pL_score} - {pR_score}";
            countdownText.enabled = false;
        }
        availablePositions.Clear();
        availablePositions.AddRange(itemSpawnPositions); // Copy all possible positions to available positions
        InvokeRepeating(nameof(balanceScore), 5f, 2f);
        if (isDuplicatorEnabled)
        {
            InvokeRepeating(nameof(spawnDuplicator), 5f, duplicatorRespawnWaitTime);
        }
        if (isBricksEnabled)
        {
            InvokeRepeating(nameof(spawnBricks), 15f, brickRespawnWaitTime); 
        }
        if (isCombEnabled)
        {
            InvokeRepeating(nameof(spawnComb), 10f, combRespawnWaitTime);
        }
        if (isFreezeEnabled)
        {
            InvokeRepeating(nameof(spawnFreeze), 20f, freezeRespawnWaitTime);
        }
        spawnPaddles();
        balls.Clear();
        spawnBall();
    }
    public void PressStart()
    {
        // Show main menu
        mainMenu.SetActive(false);
        modeMenu.SetActive(true);
        mainMenuButton.gameObject.SetActive(true);
        ResumeGame(); // Resume the game if paused
    }
    public void EnterGame(bool is2Player)
    {
        modeMenu.SetActive(false);
        mainMenuButton.gameObject.SetActive(false);
        two_player = is2Player;
    }
    public void SinglePlayer()
    {
        EnterGame(false);
        StartGame();
    }
    public void TwoPlayer()
    {
        EnterGame(true);
        StartGame();
    }
    public void CoOpMode()
    {
        EnterGame(true);
        co_opMode = true;
        StartGame(); 
    }
    public void Settings()
    {
        mainMenu.SetActive(false);
        settingsMenu.SetActive(true);
        mainMenuButton.gameObject.SetActive(true);
    }
    public void leftWon()
    {
        pauseMenu.SetActive(true);
        if(two_player)
        {
            headerText.text = "BLUE WON!";
        }
        else
        {
            headerText.text = "YOU WON!";
        }
        Time.timeScale = 0; // Pause the game
    }
    public void rightWon()
    {
        pauseMenu.SetActive(true);
        if (two_player)
        {
            headerText.text = "RED WON!";
        }
        else
        {
            headerText.text = "YOU LOST!";
        }
        Time.timeScale = 0; // Pause the game
    }
    public void BackToMainMenu()
    {
        mainMenu.SetActive(true);
        modeMenu.SetActive(false);
        settingsMenu.SetActive(false);
        scoreBoard.SetActive(false);
        pauseMenu.SetActive(false);
        mainMenuButton.gameObject.SetActive(false);
    }
    public void gameOver()
    {
        pauseMenu.SetActive(true);
        headerText.text = "Time's up!";
        countdownText.enabled = false;
        Time.timeScale = 0; // Pause the game
    }
    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        headerText.text = "PAUSED";
        Time.timeScale = 0; // Pause the game
    }
    public void RestartGame()
    {
        EndGame(); // Clear the game
        StartGame(); // Start the game (again)
    }
    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1; // Resume the game
    }
    public void QuitGame()
    {
        EndGame(); // Clear the game
        in_game = false;
        co_opMode = false;
        two_player = false;
        BackToMainMenu(); // Go back to main menu
    }    
    public void EndGame()
    {
        ResumeGame(); // Resume the game if paused
        ClearAll();
        CancelInvoke(); // Cancel all invokes
        if (countdownCoroutine != null)
        {
            StopCoroutine(countdownCoroutine); // Cancel the countdown if it was running
        }
    }
/*----------------------------------------------
                   SETTINGS
----------------------------------------------*/
    public void enableDuplicator(bool isEnabled)
    {
        isDuplicatorEnabled = isEnabled;
        if (isDuplicatorEnabled)
        {
            Debug.Log("Duplicator enabled");
        }
        else
        {
            Debug.Log("Duplicator disabled");
        }
    }
    public void enableBricks(bool isEnabled)
    {
        isBricksEnabled = isEnabled;
        if (isBricksEnabled)
        {
            Debug.Log("Bricks enabled");
        }
        else
        {
            Debug.Log("Bricks disabled");
        }
    }
    public void enableComb(bool isEnabled)
    {
        isCombEnabled = isEnabled;
        if (isCombEnabled)
        {
            Debug.Log("Comb enabled");
        }
        else
        {
            Debug.Log("Comb disabled");
        }
    }
    public void enableFreeze(bool isEnabled)
    {
        isFreezeEnabled = isEnabled;
        if (isFreezeEnabled)
        {
            Debug.Log("Freeze enabled");
        }
        else
        {
            Debug.Log("Freeze disabled");
        }
    }
    public void SetBallSpeed(int speedIndex)
    {
        switch (speedIndex)
        {
            case 0: // Slow
                ballLaunchSpeed = 3f;
                break;
            case 1: // Medium
                ballLaunchSpeed = 5f;
                break;
            case 2: // Fast
                ballLaunchSpeed = 7f;
                break;
            default:
                ballLaunchSpeed = 5f; // Default to Medium speed
                break;
        }
        Debug.Log("Ball speed set to: " + ballLaunchSpeed);
    }
    public void SetCoOpTimeLimit(int timeIndex)
    {
        switch (timeIndex)
        {
            case 0: // 1 minute
                coOpTimeLimit = 60f;
                break;
            case 1: // 2 minutes
                coOpTimeLimit = 120f;
                break;
            case 2: // 5 minutes
                coOpTimeLimit = 300f;
                break;
            case 3: // 10 minutes
                coOpTimeLimit = 600f;
                break;
            case 4: // 20 minutes
                coOpTimeLimit = 1200f;
                break;
            default:
                coOpTimeLimit = 120f; // Default to 2 minutes
                break;
        }
        Debug.Log("Co-op time limit set to: " + coOpTimeLimit/60 + " mins");
    }
    public void SetPointsToWin(string pointsToWinText)
    {
        if (int.TryParse(pointsToWinText, out int pointsToWin))
        {
            scoreThreshold = pointsToWin;
        }
        else
        {
            Debug.LogError("Invalid input for points to win. Defaulting to 10.");
            scoreThreshold = 10; // Default value
        }
        Debug.Log("Points to win set to: " + scoreThreshold);
    }
}