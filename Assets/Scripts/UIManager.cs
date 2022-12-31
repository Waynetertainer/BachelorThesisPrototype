using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public GameObject linePrefab;

    public string ComText { set { comText.text = value; } }
    public float CountdownFillamount { set { countdown.fillAmount = value; } }
    public Color CountdownColor { set { countdown.color = value; } }
    public string ProblemSolution { set { problemSolutionText.text = value; } }
    public string ProblemDescription { set { problemDescriptionText.text = value; } }
    public string TrainingText { set { trainingText.text = value; } }
    public bool GameWon { get { return progress >= 10; } }
    public char ActiveChar { get { return GameManager.Instance.Textures.Keys.OrderBy(x => x).ElementAt(trainingSelPos); } }


    private GameObject victoryPanel;
    private GameObject trainingPanel;
    private GameObject trainingSelPanel;
    private GameObject ProblemPanel;
    private Transform progressBar;
    private Image countdown;
    private RawImage trainingImage;
    private RawImage leftTrainingSelImage;
    private RawImage middleTrainingSelImage;
    private RawImage rightTrainingSelImage;
    private TextMeshProUGUI comText;
    private TextMeshProUGUI problemSolutionText;
    private TextMeshProUGUI problemDescriptionText;
    private TextMeshProUGUI trainingText;
    private GameObject[] trainingJoints = new GameObject[21];
    private RectTransform[] trainingLines = new RectTransform[15];
    private Dictionary<char, Vector3> letterOffsets = new Dictionary<char, Vector3>();
    private int progress = 0;
    private int trainingSelPos;
    private float lastChange;



    // Start is called before the first frame update
    void Awake()
    {
        progressBar = transform.GetChild(6).GetChild(1).GetChild(1);
        trainingPanel = transform.GetChild(2).gameObject;
        trainingSelPanel = transform.GetChild(3).gameObject;
        ProblemPanel = transform.GetChild(4).gameObject;
        victoryPanel = transform.GetChild(5).gameObject;

        comText = transform.GetChild(1).GetChild(0).GetComponent<TextMeshProUGUI>();
        problemDescriptionText = ProblemPanel.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        problemSolutionText = ProblemPanel.transform.GetChild(1).GetComponent<TextMeshProUGUI>();
        countdown = ProblemPanel.transform.GetChild(2).GetComponent<Image>();
        trainingImage = trainingPanel.transform.GetChild(1).GetChild(0).GetComponent<RawImage>();
        trainingText = trainingPanel.GetComponentInChildren<TextMeshProUGUI>();
        leftTrainingSelImage = trainingSelPanel.transform.GetChild(0).GetChild(0).GetComponent<RawImage>();
        middleTrainingSelImage = trainingSelPanel.transform.GetChild(1).GetChild(0).GetComponent<RawImage>();
        rightTrainingSelImage = trainingSelPanel.transform.GetChild(2).GetChild(0).GetComponent<RawImage>();

        CloseAllPanels();
    }

    private void Start()
    {
        Transform jointParent = trainingPanel.transform.GetChild(1).GetChild(0).GetChild(0);
        Transform lineParent = trainingPanel.transform.GetChild(1).GetChild(0).GetChild(1);
        for (int i = 0; i < 21; i++)
        {
            trainingJoints[i] = jointParent.GetChild(i).gameObject;
        }
        for (int i = 0; i < trainingLines.Length; i++)
        {
            trainingLines[i] = Instantiate(linePrefab, lineParent).GetComponent<RectTransform>();
        }

        letterOffsets.Add('A', new Vector3(140, 175, 0));
        letterOffsets.Add('B', new Vector3(140, 175, 0));
        letterOffsets.Add('C', new Vector3(75, 180, 0));
        letterOffsets.Add('D', new Vector3(140, 175, 0));
        letterOffsets.Add('E', new Vector3(140, 175, 0));
    }

    public void Restart()
    {
        GameManager.Instance.Restart();
        CloseAllPanels();
        progress = 0;
        for (int i = 0; i < 10; i++)
        {
            progressBar.GetChild(i).gameObject.SetActive(false);
        }
    }

    public void AddProgress()
    {
        if (progress < 10)
        {
            progressBar.GetChild(progress).gameObject.SetActive(true);
            progress++;
        }
    }

    public void RemoveProgress()
    {
        if (progress > 0)
        {
            progress--;
            progressBar.GetChild(progress).gameObject.SetActive(false);
        }
    }

    public void RefreshTrainingSelPositions()
    {
        lastChange = Time.realtimeSinceStartup;
        char[] chars = GameManager.Instance.Textures.Keys.OrderBy(x => x).ToArray();
        if (trainingSelPos >= GameManager.Instance.Textures.Count) return;
        if (trainingSelPos == 0)
        {
            leftTrainingSelImage.enabled = false;
        }
        else
        {
            leftTrainingSelImage.enabled = true;
            leftTrainingSelImage.texture = GameManager.Instance.Textures[chars[trainingSelPos - 1]];
        }
        middleTrainingSelImage.texture = GameManager.Instance.Textures[chars[trainingSelPos]];
        if (trainingSelPos == GameManager.Instance.Textures.Count - 1)
        {
            rightTrainingSelImage.enabled = false;
        }
        else
        {
            rightTrainingSelImage.enabled = true;
            rightTrainingSelImage.texture = GameManager.Instance.Textures[chars[trainingSelPos + 1]];
        }
    }

    public void ResetTrainingSel()
    {
        trainingSelPos = 0;
        RefreshTrainingSelPositions();
    }

    public void TrainingSelRight()
    {
        if (GameManager.Instance.Textures.Count == 1) return;
        if (trainingSelPos == GameManager.Instance.Textures.Count - 1) return;
        if (lastChange + 1 >= Time.realtimeSinceStartup) return;
        trainingSelPos++;
        RefreshTrainingSelPositions();
    }

    public void TrainingSelLeft()
    {
        if (GameManager.Instance.Textures.Count == 1) return;
        if (trainingSelPos == 0) return;
        if (lastChange + 1 >= Time.realtimeSinceStartup) return;
        trainingSelPos--;
        RefreshTrainingSelPositions();
    }

    // Positions the feedback joints over the reference image in training
    public void UpdateTrainingFeedback(List<Vector3> joints)
    {
        if (joints.Count == 0) return;
        Vector3[] trainingPositions = new Vector3[21];
        LetterCondition lc = GameManager.Instance.TrainingLetterCondition;
        Vector3 root = joints[0];
        float generalFactor = 12 / (joints[5] - joints[9]).magnitude;

        // Transforming the joint positions to math the image
        for (int i = 0; i < joints.Count; i++)
        {
            Vector3 jointPos = joints[i] - root;
            Vector3 newPos = new Vector3(-2 * jointPos.x, jointPos.y, 0);
            newPos *= generalFactor;
            newPos += letterOffsets[lc.Letter];
            trainingPositions[i] = newPos;
            trainingJoints[i].GetComponent<RectTransform>().localPosition = newPos;
        }
        // Positioning the lines between the joints
        for (int i = 0; i < 15; i++)
        {
            int index = i + 1 + i / 3;
            Vector3 line1Vec = trainingPositions[index + 1] - trainingPositions[index];
            float angle = Vector3.Angle(line1Vec, Vector3.right);
            if (line1Vec.y < 0)
                angle *= -1;
            trainingLines[i].localPosition = trainingPositions[index] + line1Vec / 2;
            trainingLines[i].sizeDelta = new Vector2(line1Vec.magnitude, 3);
            trainingLines[i].eulerAngles = new Vector3(0, 0, angle);
            if (i / 3 == 0)
            {
                trainingLines[i].GetComponent<Image>().color = GameManager.Instance.Recognizer.ThumpCorrect(lc) ? Color.green : Color.red;
            }
            else if (i / 3 == 1)
            {
                trainingLines[i].GetComponent<Image>().color = GameManager.Instance.Recognizer.IndexCorrect(lc) ? Color.green : Color.red;
            }
            else if (i / 3 == 2)
            {
                trainingLines[i].GetComponent<Image>().color = GameManager.Instance.Recognizer.MiddleCorrect(lc) ? Color.green : Color.red;
            }
            else if (i / 3 == 3)
            {
                trainingLines[i].GetComponent<Image>().color = GameManager.Instance.Recognizer.RingCorrect(lc) ? Color.green : Color.red;
            }
            else
            {
                trainingLines[i].GetComponent<Image>().color = GameManager.Instance.Recognizer.LittleCorrect(lc) ? Color.green : Color.red;
            }
        }
    }

    public void ActivateVictory()
    {
        CloseAllPanels();
        victoryPanel.SetActive(true);
    }

    public void ActivateProblem()
    {
        CloseAllPanels();
        ProblemPanel.SetActive(true);
    }

    public void ActivateTraining(Texture trainingTexture)
    {
        CloseAllPanels();
        trainingPanel.SetActive(true);
        trainingImage.texture = trainingTexture;
    }

    public void ActivateTrainingSel()
    {
        CloseAllPanels();
        trainingSelPanel.SetActive(true);
    }

    public void CloseAllPanels()
    {
        victoryPanel.SetActive(false);
        trainingSelPanel.SetActive(false);
        trainingPanel.SetActive(false);
        ProblemPanel.SetActive(false);
    }
}
