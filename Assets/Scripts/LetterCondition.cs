using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using Newtonsoft.Json;

[System.Serializable]
public class LetterCondition
{
    public char Letter;
    public FingerPose Index;
    public FingerPose Middle;
    public FingerPose Ring;
    public FingerPose Little;
    public float Straightness;
    public float Rotation;
    public float Spread;
    public List<int> Touches;
    public bool MustTouch;

    public LetterCondition(char inLetter, FingerPose inIndex, FingerPose inMiddle, FingerPose inRing, FingerPose inLittle, List<int> inTouches, float inStraightness = 0, float inRotation = 0, float inSpread = 0, bool inMustTouch = true, bool inFingersMustBeCrossed = false)
    {
        //values that are 0 will be not be evaluated
        Index = inIndex;
        Middle = inMiddle;
        Ring = inRing;
        Little = inLittle;
        Touches = inTouches;
        Straightness = inStraightness;
        Rotation = inRotation;
        Spread = inSpread;
        MustTouch = inMustTouch;
    }

    public LetterCondition(LetterConditionStrings lcs)
    {
        Letter = lcs.annotations["Letter"][0];
        Index = (FingerPose)int.Parse(lcs.annotations["Index"]);
        Middle = (FingerPose)int.Parse(lcs.annotations["Middle"]);
        Ring = (FingerPose)int.Parse(lcs.annotations["Ring"]);
        Little = (FingerPose)int.Parse(lcs.annotations["Little"]);
        Straightness = float.Parse(lcs.annotations["Straightness"]);
        Rotation = float.Parse(lcs.annotations["Rotation"]);
        Spread = float.Parse(lcs.annotations["Spread"]);
        Touches = lcs.annotations["Touches"].Split(',').Select(x => int.Parse(x)).ToList();
        MustTouch = lcs.annotations["MustTouch"] == "1" ? true : false;
    }
}

public struct LetterConditionCollection
{
    public string applicationName;
    public string recordingsID;
    public List<LetterConditionStrings> intervals;
}

public struct LetterConditionStrings
{
    public string start;
    public string end;
    public Dictionary<string, string> annotations;
}
