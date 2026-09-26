using UnityEngine;
using TMPro;

public class PauseController : MonoBehaviour
{
    public GameObject pauseContainer;
    private bool isPaused = false;
    private bool pausedThisPress = false;

    //Quit
    private bool holdingPause = false;
    private float holdPauseStartTime = 0;
    private readonly float quitHoldTime = 2f;

    //Restart from Checkpoint
    private bool holdingRestart = false;
    private float holdRestartStartTime = 0;
    private readonly float retstartHoldTime = 1f;
    private PlayerController playerController;

    public TMP_Text pauseText;

    void Start()
    {
        pauseContainer.SetActive(false);
        #if UNITY_WEBGL
        if(pauseText != null)
        {
            pauseText.text = "<b>Paused</b>\n\n<size=75%>Hold R to restart from checkpoint.</size>";
        }
        #endif
    }
    
    void Update()
    {
        CheckForHoldPause();
        CheckForHoldRestart();
    }

    private void CheckForHoldPause()
    {
        if(Input.GetButtonDown("Pause"))
        {
            if (!isPaused)
            {
                Pause();
                pausedThisPress = true;
            }
            else
            {
                holdingPause = true;
                holdPauseStartTime = Time.realtimeSinceStartup;
            }
        }

        if(Input.GetButtonUp("Pause"))
        {
            holdingPause = false;
            if (!pausedThisPress)
            {
                if (isPaused) UnPause();
            }
            else
            {
                pausedThisPress = false;
            }
        }

        if (holdingPause && Time.realtimeSinceStartup - holdPauseStartTime > quitHoldTime)
        {
            Debug.Log("Quit!");
            Application.Quit();
        }
    }

    private void CheckForHoldRestart()
    {
        if(isPaused)
        {
            if(Input.GetButtonDown("Restart"))
            {
                holdingRestart = true;
                holdRestartStartTime = Time.realtimeSinceStartup;
            }

            if(Input.GetButtonUp("Restart"))
            {
                holdingRestart = false;
            }

            if (holdingRestart && Time.realtimeSinceStartup - holdRestartStartTime > retstartHoldTime)
            {
                UnPause();
                GetPlayerController().Respawn();
            }
        }
    }

    private PlayerController GetPlayerController()
    {
        if(playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
        }
        return playerController;
    }

    public void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pauseContainer.SetActive(true);
        AudioController.GetMixer().SetFloat("pauseVolume", -15);
    }

    public void UnPause()
    {
        isPaused = false;
        Time.timeScale = 1.0f;
        pauseContainer.SetActive(false);
        AudioController.GetMixer().SetFloat("pauseVolume", 0);
    }
}
