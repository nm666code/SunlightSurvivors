using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    private static GameOverUI instance;
    private bool isVisible;
    private Texture2D overlayTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Create()
    {
        if (instance != null)
        {
            return;
        }

        GameObject uiObject = new GameObject("Game Over UI");
        instance = uiObject.AddComponent<GameOverUI>();
    }

    private void Awake()
    {
        instance = this;
        overlayTexture = new Texture2D(1, 1);
        overlayTexture.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.75f));
        overlayTexture.Apply();
    }

    public static void Show()
    {
        if (instance == null)
        {
            Create();
        }

        instance.isVisible = true;
    }

    private void Update()
    {
        if (isVisible && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            Restart();
        }
    }

    private void OnGUI()
    {
        if (!isVisible)
        {
            return;
        }

        GUIStyle overlayStyle = new GUIStyle(GUI.skin.box);
        overlayStyle.normal.background = overlayTexture;
        GUI.Box(new Rect(0f, 0f, Screen.width, Screen.height), GUIContent.none, overlayStyle);

        float panelWidth = Mathf.Min(720f, Screen.width * 0.9f);
        float panelHeight = 230f;
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);

        GUIStyle titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(Mathf.Clamp(Screen.height * 0.08f, 42f, 72f)),
            fontStyle = FontStyle.Bold,
            wordWrap = false
        };
        titleStyle.normal.textColor = Color.white;

        GUIContent title = new GUIContent("GAME OVER");
        float titleWidth = titleStyle.CalcSize(title).x;
        if (titleWidth > panelRect.width)
        {
            titleStyle.fontSize = Mathf.Max(
                28,
                Mathf.FloorToInt(titleStyle.fontSize * panelRect.width / titleWidth));
        }

        GUI.Label(new Rect(panelRect.x, panelRect.y, panelRect.width, 100f), title, titleStyle);

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold
        };

        Rect buttonRect = new Rect(
            panelRect.x + (panelRect.width - 220f) * 0.5f,
            panelRect.y + 125f,
            220f,
            60f);

        if (GUI.Button(buttonRect, "Restart  (R)", buttonStyle))
        {
            Restart();
        }
    }

    private void OnDestroy()
    {
        if (overlayTexture != null)
        {
            Destroy(overlayTexture);
        }
    }

    private static void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
