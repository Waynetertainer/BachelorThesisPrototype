import zmq, msgpack
import sys, cv2, io, numpy as np
from PIL import Image
import mediapipe as mp
import time

mp_drawing = mp.solutions.drawing_utils
mp_drawing_styles = mp.solutions.drawing_styles
mp_hands = mp.solutions.hands

input = zmq.Context().socket(zmq.SUB)
input.setsockopt_string(zmq.SUBSCRIBE, u"frames")
input.setsockopt_string(zmq.SUBSCRIBE, u"start")
input.setsockopt_string(zmq.SUBSCRIBE, u"status")
#input.setsockopt(zmq.CONFLATE, 1)
input.connect("tcp://127.0.0.1:30000")

output = zmq.Context().socket(zmq.PUB)
output.bind("tcp://127.0.0.1:30001")

print("connected")

cam = cv2.VideoCapture(0)
t = time.time()
status = "normal"


def sendFrame(points, frame):
    global t
    payload = {}
    payload[u"points"] = points
    payload[u"picture"] = frame
    payload[u"message"] = status+" "+str((time.time() - t)>10)
    output.send_multipart(["faces".encode(), msgpack.dumps(payload)])


with mp_hands.Hands(
        model_complexity=0,
        min_detection_confidence=0.5,
        min_tracking_confidence=0.5) as hands:
    while True:
        s, frame = cam.read()
        frame = cv2.flip(frame, 0)
        frame = cv2.flip(frame, 1)
        if frame.size == 0:
            print("Ignoring empty camera frame.")
            continue
        frame.flags.writeable = False
        frame = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
        pil_image = Image.fromarray(frame)
        results = hands.process(frame)

        frame.flags.writeable = True
        points = []
        if results.multi_hand_landmarks:
            for hand_landmarks in results.multi_hand_landmarks:
                mp_drawing.draw_landmarks(
                    frame,
                    hand_landmarks,
                    mp_hands.HAND_CONNECTIONS,
                    mp_drawing_styles.get_default_hand_landmarks_style(),
                    mp_drawing_styles.get_default_hand_connections_style())

            hand_landmarks = results.multi_hand_landmarks[0]
            image_height, image_width, _ = frame.shape
            #points = [(lm.x * image_width, lm.y * image_height, lm.z) for lm in hand_landmarks.landmark]
            points = [(lm.x, lm.y, lm.z) for lm in hand_landmarks.landmark]

        im_resize = cv2.resize(frame, (640, 360))
        byte_im = bytes(Image.fromarray(im_resize.reshape((640, 360, 3))).tobytes())
        sendFrame(points, byte_im)

        try:
            message = input.recv_string(flags=zmq.NOBLOCK)
            t = time.time()
            status = "msg"
            print("Message received:", message)
            if message == "stop":
                status = "stoppedStop"
                sendFrame(points, byte_im)
                input.close()
                output.close()
                break

        except:
            print("No message received yet")
            if (time.time() - t) > 30:
                status = "stoppedTime"
                sendFrame(points, byte_im)
                print("timeout")
                input.close()
                output.close()
                break