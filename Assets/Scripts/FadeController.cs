using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FadeController : MonoBehaviour
{
    public static FadeController Instance { get; private set; }

    [Header("Fade Settings")]
    public Image fadeImage; // Black image on canvas
    public float fadeDuration = 2f;
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Credits Settings")]
    public GameObject creditsPanel; // Panel containing credits text
    public float creditsDelay = 1f; // Delay after fade before showing credits
    public float creditsDuration = 10f; // How long to show credits

    [Header("Optional: Credits Scroll")]
    public ScrollRect creditsScrollRect;
    public float scrollSpeed = 0.1f; // Speed of auto-scroll

    private bool isFading = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Ensure fade image starts transparent
        if (fadeImage != null)
        {
            Color c = fadeImage.color;
            c.a = 0f;
            fadeImage.color = c;
            fadeImage.gameObject.SetActive(true);
        }

        // Ensure credits start hidden
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(false);
        }
    }

    public void StartFadeAndCredits()
    {
        if (isFading) return;

        StartCoroutine(FadeAndCreditsSequence());
    }

    IEnumerator FadeAndCreditsSequence()
    {
        isFading = true;

        // Fade to black
        yield return StartCoroutine(FadeToBlack());

        // Wait a moment
        yield return new WaitForSeconds(creditsDelay);

        // Show credits
        if (creditsPanel != null)
        {
            creditsPanel.SetActive(true);

            // If scrolling is enabled, scroll the credits
            if (creditsScrollRect != null)
            {
                yield return StartCoroutine(ScrollCredits());
            }
            else
            {
                // Just wait for credits duration
                yield return new WaitForSeconds(creditsDuration);
            }
        }

        // Credits complete - reload scene
        Debug.Log("Credits sequence complete - reloading scene");
        RestartGame();
    }

    IEnumerator FadeToBlack()
    {
        if (fadeImage == null)
        {
            Debug.LogWarning("Fade Image not assigned!");
            yield break;
        }

        float elapsed = 0f;
        Color startColor = fadeImage.color;
        Color endColor = new Color(0, 0, 0, 1); // Fully opaque black

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = fadeCurve.Evaluate(elapsed / fadeDuration);

            fadeImage.color = Color.Lerp(startColor, endColor, t);

            yield return null;
        }

        // Ensure final state
        fadeImage.color = endColor;

        Debug.Log("Fade to black complete");
    }

    IEnumerator ScrollCredits()
    {
        if (creditsScrollRect == null) yield break;

        // Start from bottom
        creditsScrollRect.verticalNormalizedPosition = 0f;

        float elapsed = 0f;

        while (elapsed < creditsDuration)
        {
            elapsed += Time.deltaTime;

            // Scroll upwards
            creditsScrollRect.verticalNormalizedPosition += scrollSpeed * Time.deltaTime;

            // Clamp to 0-1 range
            creditsScrollRect.verticalNormalizedPosition = Mathf.Clamp01(creditsScrollRect.verticalNormalizedPosition);

            yield return null;
        }

        Debug.Log("Credits scroll complete");
    }

    // Optional: Method to restart game or return to menu
    public void RestartGame()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
