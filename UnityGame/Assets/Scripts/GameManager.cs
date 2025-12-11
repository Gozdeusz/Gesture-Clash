using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Definicje faz gry
public enum GamePhase { Selection, PrepareReorder, Reordering, PrepareFight, Fight, Summary }

/// <summary>
/// Zarzadza gra.
/// Odpowiada za:
/// - Przep³yw rund i faz gry
/// - Wymiane informacji z systemem rozpoznawania gestow i UI
/// - Zarz¹dzanie srodowiskiem gry
/// - Logike pojedynkow
/// - Niektore efekty wizualne
/// </summary>
public class GameManager : MonoBehaviour
{
    // Singleton
    public static GameManager Instance;
    [HideInInspector] public GamePhase currentPhase;

    [Header("References")]
    public UIManager uiManager;

    [Header("Table")]
    public List<CardSlot> playerSlots;
    public List<CardSlot> enemySlots;
    public GameObject cardPrefab;

    [Header("Fight Positions")]
    public Transform playerFightPos;
    public Transform enemyFightPos;

    [Header("Gestures Data")]
    public List<GestureData> allGestures;

    [Header("Reorder Phase")]
    public float reorderTime = 10f;
    private float reorderTimer;

    [Header("UI Panels")]
    public UIConnectionPanel connectionPanel;

    [Header("Camera Zoom Settings")]
    public Camera mainCamera;
    [HideInInspector]  public float camSizeZoomedOut = 7f;
    [HideInInspector]  public float camSizeZoomedIn = 5f;
    [HideInInspector] public float zoomDuration = 1.5f;

    [Header("Dissolve Shader Color")]
    public Color cleanupGlowColor;
    public Color dissolveGlowColor;

    // Logika
    private readonly int[] playerGesturesIDs = new int[3];
    private readonly int[] enemyGesturesIDs = new int[3];
    private int gesturesReceivedCount = 0;

    // Macierz obrazen
    private readonly int[,] adjacencyMatrix = new int[,] {
       {1, 0, 2, 0, 3, 0, 0},
       {0, 1, 0, 0, 0, 3, 2},
       {0, 0, 1, 3, 2, 0, 0},
       {0, 3, 0, 1, 0, 2, 0},
       {0, 2, 0, 0, 1, 0, 3},
       {2, 0, 3, 0, 0, 1, 0},
       {3, 0, 0, 2, 0, 0, 1}
    };

    void Awake()
    {
        Instance = this;
    }

    //Startowa konfiguracja, sprawdza stan polaczenia z pythonem
    void Start()
    {
        Time.timeScale = 1f;
        uiManager.ResetGameUI();
        uiManager.HideTimer();
        SetCameraZoom(camSizeZoomedOut);

        // Sprawdzenie polaczenia z skryptem pythona
        if (PythonReceiver.Instance != null && PythonReceiver.Instance.IsConnected)
        {
            if (connectionPanel != null)
            {
                connectionPanel.SetConnected();
                StartCoroutine(QuickStartSequence());
            }
            else
            {
                StartCoroutine(AnimateCameraZoom(camSizeZoomedIn));
                StartNewRound();
            }
        }
        else
        {
            if (connectionPanel != null) 
                connectionPanel.StartWaitForConnection();
        }
    }

    /// <summary>
    /// G³ówna pêtla gry
    /// </summary>
    void Update()
    {
        if (Time.timeScale == 0) return;

        // Obsluga timera w fazie przesuwania kart
        if (currentPhase == GamePhase.Reordering)
        {
            reorderTimer -= Time.deltaTime;
            uiManager.UpdateTimer(reorderTimer);

            if (reorderTimer <= 0)
            {
                StartCoroutine(ReorderingToFight());
            }
        }
    }


    // ----- Laczenie z pythonem -----

    /// <summary>
    /// Skrocona sekwencja startowa gdy polaczenie z Pythonem jest juz aktywne
    /// </summary>
    IEnumerator QuickStartSequence()
    {
        yield return new WaitForSeconds(1.0f);
        if (connectionPanel != null) connectionPanel.Close();
        StartNewRound();
    }

    /// <summary>
    /// Sekwencja po nawiazaniu polaczenia
    /// </summary>
    IEnumerator ConnectionSequence()
    {
        // Informacja o nawiazaniu polaczenia
        if (connectionPanel != null)
            connectionPanel.SetConnected();

        yield return new WaitForSecondsRealtime(1.5f);

        if (connectionPanel != null)
            connectionPanel.Close();

        // Wznowienie lub rozpoczecie nowej gry
        if (Time.timeScale == 0)
        {
            StartCoroutine(AnimateCameraZoom(camSizeZoomedIn));
            yield return StartCoroutine(uiManager.infoPanel.ShowCountdownSequence());
            Time.timeScale = 1f;
        }
        else
        {
            StartNewRound();
        }
    }

    /// <summary>
    /// Event wywolany gdy PythonReceiver nawiaze polaczenie
    /// </summary>
    public void OnPythonConnected()
    {
        StartCoroutine(ConnectionSequence());
    }

    /// <summary>
    /// Event wywo³any przy utracie po³¹czenia z Pythonemm
    /// </summary>
    public void OnPythonDisconnected()
    {
        Time.timeScale = 0f;

        if (connectionPanel != null)
            connectionPanel.StartWaitForConnection();
    }

    // ----- Sekwencje rozgrywki -----

    /// <summary>
    /// Zatrzymuje stare procesy i rozpoczyna nowa runde
    /// </summary>
    public void StartNewRound()
    {
        StopAllCoroutines();
        StartCoroutine(NewRoundSequence());
    }

    /// <summary>
    /// Glowna sekwencja inicjalizujaca runde
    /// 1. Czysci stol
    /// 2. Resetuje dane 
    /// 3. Wyswietla odliczanie 
    /// 4. Przechodzi do fazy zbierania gestow
    /// </summary>
    IEnumerator NewRoundSequence()
    {
        StartCoroutine(AnimateCameraZoom(camSizeZoomedIn));

        // Czysczenie stolu
        yield return StartCoroutine(ClearTable());

        // Reset UI i danych
        uiManager.ResetHP();
        uiManager.HideTimer();

        gesturesReceivedCount = 0;
        for (int i = 0; i < 3; i++)
        {
            playerGesturesIDs[i] = -1;
            enemyGesturesIDs[i] = -1;
        }

        // Odliczanie do startu rundy
        yield return StartCoroutine(uiManager.infoPanel.ShowCountdownSequence());

        // Rozpoeczecie fazy zbierania gestow
        currentPhase = GamePhase.Selection;
    }

    /// <summary>
    /// Usuwa wszystkie pozsotale gesty ze stolu
    /// </summary>
    IEnumerator ClearTable()
    {
        float dissolveTime = 1.0f;
        bool anyCardRemoved = false;

        foreach (var slot in playerSlots)
        {
            if (slot.currentCard != null)
            {
                StartCoroutine(slot.currentCard.AnimDissolveShader(dissolveTime, cleanupGlowColor));
                slot.currentCard = null;
                anyCardRemoved = true;
            }
        }

        foreach (var slot in enemySlots)
        {
            if (slot.currentCard != null)
            {
                StartCoroutine(slot.currentCard.AnimDissolveShader(dissolveTime, cleanupGlowColor));
                slot.currentCard = null;
                anyCardRemoved = true;
            }
        }

        if (anyCardRemoved)
        {
            yield return new WaitForSeconds(dissolveTime);
        }
    }

    /// <summary>
    /// Odbiera sygnal o wybranym gescie
    /// Umieszcza kartê na stole i zarzadza kolejnoscia tur
    /// </summary>
    public void ReceiveGesture(int gestureID, bool isPlayer)
    {
        if (currentPhase != GamePhase.Selection) return;

        if (isPlayer)
        {
            int index = GetFirstEmptyIndex(playerGesturesIDs);
            if (index != -1)
            {
                playerGesturesIDs[index] = gestureID;
                SpawnCard(gestureID, playerSlots[index]);
                gesturesReceivedCount++;
                if (gesturesReceivedCount < 6) StartCoroutine(EnemyTurn());
            }
        }
        else
        {
            int index = GetFirstEmptyIndex(enemyGesturesIDs);
            if (index != -1)
            {
                enemyGesturesIDs[index] = gestureID;
                SpawnCard(gestureID, enemySlots[index]);
                gesturesReceivedCount++;
            }
        }

        if (gesturesReceivedCount >= 6) StartCoroutine(ReceiveToReordering());
    }

    /// <summary>
    /// Przejscie z fazy zbierania gestow do fazy przesuwania gestow
    /// </summary>
    IEnumerator ReceiveToReordering()
    {
        currentPhase = GamePhase.PrepareReorder;
        uiManager.infoPanel.ShowPhase2Warning();
        yield return new WaitForSeconds(1.0f);
        ReorderingPhase();
    }

    /// <summary>
    /// Uruchamia faze przesuwania
    /// Wlacza timer i odblokowuje drag and drop
    /// </summary>
    void ReorderingPhase()
    {
        currentPhase = GamePhase.Reordering;
        reorderTimer = reorderTime;
    }

    /// <summary>
    /// Obsluguje przejscie z fazy przesuwania do fazy walki
    /// </summary>
    IEnumerator ReorderingToFight()
    {
        currentPhase = GamePhase.PrepareFight;
        uiManager.HideTimer();
        uiManager.infoPanel.ShowPhase2End();
        yield return new WaitForSeconds(1.0f);
        uiManager.infoPanel.ShowFightPrepare();
        yield return new WaitForSeconds(1.0f);
        currentPhase = GamePhase.Fight;
        StartCoroutine(FightSequence());
    }

    /// <summary>
    /// Glowna sekwencja walki 
    /// 1. Iteruje przez 3 pary kart 
    /// 2. Wykonuje animacje
    /// 3. Oblicza obrazenia 
    /// 4. Aktywuje efekty wizualne
    /// </summary>
    IEnumerator FightSequence()
    {
        // Przejscie przez 3 rzedy
        for (int i = 0; i < 3; i++)
        {
            CardController playerCard = playerSlots[i].currentCard;
            CardController enemyCard = enemySlots[i].currentCard;

            if (playerCard == null || enemyCard == null) continue;

            Vector3 pStartPos = playerCard.transform.position;
            Vector3 eStartPos = enemyCard.transform.position;

            // Przesuniêcie kart na srodek
            float moveDuration = 0.4f;
            float time = 0;
            while (time < moveDuration)
            {
                playerCard.transform.position = Vector3.Lerp(pStartPos, playerFightPos.position, time / moveDuration);
                enemyCard.transform.position = Vector3.Lerp(eStartPos, enemyFightPos.position, time / moveDuration);
                time += Time.deltaTime;
                yield return null;
            }

            // Obliczenie obrazen na podstawie macierzy
            int pID = playerCard.gestureData.gestureID;
            int eID = enemyCard.gestureData.gestureID;
            int dmgToEnemy = adjacencyMatrix[pID, eID];
            int dmgToPlayer = adjacencyMatrix[eID, pID];

            yield return new WaitForSeconds(0.5f);

            // Niszczenie przegranych kart
            bool killPlayer = dmgToPlayer > 0;
            bool killEnemy = dmgToEnemy > 0;

            float dissolveDuration = 1.0f;
            bool waitingForDissolve = false;

            if (killPlayer) { StartCoroutine(playerCard.AnimDissolveShader(dissolveDuration, dissolveGlowColor)); playerSlots[i].currentCard = null; waitingForDissolve = true; }
            if (killEnemy) { StartCoroutine(enemyCard.AnimDissolveShader(dissolveDuration, dissolveGlowColor)); enemySlots[i].currentCard = null; waitingForDissolve = true; }

            if (waitingForDissolve) yield return new WaitForSeconds(dissolveDuration);

            // Wyswietlenie obrazen w UI
            if (dmgToPlayer > 0) uiManager.DamagePlayer(1, dmgToPlayer);
            if (dmgToEnemy > 0) uiManager.DamagePlayer(2, dmgToEnemy);

            // Powrot pozostalych kart na swoje miejsce
            time = 0;
            while (time < moveDuration)
            {
                if (!killPlayer && playerCard != null) playerCard.transform.position = Vector3.Lerp(playerFightPos.position, pStartPos, time / moveDuration);
                if (!killEnemy && enemyCard != null) enemyCard.transform.position = Vector3.Lerp(enemyFightPos.position, eStartPos, time / moveDuration);
                time += Time.deltaTime;
                yield return null;
            }
            if (!killPlayer && playerCard != null) playerCard.transform.position = pStartPos;
            if (!killEnemy && enemyCard != null) enemyCard.transform.position = eStartPos;

            yield return new WaitForSeconds(0.2f);
        }
        EndRound();
    }

    /// <summary>
    /// Podsumowanie rundy
    /// 1. Sprawdza HP 
    /// 2. Liczy karty przy remisie
    /// 3. Decyduje o przyznaniu punktu lub koñcu gry
    /// </summary>
    void EndRound()
    {
        currentPhase = GamePhase.Summary;

        int pHP = uiManager.GetPlayerHP(1);
        int eHP = uiManager.GetPlayerHP(2);

        //ID zwwyciezcy 0 - remis, 1 - gracz, 2 - AI
        int winner = 0;

        // Analiza HP
        if (pHP > eHP) winner = 1;
        else if (eHP > pHP) winner = 2;
        else
        {
            // Jesli rowne HP to analizuj liczba gestow na stole
            int pCards = CountActiveCards(playerSlots);
            int eCards = CountActiveCards(enemySlots);

            if (pCards > eCards) winner = 1;
            else if (eCards > pCards) winner = 2;
        }

        // Powtorka rundy przy remisie
        if (winner == 0)
        {
            uiManager.infoPanel.ShowRematch();
            Invoke(nameof(StartNewRound), 2.0f);
            return;
        }

        // Przyznanie punktu
        uiManager.AwardPointTo(winner);
        uiManager.infoPanel.ShowPointInfo(winner == 1 ? "you!" : "enemy...");

        // Sprawdzanie warunkow zakonczenia gry
        if (uiManager.IsGameFinished())
        {
            StartCoroutine(AnimateCameraZoom(camSizeZoomedOut));

            string msg = (pHP > eHP || (pHP == eHP && CountActiveCards(playerSlots) > CountActiveCards(enemySlots)))
                         ? "You win!" : "You lost...";
            uiManager.ShowGameOverScreen(msg);
        }
        else
        {
            // Nastepna runda
            Invoke(nameof(StartNewRound), 3.0f);
        }
    }

    /// <summary>
    /// Procedura pelnego resetu gry.
    /// </summary>
    public void ResetGame()
    {
        StartCoroutine(ResetSequence());
    }

    /// <summary>
    /// Sekwencja resetu
    /// 1. Reset UI
    /// 2. Wyczyszczenie stolu 
    /// 3. Przyblizenie kamery
    /// 4. Start rundy
    /// </summary>
    IEnumerator ResetSequence()
    {
        uiManager.ResetGameUI();
        yield return StartCoroutine(ClearTable());
        StartNewRound();
    }

    // ----- Pozostale funkcje ----- 

    /// <summary>
    /// Tworzy karte
    /// </summary>
    void SpawnCard(int id, CardSlot slot)
    {
        GestureData data = allGestures.Find(g => g.gestureID == id);
        if (data != null)
        {
            GameObject newCard = Instantiate(cardPrefab, slot.transform.position, Quaternion.identity);
            CardController cardCtrl = newCard.GetComponent<CardController>();
            cardCtrl.Initialize(data, slot.isPlayerSlot);
            slot.PlaceCard(cardCtrl);
        }
    }

    /// <summary>
    /// Symulacja tury AI
    /// </summary>
    IEnumerator EnemyTurn()
    {
        yield return new WaitForSeconds(0.5f);
        int randomGestureID = Random.Range(0, 7);
        ReceiveGesture(randomGestureID, false);
    }

    /// <summary>
    /// Zwraca indeks pierwszego wolnego miejsca w tablicy gestow
    /// </summary>
    int GetFirstEmptyIndex(int[] array) 
    { 
        for (int i = 0; i < array.Length; i++) 
            if (array[i] == -1) 
                return i; 
        return -1; 
    }
    
    /// <summary>
    /// Zlicza liczbe aktywnych kart w danej kolumnie.
    /// </summary>
    int CountActiveCards(List<CardSlot> slots) 
    { 
        int count = 0; foreach (var slot in slots) 
            if (slot.currentCard != null) 
                count++; 
        return count; 
    }

    /// <summary>
    /// Ustawienie kamery
    /// </summary>
    void SetCameraZoom(float size)
    {
        if (mainCamera != null)
            mainCamera.orthographicSize = size;
    }

    /// <summary>
    /// Animacja przyblizenia / oddalenia kamery.
    /// </summary>
    IEnumerator AnimateCameraZoom(float targetSize)
    {
        if (mainCamera == null) yield break;

        float time = 0;
        float startSize = mainCamera.orthographicSize;

        while (time < zoomDuration)
        {
            float t = Mathf.SmoothStep(0, 1, time / zoomDuration);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);

            time += Time.deltaTime;
            yield return null;
        }

        mainCamera.orthographicSize = targetSize;
    }
}