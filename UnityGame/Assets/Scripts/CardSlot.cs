using UnityEngine;

/// <summary>
/// Klasa reprezentuj¹ca pojedynczy slot na stole gry
/// Slot - punkt do ktorego naleza karty oraz
/// przechowuje informacje o tym 
/// ktora karta znajduje sie w danym rzedzie
/// </summary>
public class CardSlot : MonoBehaviour
{
    //Identyfikator wiersza - sluzy do parowania kart podczas fazy walki
    public int rowID;
    // Okresa przynaleznosc slotu do gracza, wa¿ne dla mechaniki drag and drop
    public bool isPlayerSlot;
    [HideInInspector]
    // Referencja do karty ktora aktualnie znajduje siê w tym slocie
    public CardController currentCard;

    /// <summary>
    /// Przypisuje karte do slotu
    /// </summary>
    public void PlaceCard(CardController card)
    {
        currentCard = card;
        card.transform.position = this.transform.position;
        card.currentSlot = this;
    }
}
