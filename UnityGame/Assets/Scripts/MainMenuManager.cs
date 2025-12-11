using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Obsluguje glowne menu gry
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("References")]
    public GestureBookController gestureBook;

    /// <summary>
    /// Przelacza na scene gry
    /// </summary>
    public void OnStartClick()
    {
        SceneManager.LoadScene("GameScene");
    }

    /// <summary>
    /// Wlacza ksiege gestow
    /// </summary>
    public void OnBookClick()
    {
        gestureBook.OpenBook();
    }

    /// <summary>
    /// Zamyka gre
    /// </summary>
    public void OnQuitClick()
    {
        Application.Quit();
    }
}