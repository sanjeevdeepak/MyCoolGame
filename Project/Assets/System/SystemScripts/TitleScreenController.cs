using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class TitleScreenController : MonoBehaviour
{
    public Animator animator;
    public float timeToWait = 2f;
    private bool startPressed = false;

    public void Update()
    {
        if (!startPressed && Input.anyKey)
        {
            startPressed = true;
            StartCoroutine(StartGameEnumerator());
        }
    }

    private IEnumerator StartGameEnumerator()
    {
        if (animator != null) animator.SetTrigger("begin");
        yield return new WaitForSeconds(timeToWait);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
