using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class FingerRecognizer : MonoBehaviour
{
    public List<Vector3> Joints { get { return joints; } }

    private int fingerstate;
    private float lastFingerChange;
    private AvgFinger avgIndex;
    private AvgFinger avgMiddle;
    private AvgFinger avgRing;
    private AvgFinger avgLittle;
    private AvgNum<float> avgThumpSpread;
    private AvgNum<float> avgThumpRotation;
    private AvgNum<float> avgThumpStraightness;
    private int[] touchNumbers = new int[] { 6, 7, 8, 9, 11, 12, 13, 15, 19 };
    private bool[] touchesList = new bool[9];
    private List<Vector3> joints = new List<Vector3>();
    private Dictionary<int, AvgBool> touches;


    // Start is called before the first frame update
    void Start()
    {
        touches = new Dictionary<int, AvgBool>();
        foreach (int num in touchNumbers)
        {
            touches.Add(num, new AvgBool());
        }
    }

    // Update is called once per frame
    void Update()
    {
        joints = GameManager.Instance.PyInterface.handPoints;
        if (joints.Count == 0)
        {
            fingerstate = 0;
            return;
        }
        setFingerPoses();
        for (int i = 0; i < 9; i++)
        {
            touchesList[i] = touches[touchNumbers[i]].Average;
        }

        if (StraightFingersCount() != fingerstate)
        {
            fingerstate = StraightFingersCount();
            lastFingerChange = Time.realtimeSinceStartup;
        }

    }

    // Calculates how many fingers are currently raised according to the navigation gestures
    private int StraightFingersCount()
    {
        if (avgIndex.Average == FingerPose.Straight && avgMiddle.Average != FingerPose.Straight && avgRing.Average != FingerPose.Straight && avgLittle.Average == FingerPose.Straight) return 5;
        if (avgIndex.Average == FingerPose.Straight)
        {
            if (avgMiddle.Average == FingerPose.Straight)
            {
                if (avgRing.Average == FingerPose.Straight)
                {
                    if (avgLittle.Average == FingerPose.Straight)
                    {
                        return 4;
                    }
                    return 3;
                }
                return 2;
            }
            return 1;
        }
        return 0;
    }

    // Returns how many fingers are raised according to the navigation gestures
    // This includes a short delay which requires the user to hold a gesture for a second for it to be recognized
    public int StraightFingers()
    {
        if (lastFingerChange + 1 < Time.realtimeSinceStartup)
        {
            return fingerstate;
        }
        return 0;
    }

    public void ResetLastFingerChange()
    {
        lastFingerChange = Time.realtimeSinceStartup;
    }

    // Checks whether the positioning of the hand matches the requirement of a given letter
    public bool LetterRecognized(LetterCondition lc)
    {
        if (joints.Count == 0) return false;
        if (lc.Index != avgIndex.Average) return false;
        if (lc.Middle != avgMiddle.Average) return false;
        if (lc.Ring != avgRing.Average) return false;
        if (lc.Little != avgLittle.Average) return false;
        if (lc.Touches[0] != 0 && lc.MustTouch != ThumpTouches(lc.Touches)) return false;
        if (lc.Straightness != 0 && !IsInRange(avgThumpStraightness.Average, lc.Straightness)) return false;
        if (lc.Rotation != 0 && !IsInRange(avgThumpRotation.Average, lc.Rotation)) return false;
        if (lc.Spread != 0 && !IsInRange(avgThumpSpread.Average, lc.Spread)) return false;
        return true;
    }

    // Checks whether the positioning of the hand matches the requirement of a given letter and gives feedback on what to improve
    public bool LetterRecognized(LetterCondition lc, ref List<string> feedback)
    {
        if (joints.Count == 0)
        {
            feedback.Add("No hand detected");
            return false;
        }
        bool success = true;

        getFingerFeedback(lc.Index, avgIndex.Average, "index", ref feedback, ref success);
        getFingerFeedback(lc.Middle, avgMiddle.Average, "middle", ref feedback, ref success);
        getFingerFeedback(lc.Ring, avgRing.Average, "ring", ref feedback, ref success);
        getFingerFeedback(lc.Little, avgLittle.Average, "little", ref feedback, ref success);
        if (lc.Touches[0] != 0 && lc.MustTouch != ThumpTouches(lc.Touches))
        {
            success = false;
            StringBuilder sb = new StringBuilder();
            if (lc.MustTouch && !ThumpTouches(lc.Touches))
            {
                sb = new StringBuilder("Try moving your Thump closer to joint ");

            }
            if (!lc.MustTouch && ThumpTouches(lc.Touches))
            {
                sb = new StringBuilder("Try moving your Thump further away from joint ");
            }

            sb.Append(lc.Touches[0]);
            for (int i = 1; i < lc.Touches.Count - 1; i++)
            {
                sb.Append(", ");
                sb.Append(lc.Touches[i]);
            }
            if (lc.Touches.Count > 1)
            {
                sb.Append(" or ");
                sb.Append(lc.Touches.Last());
            }
            feedback.Add(sb.ToString());
        }
        if (lc.Straightness != 0 && !IsInRange(avgThumpStraightness.Average, lc.Straightness))
        {
            success = false;
            if (lc.Straightness > avgThumpStraightness.Average)
            {
                feedback.Add("Try holding your thump more angled");
            }
            else
            {
                feedback.Add("Try holding your thump more straight");
            }
        }
        if (lc.Rotation != 0 && !IsInRange(avgThumpRotation.Average, lc.Rotation))
        {
            success = false;
            if (lc.Rotation > avgThumpRotation.Average)
            {
                feedback.Add("Try holding your thump further away from your palm");
            }
            else
            {
                feedback.Add("Try holding your thump closer to your palm");
            }
        }
        if (lc.Spread != 0 && !IsInRange(avgThumpSpread.Average, lc.Spread))
        {
            success = false;
            if (lc.Spread > avgThumpSpread.Average)
            {
                feedback.Add("Try holding your thump less spreaded");
            }
            else
            {
                feedback.Add("Try holding your thump more spreaded");
            }
        }
        return success;
    }

    // Checks whether the thump is correctly positioned
    public bool ThumpCorrect(LetterCondition lc)
    {
        if (lc.Touches[0] != 0 && lc.MustTouch != ThumpTouches(lc.Touches)) return false;
        if (lc.Straightness != 0 && !IsInRange(avgThumpStraightness.Average, lc.Straightness)) return false;
        if (lc.Rotation != 0 && !IsInRange(avgThumpRotation.Average, lc.Rotation)) return false;
        if (lc.Spread != 0 && !IsInRange(avgThumpSpread.Average, lc.Spread)) return false;
        return true;
    }

    // Checks whether the index finger is correctly positioned
    public bool IndexCorrect(LetterCondition lc)
    {
        return lc.Index == avgIndex.Average;
    }

    // Checks whether the middle finger is correctly positioned
    public bool MiddleCorrect(LetterCondition lc)
    {
        return lc.Middle == avgMiddle.Average;
    }

    // Checks whether the ring finger is correctly positioned
    public bool RingCorrect(LetterCondition lc)
    {
        return lc.Ring == avgRing.Average;
    }

    // Checks whether the little finger is correctly positioned
    public bool LittleCorrect(LetterCondition lc)
    {
        return lc.Little == avgLittle.Average;
    }

    // Returns an attribute with the current state of the hand
    public Attribute GetAttribute(string time)
    {
        return new Attribute(
            time,
            avgIndex.Average,
            avgMiddle.Average,
            avgRing.Average,
            avgLittle.Average,
            avgThumpStraightness.Average,
            avgThumpRotation.Average,
            avgThumpSpread.Average,
            touches[6].Average,
            touches[7].Average,
            touches[8].Average,
            touches[9].Average,
            touches[11].Average,
            touches[12].Average,
            touches[13].Average,
            touches[15].Average,
            touches[19].Average);
    }

    // Gives feedback for fingers
    private void getFingerFeedback(FingerPose a, FingerPose b, string name, ref List<string> feedback, ref bool success)
    {
        if (a == b) return;
        success = false;
        if (a < b) feedback.Add("Try holding your " + name + " finger more straight");
        else feedback.Add("Try holding your " + name + " finger more angled");
    }

    private bool IsInRange(float value, float reference, float range = 15)
    {
        return Mathf.Abs(reference - value) <= range;
    }

    // Updates the states of all fingers
    private void setFingerPoses()
    {
        avgIndex.Add(calculateFinger(joints.GetRange(5, 4), joints[0]));
        avgMiddle.Add(calculateFinger(joints.GetRange(9, 4), joints[0]));
        avgRing.Add(calculateFinger(joints.GetRange(13, 4), joints[0]));
        avgLittle.Add(calculateFinger(joints.GetRange(17, 4), joints[0]));
        avgThumpStraightness.Add(calculateThumpStraightness());
        avgThumpSpread.Add(calculateThumpSpread());
        avgThumpRotation.Add(calculateThumpRotation2());
        foreach (int num in touchNumbers)
        {
            touches[num].Add(ThumpTouches(num));
        }
    }

    // Calculates the pose for a finger
    private FingerPose calculateFinger(List<Vector3> fingerJoints, Vector3 zero)
    {
        //0->1: a, 2->3:b
        Vector3 a = fingerJoints[1] - fingerJoints[0];
        Vector3 b = fingerJoints[3] - fingerJoints[2];
        float angle = Vector3.Angle(a, b);
        if (angle < 45)
        {
            return FingerPose.Straight;
        }
        else if (angle < 140)
        {
            return FingerPose.Half;
        }
        else
        {
            return FingerPose.Full;
        }
    }

    // Calculates the straightness of the thump
    private float calculateThumpStraightness()
    {
        Vector3 a = joints[4] - joints[3];
        Vector3 b = joints[2] - joints[1];
        return Vector3.Angle(a, b);
    }

    // Calculates the spread of the thump
    private float calculateThumpSpread()
    {
        //angle betwen (3,2) and (1,5)
        Vector3 a = joints[2] - joints[3];
        Vector3 b = joints[5] - joints[1];
        return Vector3.Angle(a, b);
    }

    // Calculates the rotation of the thump towards the palm of the hand
    private float calculateThumpRotation2()
    {
        Vector3 axis = joints[5] - joints[0];
        Vector3 axisNorm = axis.normalized;

        //create vector (0,17)
        float handScalar = Vector3.Dot(joints[17] - joints[0], axisNorm);
        Vector3 handAxisPoint = joints[0] + handScalar * axisNorm;
        Vector3 handToAxis = joints[17] - handAxisPoint;

        //create vector (0,3)
        float thumpScalar = Vector3.Dot(joints[3] - joints[0], axisNorm);
        Vector3 thumpAxisPoint = joints[0] + thumpScalar * axisNorm;
        Vector3 thumpToAxis = joints[3] - thumpAxisPoint;
        return Vector3.Angle(handToAxis, thumpToAxis);
    }

    // Checks whether the tip of the thump touches any of the given points
    private bool ThumpTouches(List<int> other, int thump = 4)
    {
        return other.Any(x => Vector3.Distance(joints[thump], joints[x]) / Vector3.Distance(joints[5], joints[9]) < 2);
    }

    // Checks whether the tip of the thump touches the given point
    private bool ThumpTouches(int other, int thump = 4)
    {
        return ThumpTouches(new List<int> { other }, thump);
    }
}
