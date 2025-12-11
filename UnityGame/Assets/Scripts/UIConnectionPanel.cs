using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Zarz¹dza panelem wyswietlanym podczas oczekiwania na polaczenie sieciowe z Pythonem.
/// Po 5 sekundach oczekiwania wyswietla przycisk wyjscia
/// </summary>
public class UIConnectionPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelRoot;
    public TMP_Text statusText;
    public GameObject connectedImage;
    public GameObject quitButton;

    private Coroutine timeoutCoroutine;

    /// <summary>
    /// Ukrywa przycisk wyjscia
    /// </summary>
    void Awake()
    {
        if (quitButton != null) quitButton.SetActive(false);
    }

    /// <summary>
    /// Ustawia panel w stan oczekiwania i wlacza odliczanie czasu
    /// </summary>
    public void StartWaitForConnection()
    {
        panelRoot.SetActive(true);
        statusText.text = "Waiting for connection with Python...";
        statusText.color = Color.yellow;
        connectedImage.SetActive(false);

        if (quitButton != null) quitButton.SetActive(false);

        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);
        timeoutCoroutine = StartCoroutine(ShowQuitButton());
    }

    /// <summary>
    /// Odliczanie czasu do wyswietlenia przycisku
    /// </summary>
    IEnumerator ShowQuitButton()
    {
        yield return new WaitForSecondsRealtime(5.0f);

        if (panelRoot.activeSelf && !connectedImage.activeSelf)
        {
            if (quitButton != null) quitButton.SetActive(true);
        }
    }

    /// <summary>
    /// Obsluguje moment polaczenia
    /// </summary>
    public void SetConnected()
    {
        if (timeoutCoroutine != null) StopCoroutine(timeoutCoroutine);

        statusText.text = "Connected!";
        statusText.color = Color.green;
        connectedImage.SetActive(true);

        if (quitButton != null) quitButton.SetActive(false);
    }

    /// <summary>
    /// Deaktywuje panel
    /// </summary>
    public void Close()
    {
        panelRoot.SetActive(false);
    }

    /// <summary>
    /// Obsluguje wyjscie do glownego menu
    /// </summary>
    public void OnQuitButton()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }
}