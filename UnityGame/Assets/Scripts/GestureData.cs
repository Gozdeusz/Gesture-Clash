using UnityEngine;

/// <summary>
/// Klasa reprezentuje gest
/// </summary>
[CreateAssetMenu(fileName = "Gesture", menuName = "Scriptable Objects/Gesture")]
public class GestureData : ScriptableObject
{
    public string gestureName; // Nazwa gestu
    public int gestureID;      // ID gestu do indentyfikacji na grafie
    public Sprite cardSprite; // Sprite gestu
}
