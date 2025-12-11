using TMPro;
using System.Collections;
using UnityEngine;

/// <summary>
/// Zarzadza centralnym panelem informacyjnym interfejsu
/// </summary>
public class UIInfoPanel : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text infoText;
    public GameObject panelObject;

    void Awake() { ClearText(); }

    /// <summary>
    /// Czysci tekst
    /// </summary>
    public void ClearText()
    {
        if (infoText != null) infoText.text = "";
        if (panelObject != null) panelObject.SetActive(false);
    }

    /// <summary>
    /// Wyswietla sekwencjê odliczania
    /// </summary>
    public IEnumerator ShowCountdownSequence()
    {
        if (panelObject != null) panelObject.SetActive(true);

        string[] counts = { "3", "2", "1", "START" };

        foreach (var msg in counts)
        {
            infoText.text = msg;
            yield return new WaitForSecondsRealtime(0.6f);
        }

        ClearText();
    }

    /// <summary>
    /// Sluzy do wyswietlania dowolnej wiadomosci przez okreslony czas
    /// </summary>
    /// <param name="message">Tresc wiadomosci</param>
    /// <param name="duration">Czas wyswietlania w sekundach</param>
    public IEnumerator ShowMessageForSeconds(string message, float duration)
    {
        if (panelObject != null) panelObject.SetActive(true);
        infoText.text = message;
        yield return new WaitForSeconds(duration);
        ClearText();
    }

    /// <summary>
    /// Wiadomosc: Wejscie w faze 2
    /// </summary>
    public void ShowPhase2Warning()
    {
        StartCoroutine(ShowMessageForSeconds("REORDER TIME", 2.0f));
    }

    /// <summary>
    /// Wiadomosc: Koniec fazy 2
    /// </summary>
    public void ShowPhase2End()
    {
        StartCoroutine(ShowMessageForSeconds("TIME'S UP", 2.0f));
    }

    /// <summary>
    /// Wiadomosc: Wejscie w faze 3
    /// </summary>
    public void ShowFightPrepare()
    {
        StartCoroutine(ShowMessageForSeconds("FIGHT TIME", 2.0f));
    }

    /// <summary>
    /// Wiadomosc: Remis
    /// </summary>
    public void ShowRematch()
    {
        StartCoroutine(ShowMessageForSeconds("REMATCH", 2.0f));
    }

    /// <summary>
    /// Wiadomosc: Przyznanie punktu
    /// </summary>
    public void ShowPointInfo(string whoScored)
    {
        StartCoroutine(ShowMessageForSeconds("Point for " + whoScored, 3.0f));
    }
}
