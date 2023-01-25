using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    bool freeze = false;

    [Header("Score UI")]
    int score = 0;
    int comboScore = 0;
    Coroutine scoreCoroutine;
    [SerializeField] TextMeshProUGUI scoreText;
    [SerializeField] TextMeshProUGUI comboText;
    [SerializeField] Animator comboAnim;

    [Header("Final Cutscene")]
    [SerializeField] CinemachineFreeLook mainCamera;
    [SerializeField] CinemachineVirtualCamera cutsceneCamera;
    [SerializeField] CinemachineBrain cameraBrain;
    [SerializeField] CinemachineTargetGroup cutsceneTargetGroup;

    private void Awake()
    {
        Instance = this;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        StartCoroutine(ShakeCombo());
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
            Application.Quit();
        if (Input.GetKeyDown(KeyCode.R))
            OnRestart();
        if(Input.GetKeyDown(KeyCode.Q))
        {
            freeze = !freeze;
            Time.timeScale = freeze ? 0 : 1; 
        }
    }

    public void AddScore()
    {
        //comboText.color = new Color(comboText.color.r, comboText.color.g, comboText.color.b, 255);
        comboAnim.Play("Empty");
        comboScore++;
        comboText.text = $"x{comboScore.ToString()}";
        if (scoreCoroutine != null) StopCoroutine(scoreCoroutine);
        StartCoroutine(incrementScore());

        IEnumerator incrementScore()
        {
            var duration = 0f;
            var startScore = score;
            score += comboScore * 10;
            while(duration < 1f)
            {
                duration += Time.deltaTime;
                scoreText.text = ((int)Mathf.Lerp(startScore, score, duration)).ToString();
                yield return null;
            }
        }
    }

    public void CancelCombo()
    {
        comboScore = 0;
        comboAnim.Play("Fade");
    }

    void OnRestart()
    {
        Time.timeScale = 1;
        SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().buildIndex);
    }

    public void StartCutscene() => StartCoroutine(EndCombatCutscene());

    IEnumerator EndCombatCutscene()
    {
        cutsceneCamera.enabled = true;
        cutsceneCamera.transform.position = cutsceneTargetGroup.transform.position;
        var enemy = EnemyAI.Instance.enemies[0];
        cutsceneTargetGroup.AddMember(enemy.transform, 1, 1);
        cutsceneCamera.transform.position = enemy.transform.position + enemy.transform.right * 5;

        Time.timeScale = 0.3f;
        yield return new WaitForSeconds(1f);
        Time.timeScale = 1;
        cutsceneCamera.enabled = false;
    }

    IEnumerator ShakeCombo()
    {
        var originalPos = comboText.transform.position;
        while(Application.isPlaying)
        {
            if(comboText.enabled)
                comboText.transform.position = originalPos + Random.onUnitSphere * 3;   
            yield return new WaitForSeconds(0.05f);
        }
    }
}
