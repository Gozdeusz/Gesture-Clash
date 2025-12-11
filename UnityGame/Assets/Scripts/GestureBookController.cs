using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Kontroler Ksiegi Gestow
/// Odpowiada za wyswietlenie grafiki z grafem gestow oraz nawigacje po niej
/// Przesuwanie za pomoca klawiatury/krawêdzi ekranu i przybli¿anie scrollem
/// </summary>
public class GestureBookController : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform contentImage;
    public GameObject bookPanel;

    [Header("Settings")]
    public float cameraSpeed = 500f;
    public float zoomSpeed = 0.5f;
    public float edgeBorder = 50f;
    public float minZoom = 0.5f; 
    public float maxZoom = 3.0f;

    private bool isOpen = false;

    /// <summary>
    /// Obsluga sterowania jesli prefab jest aktywny
    /// </summary>
    void Update()
    {
        if (!isOpen) 
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
            CloseBook();

        HandlePan();
        HandleZoom();
    }

    /// <summary>
    /// Otwiera ksiege, resetuje konfiguracje nawigacji
    /// </summary>
    public void OpenBook()
    {
        bookPanel.SetActive(true);
        isOpen = true;

        contentImage.anchoredPosition = Vector2.zero;
        contentImage.localScale = Vector3.one;
    }

    /// <summary>
    /// Zamyka ksiege
    /// </summary>
    public void CloseBook()
    {
        bookPanel.SetActive(false);
        isOpen = false;
    }

    /// <summary>
    /// Obsluguje przesuwanie obrazka
    /// Dziala na WSAD/Strzalki oraz po najechaniu myszk¹ na krawedz ekranu
    /// </summary>
    void HandlePan()
    {
        Vector3 pos = contentImage.anchoredPosition;
        float dt = Time.unscaledDeltaTime;

        // WSAD / Strzalki
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Lrawedzie ekranu
        if (Input.mousePosition.x > Screen.width - edgeBorder) h = 1;
        if (Input.mousePosition.x < edgeBorder) h = -1;
        if (Input.mousePosition.y > Screen.height - edgeBorder) v = 1;
        if (Input.mousePosition.y < edgeBorder) v = -1;

        pos.x -= h * cameraSpeed * dt;
        pos.y -= v * cameraSpeed * dt;

        contentImage.anchoredPosition = pos;
    }

    /// <summary>
    /// Obsluguje przyblizanie i oddalanie obrazka
    /// </summary>
    void HandleZoom()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll != 0)
        {
            Vector3 scale = contentImage.localScale;
            scale += Vector3.one * (scroll * zoomSpeed);

            scale.x = Mathf.Clamp(scale.x, minZoom, maxZoom);
            scale.y = Mathf.Clamp(scale.y, minZoom, maxZoom);
            scale.z = 1;

            contentImage.localScale = scale;
        }
    }
}