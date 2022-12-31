using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using Unity.Collections;

public static class Extensions
{
    public static string Colored(this string str, Color col)
    {
        return $"<color={$"#{ColorUtility.ToHtmlStringRGBA(col)}"}>{str}</color>";
    }
}

// Creates a number that is the average of the last 10 numbers that were added
[System.Serializable]
public class AvgNum<T> where T : IComparable<T>
{
    private T[] elements = new T[10];
    private int iterator = 0;
    public T Average;
    public void Add(T newElement)
    {
        elements[iterator % 10] = newElement;
        Average = CalculateAverage();
        iterator++;
    }

    private T CalculateAverage()
    {
        dynamic a = elements[0];
        foreach (var element in elements)
        {
            dynamic b = element;
            a += b;
        }
        return a / 10;
    }
}

// Creates a boolean that represents the majority of the last 10 added entries
[System.Serializable]
public class AvgBool
{
    private bool[] elements = new bool[10];
    private int iterator = 0;
    public bool Average;

    public void Add(bool newValue)
    {
        elements[iterator % 10] = newValue;
        Average = CalculateAverage();
        iterator++;
    }

    private bool CalculateAverage()
    {
        return elements.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key;
    }
}

// Creates a FingerPose that represents the majority of the last 10 added entries
[System.Serializable]
public class AvgFinger
{
    private FingerPose[] elements = new FingerPose[10];
    private int iterator = 0;
    public FingerPose Average;

    public void Add(FingerPose newValue)
    {
        elements[iterator % 10] = newValue;
        Average = CalculateAverage();
        iterator++;
    }

    private FingerPose CalculateAverage()
    {
        return elements.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key;
    }
}
