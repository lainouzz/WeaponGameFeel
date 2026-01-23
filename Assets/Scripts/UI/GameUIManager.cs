using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;

public class GameUIManager : MonoBehaviour
{
    public static GameUIManager Instance { get; private set; }

    public GameObject MainMenuPanel;

    public event Action<bool> OnPauseStateChanged;

    private GameInput gameInput;

    private bool isPaused = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
    }

    private void OnEnable()
    {
        gameInput = new GameInput();
        gameInput.Enable();
    }

    private void Update()
    {
        if(Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
           ToggleMainMenu();
        }
    }

    private void ToggleMainMenu()
    {
        if (isPaused)
            UnPause();
        else
            Pause();
    }

    private void Pause()
    {
        isPaused = true;
        if(MainMenuPanel != null)
        {
            MainMenuPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        OnPauseStateChanged?.Invoke(isPaused);
    }

    private void UnPause()
    {
        isPaused = false;
        if (MainMenuPanel != null)
        {
            MainMenuPanel.SetActive(false);
            Time.timeScale = 1f;
        }
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
        OnPauseStateChanged?.Invoke(isPaused);
    }

    public void SceneSwitch(int index)
    {
        // Later on add in asnc loading screen here
        SceneManager.LoadScene(index);

    }

    private void OnDisable()
    {
        gameInput.Disable();
    }
}
