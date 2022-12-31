using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Problem
{
    public Room room;
    public string textCom;
    public string textDesc;
    public string solution;
    public bool available;

    public Problem(Room roomIn, string textComIn, string textDescIn, string solutionIn, bool availableIn = true)
    {
        this.room = roomIn;
        this.textCom = textComIn;
        this.textDesc = textDescIn;
        this.solution = solutionIn;
        this.available = availableIn;
    }
}
