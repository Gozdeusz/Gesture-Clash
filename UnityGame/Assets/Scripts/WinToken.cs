using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Obsluguje obiekt pucharu wraz z animacjami
/// </summary>
public class WinToken : MonoBehaviour
{
    [Header("References and Sprites")]
    public Image rewardObject;
    public ParticleSystem rewardLightParticle;
    public Sprite rewardEmptySprite;
    public Sprite rewardBlueSprite;
    public Sprite rewardRedSprite;

    [Header("Animation Settings")]
    public Vector2 targetPosition;
    public float moveSpeed = 2.0f;

    private RectTransform rectTransform;
    private Vector2 startPosition;
    private Coroutine currentAnim;

    /// <summary>
    /// Ustawienie referencji i pozycji startowej
    /// </summary>
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        startPosition = rectTransform.anchoredPosition;
        ResetToken();
    }

    /// <summary>
    /// Ustawia wyglad tokenu dla niebieskiego gracza.
    /// </summary>
    public void SetBlue()
    {
        ActivateWinInternal(rewardBlueSprite, true);
    }

    /// <summary>
    /// Ustawia wyglad tokenu dla czerwonego gracza.
    /// </summary>
    public void SetRed()
    {
        ActivateWinInternal(rewardRedSprite, false);
    }

    /// <summary>
    /// Przywraca token do stanu poczatkowego
    /// </summary>
    public void ResetToken()
    {
        rewardObject.sprite = rewardEmptySprite;

        if (rewardLightParticle != null) 
            rewardLightParticle.Stop();

        if (currentAnim != null) 
            StopCoroutine(currentAnim);

        rectTransform.anchoredPosition = startPosition;
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Animacja przejscia na srodek ekranu
    /// </summary>
    IEnumerator MoveTo(Vector2 target, float targetScale)
    {
        float time = 0;
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.one * targetScale;

        while (time < 1)
        {
            time += Time.unscaledDeltaTime * moveSpeed;

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, target, Mathf.SmoothStep(0, 1, time));
            transform.localScale = Vector3.Lerp(startScale, endScale, Mathf.SmoothStep(0, 1, time));

            yield return null;
        }

        rectTransform.anchoredPosition = target;
        transform.localScale = endScale;
    }

    /// <summary>
    /// Logika sekwencji wygranej
    /// 1. Podmienia sprite
    /// 2. Uruchamia VFX 
    /// 3. Wlacza animacje
    /// </summary>
    /// <param name="spriteToSet">Sprite pucharu</param>
    /// <param name="blue">Steruje aktywacja VFX</param>
    private void ActivateWinInternal(Sprite spriteToSet, bool blue)
    {
        if (rewardObject.sprite == spriteToSet) 
            return;

        rewardObject.sprite = spriteToSet;

        if (rewardLightParticle != null && blue)
            rewardLightParticle.Play();

        if (currentAnim != null) 
            StopCoroutine(currentAnim);

        currentAnim = StartCoroutine(MoveTo(targetPosition, 1.5f));
    }
}