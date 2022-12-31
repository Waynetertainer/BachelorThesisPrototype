
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;


[System.Serializable]
public class Attributes
{
    public string RecordingID = "test1";
    public string applicationName = "CalculatedValues";
    public List<Attribute> frames;
}

[System.Serializable]
public struct Attribute
{
    public string frameStamp;
    [SerializeField]
    public Dictionary<string, string> frameAttributes;

    public Attribute(
        string timeStamp,
        FingerPose index,
        FingerPose middle,
        FingerPose ring,
        FingerPose little,
        float straighness,
        float rotation,
        float spread,
        bool touches6,
        bool touches7,
        bool touches8,
        bool touches9,
        bool touches11,
        bool touches12,
        bool touches13,
        bool touches15,
        bool touches19
        )
    {
        frameStamp = timeStamp;
        frameAttributes = new Dictionary<string, string>();
        frameAttributes.Add("index", ((int)index).ToString());
        frameAttributes.Add("middle", ((int)middle).ToString());
        frameAttributes.Add("ring", ((int)ring).ToString());
        frameAttributes.Add("little", ((int)little).ToString());
        frameAttributes.Add("ThumpStraightness", straighness.ToString().Replace(',','.'));
        frameAttributes.Add("ThumpRotation", rotation.ToString().Replace(',', '.'));
        frameAttributes.Add("ThumpSpread", spread.ToString().Replace(',', '.'));
        frameAttributes.Add("Touches6", (touches6 ? 1 : 0).ToString());
        frameAttributes.Add("Touches7", (touches7 ? 1 : 0).ToString());
        frameAttributes.Add("Touches8", (touches8 ? 1 : 0).ToString());
        frameAttributes.Add("Touches9", (touches9 ? 1 : 0).ToString());
        frameAttributes.Add("Touches11", (touches11 ? 1 : 0).ToString());
        frameAttributes.Add("Touches12", (touches12 ? 1 : 0).ToString());
        frameAttributes.Add("Touches13", (touches13 ? 1 : 0).ToString());
        frameAttributes.Add("Touches15", (touches15 ? 1 : 0).ToString());
        frameAttributes.Add("Touches19", (touches19 ? 1 : 0).ToString());
    }
}