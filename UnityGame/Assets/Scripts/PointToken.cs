using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Obsluguje obiekt punktu gracza
/// </summary>
public class PointToken : MonoBehaviour
{
    [Header("References")]
    public Image tokenImage; 
    public ParticleSystem hitEffect;

    [Header("Settings")]
    public Sprite emptySprite;

    /// <summary>
    /// Resetuje token
    /// </summary>
    void Awake()
    {
        ResetToken();
    }

    /// <summary>
    /// Aktywuje token - ustawia obrazek i wlacza VFX
    /// </summary>
    public void SetActive(Sprite activeSprite)
    {
        if (tokenImage.sprite == activeSprite) 
            return;
        tokenImage.sprite = activeSprite;

        if (hitEffect != null)
            hitEffect.Play();
    }

    /// <summary>
    /// Resetuje obiekt
    /// </summary>
    public void ResetToken()
    {
        tokenImage.sprite = emptySprite;
        if (hitEffect != null) 
            hitEffect.Stop();
    }
}