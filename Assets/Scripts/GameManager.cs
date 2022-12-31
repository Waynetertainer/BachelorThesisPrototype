using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public int StoryProgression = 0;
    public bool CamStarted;
    public float LastUserEvent;
    public RawImage VideoTarget;
    public GameObject Canvas;
    public PythonInterface PyInterface;
    public FingerRecognizer Recognizer;
    public Dictionary<char, Texture2D> Textures = new Dictionary<char, Texture2D>();

    private int problemProgress;
    private float lastTextChange;
    private float problemEndTime;
    private Room room;
    private State state;
    private Skybox skybox;
    private Problem activeProblem;
    private UIManager uiManager;
    private System.Random rnd = new System.Random();
    private List<Problem> allProblems = new List<Problem>();
    private List<LetterCondition> letterConditions = new List<LetterCondition>();
    private Dictionary<int, string> story = new Dictionary<int, string>();
    private Dictionary<string, string> texts = new Dictionary<string, string>();

    public LetterCondition TrainingLetterCondition { get {return letterConditions.Where(x => x.Letter == uiManager.ActiveChar).First(); } }

    private void OnEnable()
    {
        PythonInterface.eOnFrameReceived += ReceiveCamStart;
    }

    private void OnDisable()
    {
        PythonInterface.eOnFrameReceived -= ReceiveCamStart;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            GameObject canvas = Instantiate(Canvas);
            DontDestroyOnLoad(canvas);
            DontDestroyOnLoad(gameObject);
            uiManager = canvas.GetComponent<UIManager>();
            room = Room.Command;
            state = State.Start;
            VideoTarget = canvas.transform.GetChild(0).GetComponent<RawImage>();
            
            Recognizer = GetComponent<FingerRecognizer>();
            PyInterface = GetComponent<PythonInterface>();
            skybox = FindObjectOfType<Skybox>();
            ReadProblems();
            ReadLetterConditions();
            FillDicts();
            uiManager.ComText = story[0];
            foreach (LetterCondition c in letterConditions)
            {
                Texture2D tex = Resources.Load("Textures/" + c.Letter + "2") as Texture2D;
                Textures.Add(c.Letter, tex);
            }
        }
        else
        {
            Destroy(gameObject);
        }

    }

    // Update is called once per frame
    void Update()
    {
        if (skybox == null)
        {
            skybox = FindObjectOfType<Skybox>();
        }
        else
        {
            skybox.material.SetFloat("_Rotation", (Time.time * 0.5f) % 360);
        }


        if (!CamStarted) return;
        switch (state)
        {
            case State.Start:
                if (Recognizer.StraightFingers() == 5)
                {
                    StoryProgression = 9;
                    GoToTrainingSel();
                    break;
                }
                if (StoryProgression == 0 || Time.realtimeSinceStartup > lastTextChange + 5)
                {
                    lastTextChange = Time.realtimeSinceStartup;
                    if (StoryProgression >= 9)
                    {
                        GoToTrainingSel();
                    }
                    else
                    {
                        StoryProgression++;
                        uiManager.ComText = story[StoryProgression];
                    }
                }
                break;
            case State.Room:
                if (Recognizer.StraightFingers() == 2)
                {
                    GoToTrainingSel();
                }
                else if (Recognizer.StraightFingers() == 3 && allProblems.Any(x => x.available && x.room != room))
                {
                    GoToRoom(allProblems.Where(x => x.available && x.room != room).First().room);
                }
                else if (allProblems.Where(x => x.available && x.room == room).Count() > 0 && Recognizer.StraightFingers() == 1)
                {
                    GoToProblem();
                }
                break;
            case State.Problem:
                if (Recognizer.StraightFingers() == 5)
                {
                    GoToRoom();
                }
                float percentageTimeLeft = (problemEndTime - Time.realtimeSinceStartup) / (activeProblem.solution.Length * 5);
                // problem solved
                if (problemProgress == activeProblem.solution.Length)
                {
                    uiManager.AddProgress();
                    uiManager.ComText = uiManager.GameWon? "That was all the data needed, I am ready for the reboot." : "Thank you!";

                    int newProblemCount = allProblems.Where(x => !x.available).Count();
                    if (newProblemCount > 5)
                    {
                        allProblems.Where(x => !x.available).ElementAt(rnd.Next(0, newProblemCount)).available = true;
                    }
                    activeProblem.available = false;
                    state = State.ProblemEnd;

                }
                // no time left
                else if (percentageTimeLeft <= 0)
                {
                    uiManager.ComText = "Please try to be faster next time!";
                    uiManager.RemoveProgress();
                    state = State.ProblemEnd;
                }
                else
                {
                    // update timer
                    float green = Mathf.Min(1, percentageTimeLeft * 2f);
                    float red = Mathf.Max(0, (1 - percentageTimeLeft) * 2f);
                    uiManager.CountdownColor = new Color(red, green, 0);
                    uiManager.CountdownFillamount = percentageTimeLeft;
                    lastTextChange = Time.realtimeSinceStartup;
                    LetterCondition nextLetter = letterConditions.Where(x => x.Letter == activeProblem.solution[problemProgress]).First();
                    // colour the next letter green if recognized
                    if (Recognizer.LetterRecognized(nextLetter))
                    {
                        problemProgress++;
                        StringBuilder sb = new StringBuilder();
                        sb.Append(activeProblem.solution.Substring(0, problemProgress).Colored(Color.green));
                        sb.Append(activeProblem.solution.Substring(problemProgress));
                        uiManager.ProblemSolution = sb.ToString();
                    }
                }
                break;
            case State.ProblemEnd:
                if (lastTextChange + 3 < Time.realtimeSinceStartup)
                {
                    if (uiManager.GameWon)
                    {
                        GoToVictory();
                    }
                    else
                    {
                        GoToRoom();
                    }
                }
                break;
            case State.TrainingSel:
                switch (Recognizer.StraightFingers())
                {
                    case 1:
                        //confirm
                        GoToTraining(uiManager.ActiveChar);
                        break;
                    case 2:
                        //next
                        uiManager.TrainingSelRight();
                        break;
                    case 3:
                        //previous
                        uiManager.TrainingSelLeft();
                        break;
                    case 4:
                    case 5:
                        //back
                        GoToRoom();
                        break;
                    default:
                        break;
                }
                break;
            case State.Training:
                if (Recognizer.StraightFingers() == 5)
                {
                    GoToTrainingSel();
                }
                List<string> feedback = new List<string>();
                LetterCondition currentLetterCondition = letterConditions.Where(x => x.Letter == uiManager.ActiveChar).First();
                uiManager.UpdateTrainingFeedback(Recognizer.Joints);
                bool success = Recognizer.LetterRecognized(currentLetterCondition, ref feedback);
                uiManager.TrainingText = string.Join("\n", feedback);
                lastTextChange = Time.realtimeSinceStartup;
                if (success)
                {
                    uiManager.TrainingText = "Well done";
                    state = State.TrainingEnd;
                }
                break;
            case State.TrainingEnd:
                if (lastTextChange + 3 < Time.realtimeSinceStartup)
                {
                    GoToTrainingSel();
                }
                break;
            case State.Victory:
                if (Recognizer.StraightFingers() == 1)
                {
                    uiManager.Restart();
                }
                break;
            default:
                break;
        }
    }

    private void ReceiveCamStart()
    {
        CamStarted = true;
    }

    private void FillDicts()
    {
        story.Add(0, "Hello? Is anybody there?");
        story.Add(1, "I'm glad you're awake. We had apretty bad collision with that asteroid!");
        story.Add(2, "Sadly, I am not able to read data from the ship systems.");
        story.Add(3, "Thankfully, I can still control them so I just need the data.");
        story.Add(4, "With enough data i can reebot the system which should fix all problems.");
        story.Add(5, "Can you read the consoles for me?");
        story.Add(6, "Seems like I also can't access the microphone.");
        story.Add(7, "I found these datasheets about the German finger alphabet.");
        story.Add(8, "Maybe you can provide the data to me by using those?");
        story.Add(9, "I have prepared some training lessons for you. \nIf you want to skip this introduction next time raise your index and little finger.");
        story.Add(10, "Let's restart with the training.");

        texts.Add("SelTrainingCom", "What letter do you want to practice? \nRaise your index finger to confirm. " +
            "\nRaise your index and middle finger to selecet the next letter. " +
            "\nRaise your index, middle and ring finger to select the previous letter. " +
            "\nRaise your index and little finger to go back.");
        texts.Add("Training", "\nFor training raise your index and middle finger.");
        texts.Add("Room", "There is no problem here, what do you want to do?");
        texts.Add("RoomProb", "If you're ready to help me with the problem in this room raise your index finger.");
        texts.Add("RoomProbOther", "\nThere are problems in other rooms, if you want to go there raise your index, middle and ring finger.");
        texts.Add("TrainingCom", "Try to copy the shown gesture.");
    }

    public void ReadLetterConditions()
    {
        string ConditionsAsJson;
        using (StreamReader sr = new StreamReader(Application.streamingAssetsPath + @"/conditions.json"))
        {
            ConditionsAsJson = sr.ReadToEnd();
            LetterConditionCollection LetterConditionsCollection = JsonConvert.DeserializeObject<LetterConditionCollection>(ConditionsAsJson);
            foreach (LetterConditionStrings lcs in LetterConditionsCollection.intervals)
            {
                letterConditions.Add(new LetterCondition(lcs));
            }
        }

    }

    public void ReadProblems()
    {
        string ProblemsAsJson;
        using (StreamReader sr = new StreamReader(Application.streamingAssetsPath + @"/problems.json"))
        {
            ProblemsAsJson = sr.ReadToEnd();
            allProblems = JsonConvert.DeserializeObject<List<Problem>>(ProblemsAsJson);
        }
    }

    public void GoToTrainingSel()
    {
        Recognizer.ResetLastFingerChange();
        uiManager.ActivateTrainingSel();
        uiManager.ResetTrainingSel();
        state = State.TrainingSel;
        uiManager.ComText = texts["SelTrainingCom"];

    }

    public void GoToTraining(char letter)
    {
        uiManager.ActivateTraining(Textures[letter]);
        uiManager.ComText = texts["TrainingCom"];
        state = State.Training;
    }

    public void GoToRoom()
    {
        GoToRoom(room);
    }

    public void GoToRoom(Room targetRoom)
    {
        if (targetRoom != room && LastUserEvent + 3 < Time.realtimeSinceStartup)
        {
            room = targetRoom;
            SceneManager.LoadScene(targetRoom.ToString());
            LastUserEvent = Time.realtimeSinceStartup;
        }
        Recognizer.ResetLastFingerChange();
        uiManager.CloseAllPanels();
        state = State.Room;
        StringBuilder sb = new StringBuilder();
        if (allProblems.Any(x => x.available && x.room == room))
        {
            sb.Append(texts["RoomProb"]);
        }
        else
        {
            sb.Append(texts["Room"]);
        }
        sb.Append(texts["Training"]);
        if (allProblems.Any(x => x.available && x.room != room))
        {
            sb.Append(texts["RoomProbOther"]);
        }
        uiManager.ComText = sb.ToString();
    }

    public void GoToProblem()
    {
        uiManager.ActivateProblem();
        state = State.Problem;
        problemProgress = 0;
        int availableProblems = allProblems.Where(x => x.available && x.room == room).Count();
        activeProblem = allProblems.Where(x => x.available && x.room == room).ElementAt(rnd.Next(0, availableProblems));
        problemEndTime = Time.realtimeSinceStartup + activeProblem.solution.Length * 5;
        uiManager.ProblemDescription = activeProblem.textDesc;
        uiManager.ProblemSolution = activeProblem.solution;
        uiManager.ComText = activeProblem.textCom + "\nPlease hurry!";
    }

    public void GoToVictory()
    {
        uiManager.ActivateVictory();
        state = State.Victory;
    }

    public void Restart()
    {
        state = State.Start;
        StoryProgression = 10; 
        uiManager.ComText = story[StoryProgression];
        lastTextChange = Time.realtimeSinceStartup;
        allProblems.Select(x => x.available = true);
    }
}
