import cv2
import mediapipe as mp
import pickle
import socket
import time
from collections import deque, Counter

# Ustawienia
SHOW_CAMERA_WINDOW = False
MODEL_PATH = "model_gesty.pkl"
UNITY_IP = "127.0.0.1"
UNITY_PORT = 5005
BUFFER_SIZE = 15
SEND_COOLDOWN = 3.0

# Wczytanie modelu
try:
    with open(MODEL_PATH, "rb") as f:
        model_data = pickle.load(f)
        svm = model_data['svm']
except FileNotFoundError:
    print(f"Nie znaleziono pliku '{MODEL_PATH}'!")
    exit()

# Laczenie z Unity
sock = None
connected = False
try:
    sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    sock.connect((UNITY_IP, UNITY_PORT))
    connected = True
except Exception as e:
    print(f"Blad polaczenia z Unity: {e}")

# Konfiguracja MediaPipe
mp_hands = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils
mp_drawing_styles = mp.solutions.drawing_styles

hands = mp_hands.Hands(
    static_image_mode=False,
    max_num_hands=1,
    min_detection_confidence=0.7,
    min_tracking_confidence=0.5
)

prediction_buffer = deque(maxlen=BUFFER_SIZE)
last_send_time = 0

# Glowna petla
cap = cv2.VideoCapture(0)

if not cap.isOpened():
    print("Problem z otwarciem kamery")
    exit()

try:
    while True:
        ret, frame = cap.read()
        if not ret:
            break

        frame = cv2.flip(frame, 1)
        frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

        current_time = time.time()
        time_diff = current_time - last_send_time
        is_cooldown = time_diff < SEND_COOLDOWN

        raw_gesture = None

        results = hands.process(frame_rgb)

        if results.multi_hand_landmarks:
            for hand_landmarks in results.multi_hand_landmarks:
                if SHOW_CAMERA_WINDOW:
                    mp_drawing.draw_landmarks(
                        frame,
                        hand_landmarks,
                        mp_hands.HAND_CONNECTIONS,
                        mp_drawing_styles.get_default_hand_landmarks_style(),
                        mp_drawing_styles.get_default_hand_connections_style())

                if not is_cooldown:
                    landmarks = []
                    for lm in hand_landmarks.landmark:
                        landmarks.extend([lm.x, lm.y])

                    try:
                        prediction = svm.predict([landmarks])
                        raw_gesture = prediction[0]
                    except Exception:
                        pass

        stable_gesture = None
        if not is_cooldown:
            if raw_gesture:
                prediction_buffer.append(raw_gesture)

                if len(prediction_buffer) == BUFFER_SIZE:
                    counts = Counter(prediction_buffer)
                    most_common, count = counts.most_common(1)[0]

                    if count > (BUFFER_SIZE * 0.7):     # Procent pewnosci co do rozpoznania gestu
                        stable_gesture = most_common
            else:
                prediction_buffer.clear()

            # Wysylanie do Unity
            if stable_gesture:
                if connected:
                    try:
                        sock.sendall(stable_gesture.encode("utf-8"))
                        last_send_time = time.time()
                        prediction_buffer.clear()
                    except Exception:
                        print("\nZerwano połączenie.")
                        connected = False
                        sock.close()

        if SHOW_CAMERA_WINDOW:
            if stable_gesture:
                cv2.putText(frame, f"Gest: {stable_gesture}", (10, 50),
                            cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 0), 2)

            cv2.imshow("Gesty - Kamera", frame)

            if cv2.waitKey(1) & 0xFF == ord('q'):
                break

except KeyboardInterrupt:
    print("\nZamknieto program")

finally:
    cap.release()
    cv2.destroyAllWindows()
    if connected:
        sock.close()