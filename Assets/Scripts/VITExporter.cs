#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;
using UnityEngine.UI;

public class VITExporter : MonoBehaviour
{
    private bool startedRecording;
    private DateTime recordStartTime;
    private Attributes attributes;
    private RecorderWindow recorderWindow;

    private void OnEnable()
    {
        PythonInterface.eOnFrameReceived += CreateFrame;
    }

    private void OnDisable()
    {
        PythonInterface.eOnFrameReceived -= CreateFrame;
    }


    // Start is called before the first frame update
    void Start()
    {
        GetWindow();
        attributes = new Attributes();
        attributes.frames = new List<Attribute>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown("space"))
        {
            Debug.Log("Stop Recording");
            ExportJson();
            StopRecording();
        }
        // Only as fallback, should not be necessary
        if (Input.GetKeyDown("w"))
        {
            Debug.Log("Start Recording");
            if (!recorderWindow.IsRecording())
            {
                recorderWindow.StartRecording();
            }
        }
    }

    public void CreateFrame()
    {
        if (!startedRecording)
        {
            startedRecording = true;
            StartRecording();
        }
        TimeSpan time = DateTime.Now - recordStartTime;
        string timeFormatted = string.Format("{0:hh}:{0:mm}:{0:ss}.{0:ffff}", time);
        attributes.frames.Add(GameManager.Instance.Recognizer.GetAttribute(timeFormatted));
    }

    public void StartRecording()
    {
        if (recorderWindow && !recorderWindow.IsRecording())
        {
            recorderWindow.StartRecording();
        }
        recordStartTime = DateTime.Now;
    }

    public void StopRecording()
    {
        if (recorderWindow && recorderWindow.IsRecording())
        {
            recorderWindow.StopRecording();
        }
    }

    public void ExportJson()
    {
        using (StreamWriter sw = new StreamWriter(Application.streamingAssetsPath + @"/Recordings/attributes.json"))
        {
            sw.Write(JsonConvert.SerializeObject(attributes, Formatting.Indented));
        }
    }

    private void GetWindow()
    {
        recorderWindow = (RecorderWindow)EditorWindow.GetWindow(typeof(RecorderWindow));
    }
}

#endif