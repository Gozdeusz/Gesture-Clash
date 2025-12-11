using System.Collections;
using UnityEngine;

/// <summary>
/// Odpowiada za logikê pojedynczej karty w grze
/// Obs³uguje przypisanie danych, wyswietlanie, Drag & Drop 
/// oraz animacje pojawiania siê i znikania
/// </summary>
public class CardController : MonoBehaviour
{
    [Header("Animation Settings")]
    public float targetScale = 0.3f;
    public float spawnAnimationDuration = 0.4f;

    public GestureData gestureData;
    public CardSlot currentSlot;

    // Okreœla czy gracz mo¿e chwyciæ tê kartê
    private bool isDraggable = false;
    private bool isDragging = false;
    private SpriteRenderer spriteRenderer;

    // Nazwy zmiennych kontrolujacych shader
    private readonly string dissolveAmountProp = "_DissolveAmount";
    private readonly string dissolveColorProp = "_DissolveColor";

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        // Przygotowanie karty do animacji pop-up
        transform.localScale = Vector3.zero;
        StartCoroutine(AnimSpawn());
    }

    void Update()
    {
        // Jesli gracz trzyma karte ale skonczyl sie czas
        // wymuszone zostaje upuszczenie karty
        if (isDragging && GameManager.Instance.currentPhase != GamePhase.Reordering)
            ForceRelease();
        
    }

    /// <summary>
    /// Konfiguracja karty
    /// </summary>
    public void Initialize(GestureData gestureData, bool isInteractive)
    {
        this.gestureData = gestureData;
        spriteRenderer.sprite = this.gestureData.cardSprite;
        isDraggable = isInteractive;

        // Reset shadera
        spriteRenderer.material.SetFloat(dissolveAmountProp, 1f);
    }

    /// <summary>
    /// Wykonuje animacje pop-up
    /// </summary>
    IEnumerator AnimSpawn()
    {
        float time = 0;
        Vector3 startScale = Vector3.zero;

        Vector3 endScale = new (targetScale, targetScale, targetScale);

        while (time < spawnAnimationDuration)
        {
            time += Time.deltaTime;

            float t = Mathf.SmoothStep(0, 1, time / spawnAnimationDuration);
            transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        transform.localScale = endScale;
    }
      
    /// <summary>
    /// Uruchomienie animacji shadera dissolve
    /// Po zakonczeniu animacji niszczy obiekt karty
    /// </summary>
    public IEnumerator AnimDissolveShader(float duration, Color glowColor)
    {
        float time = 0;
        Material mat = spriteRenderer.material;

        mat.SetColor(dissolveColorProp, glowColor);

        float startVal = 1f;
        float endVal = -0.1f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float value = Mathf.Lerp(startVal, endVal, time / duration);
            mat.SetFloat(dissolveAmountProp, value);
            yield return null;
        }

        mat.SetFloat(dissolveAmountProp, endVal);
        Destroy(gameObject);
    }

    // ----- Drag and Drop -----

    // Chwycenie karty
    void OnMouseDown()
    {
        if (GameManager.Instance.currentPhase == GamePhase.Reordering && isDraggable)
        {
            isDragging = true;
            spriteRenderer.sortingOrder = 10;
        }
    }

    // Zmiana pozycji karty
    void OnMouseDrag()
    {
        if (GameManager.Instance.currentPhase != GamePhase.Reordering)
            return;

        if (isDragging)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            transform.position = new Vector3(mousePos.x, mousePos.y, 0);
        }
    }

    // Puszczenie karty
    void OnMouseUp()
    {
        if (isDragging)
        {
            isDragging = false;
            spriteRenderer.sortingOrder = 0;
            CheckDrop();
        }
    }

    /// <summary>
    /// Przerywa drag karty i resetuje ja do pozycji slotu
    /// Uzywana przy koncu czasu lub bladnym upuszczeniu
    /// </summary>
    public void ForceRelease()
    {
        isDragging = false;
        spriteRenderer.sortingOrder = 0;

        if (currentSlot != null)
            transform.position = currentSlot.transform.position;
    }

    /// <summary>
    /// Logika upuszczania karty 
    /// Sprawdza najblizszy slot i zamienia pozycje kart
    /// </summary>
    void CheckDrop()
    {
        if (GameManager.Instance.currentPhase != GamePhase.Reordering)
        {
            ForceRelease();
            return;
        }

        CardSlot closestSlot = null;
        float minDistance = float.MaxValue;

        // Szukanie najblizszego slotu
        foreach (CardSlot slot in GameManager.Instance.playerSlots)
        {
            float dist = Vector3.Distance(transform.position, slot.transform.position);
            if (dist < 1.5f)
            {
                if (dist < minDistance) { minDistance = dist; closestSlot = slot; }
            }
        }

        // Zamiana miejsc
        if (closestSlot != null && closestSlot != currentSlot)
        {
            CardController targetCard = closestSlot.currentCard; // Karta do zamiany
            CardSlot oldSlot = currentSlot;

            // Przeniesienie karty do nowego slotu
            closestSlot.PlaceCard(this);

            // Zamiana kart
            if (targetCard != null) 
                oldSlot.PlaceCard(targetCard);
            else 
                oldSlot.currentCard = null;
        }
        else
        {
            // Powrot na oryginalne miejsce
            transform.position = currentSlot.transform.position;
        }
    }
}