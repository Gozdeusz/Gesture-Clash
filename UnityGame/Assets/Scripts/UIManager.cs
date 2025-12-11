using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Zarzadza interfejsem uzytkownika
/// </summary>
public class UIManager : MonoBehaviour
{
    public int maxHP = 9;

    [Header("Sprite ikonek zycia gracza")]
    public Sprite hpFullBlue;
    public Sprite hpFullRed;
    public Sprite hpEmpty;

    [Header("Sprite ikonek punktow gracza")]
    public Sprite bluePoint;
    public Sprite redPoint;
    public Sprite noPoint;
    public Sprite blueWinPoint;
    public Sprite redWinPoint;
    public Sprite noWinPoint;

    [Header("Panele graczy")]
    public Transform playerBluePanel;
    public Transform playerRedPanel;

    [Header("Tokens & Win Point")]
    public PointToken pBlueToken1;
    public PointToken pBlueToken2;

    public WinToken winPoint;

    public PointToken pRedToken2;
    public PointToken pRedToken1;

    [Header("Game Over UI")]
    public GameObject finalPanel;
    public TMP_Text resultText;
    public GameObject resetButton;

    [Header("Timer & Info")]
    public TMP_Text timerText;
    public UIInfoPanel infoPanel;

    [Header("Pause Menu")]
    public GameObject pausePanel;

    // Przechowuje referencje do utworzonych paskow zdrowia
    private readonly List<Image> playerBlueIcons = new();
    private readonly List<Image> playerRedIcons = new();

    // Aktualne wartosci zdrowia
    private int playerBlueHP;
    private int playerRedHP;

    // Punkty graczy
    private int playerBluePoints = 0;
    private int playerRedPoints = 0;

    // Zmienne stanu
    private bool isPaused = false;
    private bool end = false;

    /// <summary>
    /// Inicjalizacja UI
    /// </summary>
    void Awake()
    {
        playerBlueHP = maxHP;
        playerRedHP = maxHP;

        GenerateHPIcons(playerBluePanel, playerBlueIcons, leftToRight: true);
        GenerateHPIcons(playerRedPanel, playerRedIcons, leftToRight: false);

        finalPanel.SetActive(false);
        UpdateHPUI();

        if (resultText != null) resultText.text = "";
        if (resetButton != null) resetButton.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(false);
    }

    /// <summary>
    /// Wlaczenie pauzy - ESC
    /// </summary>
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    /// <summary>
    /// Generuje pasek HP
    /// </summary>
    private void GenerateHPIcons(Transform panel, List<Image> list, bool leftToRight)
    {
        List<Image> tempList = new();

        for (int i = 0; i < maxHP; i++)
            tempList.Add(CreateIcon(panel, leftToRight ? "blue" : "red"));

        if (!leftToRight)
            tempList.Reverse();

        list.AddRange(tempList);
    }

    /// <summary>
    /// Tworzy pojedyncza ikone HP
    /// </summary>
    private Image CreateIcon(Transform parent, string color)
    {
        GameObject icon = new("HP", typeof(Image));
        icon.transform.SetParent(parent);

        RectTransform rt = icon.GetComponent<RectTransform>();
        rt.localScale = Vector3.one;
        rt.sizeDelta = new Vector2(60, 60);

        Image img = icon.GetComponent<Image>();
        img.sprite = (color == "blue") ? hpFullBlue : hpFullRed;

        return img;
    }

    /// <summary>
    /// Aktualizuje timer, zmienia kolor na czerowny przy ostatnich 3 sekundach
    /// </summary>
    public void UpdateTimer(float timeRemaining)
    {
        if (timerText != null)
        {
            float time = Mathf.Max(0, timeRemaining);
            timerText.text = Mathf.CeilToInt(time).ToString();

            if (time <= 3) timerText.color = Color.red;
            else timerText.color = Color.white;
        }
    }

    /// <summary>
    /// Ukrywa licznik czasu - usatwia tekst na pusty
    /// </summary>
    public void HideTimer()
    {
        if (timerText != null) timerText.text = "";
    }

    /// <summary>
    /// Zwraca aktualna ilosc zdrowia wskazanego gracza
    /// </summary>
    /// <param name="playerIndex">1 - player, 2 - AI</param>
    public int GetPlayerHP(int playerIndex)
    {
        return (playerIndex == 1) ? playerBlueHP : playerRedHP;
    }
    
    /// <summary>
    /// Wizualny efekt zadania obrazen dla garcza
    /// </summary>
    public void DamagePlayer(int player, int dmg)
    {
        if (player == 1) playerBlueHP = Mathf.Clamp(playerBlueHP - dmg, 0, maxHP);
        else playerRedHP = Mathf.Clamp(playerRedHP - dmg, 0, maxHP);

        UpdateHPUI();
    }

    /// <summary>
    /// Aktualizuje stan wizualny tokenow punktow i sprawdza warunek zwyciêstwa
    /// </summary>
    void SetPoint()
    {
        if (playerBluePoints >= 1) pBlueToken1.SetActive(bluePoint);
        if (playerBluePoints >= 2) pBlueToken2.SetActive(bluePoint);

        if (playerRedPoints >= 1) pRedToken1.SetActive(redPoint);
        if (playerRedPoints >= 2) pRedToken2.SetActive(redPoint);

        if (playerBluePoints >= 3)
        {
            winPoint.SetBlue();
            end = true;
        }
        else if (playerRedPoints >= 3)
        {
            winPoint.SetRed();
            end = true;
        }
    }

    /// <summary>
    /// Przyznaje punkt zwyciestwa wskazanemu garczowi
    /// </summary>
    public void AwardPointTo(int playerIndex)
    {
        if (playerIndex == 1) playerBluePoints++;
        else playerRedPoints++;

        SetPoint();
    }

    /// <summary>
    /// Sprawdzanie warunkow zwyciezstwa
    /// </summary>
    public bool IsGameFinished()
    {
        return end;
    }

    /// <summary>
    /// Wyswietla ekran konca gry
    /// </summary>
    public void ShowGameOverScreen(string message)
    {
        if (finalPanel != null) finalPanel.SetActive(true);

        if (resultText != null)
        {
            resultText.text = message;
            resultText.gameObject.SetActive(true);
        }

        if (resetButton != null) resetButton.SetActive(true);
    }  

    /// <summary>
    /// Reset paska zdrowia
    /// </summary>
    private void UpdateHPUI()
    {
        for (int i = 0; i < maxHP; i++)
        {
            playerBlueIcons[i].sprite = i < playerBlueHP ? hpFullBlue : hpEmpty;
            playerRedIcons[i].sprite = i < playerRedHP ? hpFullRed : hpEmpty;
        }
    }

    /// <summary>
    /// Reset logiki HP graczy
    /// </summary>
    public void ResetHP()
    {
        playerBlueHP = maxHP;
        playerRedHP = maxHP;
        UpdateHPUI();
    }

    /// <summary>
    /// Resetuje caly interfejs 
    /// </summary>
    public void ResetGameUI()
    {
        ResetHP();

        if (pBlueToken1 != null) pBlueToken1.ResetToken();
        if (pBlueToken2 != null) pBlueToken2.ResetToken();
        if (pRedToken1 != null) pRedToken1.ResetToken();
        if (pRedToken2 != null) pRedToken2.ResetToken();

        if (winPoint != null) winPoint.ResetToken();

        playerBluePoints = 0;
        playerRedPoints = 0;
        end = false;

        if (finalPanel != null) finalPanel.SetActive(false);
        if (resultText != null) resultText.gameObject.SetActive(false);
        if (resetButton != null) resetButton.SetActive(false);
    }

    /// <summary>
    /// Wlacza ekran pauzy zatrzymujac lub wznawiajac czas gry
    /// </summary>
    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    /// <summary>
    /// Event dla przycisku restartu
    /// </summary>
    public void OnResetButtonClicked()
    {
        GameManager.Instance.ResetGame();
    }
    
    /// <summary>
    /// Wznowienie gry
    /// </summary>
    public void OnResumeClick()
    {
        if (isPaused) TogglePause();
    }

    /// <summary>
    /// Powrot do Menu Glownego
    /// </summary>
    public void OnExitToMenuClick()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenuScene");
    }   
}
