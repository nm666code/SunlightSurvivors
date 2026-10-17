using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseGame : MonoBehaviour
{
    public TextMeshProUGUI PauseText;
    private bool isPaused = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // If the 'p' key is pressed, pause or resume the game.
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            if (isPaused)
            {
                PauseText.gameObject.SetActive(false);
                Time.timeScale = 1f;
                isPaused = false;
                Debug.Log("Game is unpaused");
            }
            else
            {
                PauseText.gameObject.SetActive(true);
                Time.timeScale = 0f;
                isPaused = true;
                Debug.Log("Game is paused");
            }
        }
    }
}
