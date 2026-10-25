using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.InputSystem;


public class WinScreenController : MonoBehaviour
{
    public Animator animator;
    public float noInputTime = 5f;
    public float endAnimTime = 2f;
    private bool startPressed = false;
    private float startTime = 0;

    void Start()
    {
        startTime = Time.time;
    }

    public void Update()
    {
        if (!startPressed && Input.anyKey && (Time.time - startTime) > noInputTime)
        {
            startPressed = true;
            StartCoroutine(EndameEnumerator());
        }
    }

    private IEnumerator EndameEnumerator()
    {
        if (animator != null) animator.SetTrigger("begin");
        yield return new WaitForSeconds(endAnimTime);
        Debug.Log("Quit!");
        Application.Quit();
    }
}
