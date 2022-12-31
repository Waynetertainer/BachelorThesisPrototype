using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NetMQ;
using NetMQ.Sockets;
using System;
using MessagePack;
using System.Linq;
using UnityEngine.UI;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using UnityEngine.Events;

public class PythonInterface : MonoBehaviour
{
    public List<Vector3> handPoints = new List<Vector3>();
    public delegate void OnFrameReceived();
    public static event OnFrameReceived eOnFrameReceived;

    private RawImage m_RawImage;
    private SubscriberSocket sub;
    private PublisherSocket pub;
    private Texture2D t2;
    private List<byte[]> frames = new List<byte[]>();
    private float timeSinceLastMsg;
    private int counter = 0;

    // Start is called before the first frame update
    void Start()
    {
        t2 = new Texture2D(640, 360, TextureFormat.RGB24, false);
        m_RawImage = GameManager.Instance.VideoTarget;
        timeSinceLastMsg = Time.time;

        AsyncIO.ForceDotNet.Force();

        pub = new PublisherSocket();
        pub.Bind("tcp://127.0.0.1:30000");
        StartPythonProcess();

        sub = new SubscriberSocket();
        sub.Connect("tcp://127.0.0.1:30001");
        sub.Subscribe("faces");
    }

    // Update is called once per frame
    void Update()
    {
        // Check if something is received
        if (sub.TryReceiveMultipartBytes(ref frames, 2))
        {
            if(eOnFrameReceived != null) eOnFrameReceived();
            Debug.Log("gotMessage");
            timeSinceLastMsg = Time.time;
            object[] points = MessagePackSerializer.Deserialize<Dictionary<dynamic, dynamic>>(frames[1])["points"];
            var vPoints = points.Select(p => ((object[])p).Select(pp => (float)((double)pp * 10)).ToArray()).Select(p => new Vector3(p[0], p[1], p[2])).ToList();
            handPoints = vPoints;

            byte[] picbyte = MessagePackSerializer.Deserialize<Dictionary<dynamic, dynamic>>(frames[1])["picture"];
            string message = MessagePackSerializer.Deserialize<Dictionary<dynamic, dynamic>>(frames[1])["message"];
            Debug.Log("msg:" + message);
            // Restart the Python script after it has stopped itself
            if (message.Contains("stoppedTime"))
            {
                StartPythonProcess();
                Debug.Log("restarting after stopped");
            }
            t2.LoadRawTextureData(picbyte);
            t2.Apply();
            m_RawImage.texture = t2;
        }
        else
        {
            Debug.Log("no message");
        }

        // Restart the Python script after not receiving a message for a minute
        if (Time.time > timeSinceLastMsg + 60)
        {
            timeSinceLastMsg = Time.time;
            Debug.Log("message missing, restarting");
            StartPythonProcess();
        }

        // Send a ping to ensure the Python script that the game is still running
        if (counter % 200 == 0)
        {
            pub.SendMoreFrame("status").SendFrame("ping");
        }
        counter++;
    }

    public void SendRequestStop()
    {
        pub.SendMoreFrame("status").SendFrame("stop");
    }


    private static void StartPythonProcess()
    {
        Debug.Log("py process");
        var psi = new ProcessStartInfo();
        psi.FileName = Application.streamingAssetsPath + @"/dist/Annotator.exe";
        psi.UseShellExecute = false;
        psi.CreateNoWindow = true;
        psi.RedirectStandardOutput = false;
        psi.RedirectStandardError = false;

        Process.Start(psi);
    }

    private void OnApplicationQuit()
    {
        Debug.Log("quit");
        SendRequestStop();
        sub.Disconnect("tcp://127.0.0.1:30001");
        sub.Dispose();
        pub.Unbind("tcp://127.0.0.1:30000");
        pub.Dispose();
        NetMQConfig.Cleanup();
    }
}
