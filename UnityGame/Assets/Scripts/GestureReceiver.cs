using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// Menedzer sieciowy
/// Odpowiada za odebranie danych z zewnetrznego skryptu Python
/// i bezpieczne przekazanie ich do w¹tku glownego
/// i utrzymuje polaczenie miedzy scenami
/// </summary>
public class PythonReceiver : MonoBehaviour
{
    public static PythonReceiver Instance { get; private set; }

    [Header("Config")]
    public int port = 5005;

    // Kolejka do buferowania wiadomosci 
    private readonly ConcurrentQueue<string> messageQueue = new();

    private TcpListener listener;
    private Thread listenThread;
    private TcpClient connectedClient;

    // Flaga do kontrolowania przez GameManagera polaczenia
    public bool IsConnected { get; private set; } = false;

    // Flaga do wykrycia zmiany stanu 
    private bool wasConnectedLastFrame = false;

    /// <summary>
    /// Tworzy singleton i zapewnia ¿e nie jest niszczony przy zmianie sceny
    /// </summary>
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// Uruchamia watek sieciowy
    /// </summary>
    void Start()
    {
        listenThread = new Thread(ListenForClient)
        {
            IsBackground = true
        };
        listenThread.Start();
    }

    /// <summary>
    /// Monitoruje stan polaczenia i przetwarza komunikaty z kolejki
    /// </summary>
    void Update()
    {
        if (Instance != this)
            return;

        // Wykrywanie momentu polaczenia
        if (IsConnected && !wasConnectedLastFrame)
        {
            wasConnectedLastFrame = true;
            if (GameManager.Instance != null)
                GameManager.Instance.OnPythonConnected();
        }

        // Wykrywanie rozlaczenia
        if (!IsConnected && wasConnectedLastFrame)
        {
            wasConnectedLastFrame = false;
            Debug.LogWarning("Utracono po³¹czenie z Pythonem");
            if (GameManager.Instance != null)
                GameManager.Instance.OnPythonDisconnected();
        }

        // Przetwarzanie wiadomosci
        while (messageQueue.TryDequeue(out string message))
        {
            ProcessMessage(message);
        }
    }

    /// <summary>
    /// Glowna petla watku sieciowego
    /// 1. Otwiera port
    /// 2. Czeka na klienta
    /// 3. Nas³uchuje przychodz¹ce dane
    /// </summary>
    void ListenForClient()
    {
        while (true)
        {
            try
            {
                if (listener == null)
                {
                    listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
                    listener.Start();
                }

                connectedClient = listener.AcceptTcpClient();
                IsConnected = true;

                NetworkStream stream = connectedClient.GetStream();
                byte[] buffer = new byte[1024];

                // Odczyt danych
                while (true)
                {
                    int bytes = stream.Read(buffer, 0, buffer.Length);
                    if (bytes <= 0) break;

                    string msg = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                    messageQueue.Enqueue(msg);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("B³¹d sieci: " + e.Message);
                Thread.Sleep(1000);
            }
            finally
            {
                IsConnected = false;
                if (connectedClient != null) connectedClient.Close();
            }
        }
    }

    /// <summary>
    /// Interpretuje odebrana wiadomosc tekstowa i przekazuje ja do GameManagera.
    /// </summary>
    void ProcessMessage(string msg)
    {
        if (GameManager.Instance != null)
        {
            int gestureID = MapGestureToID(msg);
            if (gestureID != -1)
            {
                GameManager.Instance.ReceiveGesture(gestureID, true);
            }
        }
    }

    /// <summary>
    /// Tlumaczy nazwe gestu na identyfikator ID
    /// </summary>
    int MapGestureToID(string gestureName) => gestureName.ToLower() switch
    {
        "rock" => 0,
        "sun" => 1,
        "snake" => 2,
        "gun" => 3,
        "scissors" => 4,
        "devil" => 5,
        "paper" => 6,
        _ => -1
    };

    /// <summary>
    /// Bezpiecznie zamyka watki i gniazda sieciowe
    /// </summary>
    private void OnApplicationQuit()
    {
        if (listener != null) 
            listener.Stop();
        if (listenThread != null) 
            listenThread.Abort();
        if (connectedClient != null) 
            connectedClient.Close();
    }
}