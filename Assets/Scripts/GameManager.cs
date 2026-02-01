using UnityEngine;
using TMPro;
using System.Collections;
using System;
using UnityEngine.SceneManagement;

public class GameManager : Singleton<GameManager>
{
    public LayerMask anchorLayer;
    public TMP_Text scoreText;
    public Transform covid;
    public float largestScale;
    public float gameplayTime;
    public AnimationCurve scaleCurve;
    public GameObject[] endGameInvisibleObjs;
    public GameObject[] endGameVisibleObjs;
    public MyButton restartButton, quitButton;

    float startScale;
    float startTime;
    [HideInInspector] [NonSerialized] public bool gameEnded=false;
    void Start()
    {
        StartCoroutine(UpdateScore());
        startScale=covid.localScale.x;
        restartButton.onClick+=ReloadCurrentScene;
        quitButton.onClick+=Application.Quit;
        startTime=Time.time;
    }
    void FixedUpdate() {
        float t=(Time.time-startTime)/gameplayTime;
        float scale=Mathf.Lerp(startScale, largestScale, scaleCurve.Evaluate(t));
        covid.localScale=new Vector3(scale,scale,scale);
        if(t>1&&!gameEnded) EndGame();
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.R))
            ReloadCurrentScene();
    }
    IEnumerator UpdateScore()
    {
        WaitForSeconds wait=new WaitForSeconds(1f);
        while (true) {
            if(gameEnded) yield break;
            float coverPercentage=Mathf.Round(10000f*CoverageDetector.inst.GetCoverage())/100f;
            scoreText.text=$"{coverPercentage}% masked";
            yield return wait;
        }
    }
    void EndGame()
    {
        gameEnded=true;
        foreach(GameObject go in endGameInvisibleObjs)
        {
            go.SetActive(false);
        }
        foreach(GameObject go in endGameVisibleObjs)
            go.SetActive(true);
        MaskBox.inst.CreateMask().endGameDraggable=true;
    }
    public static void ReloadCurrentScene()
    {
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.buildIndex);
    }
}