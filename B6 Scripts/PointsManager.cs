using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class PointsManager : MonoBehaviour
{
    public GameObject endScreen;
    private List<cPointBehavior> cPoints;
    private RTSAgentManager management;
    private cPointBehavior redBase;
    private cPointBehavior greenBase;
    public AudioManager audioManager;
  
    // Start is called before the first frame update
    void Start()
    {
        Time.timeScale = 0;

        cPoints = new List<cPointBehavior>();
        GameObject[] points = GameObject.FindGameObjectsWithTag("CapturePoints");
        foreach (GameObject p in points)
        {
            cPoints.Add(p.GetComponent<cPointBehavior>());
        }

        management = this.transform.GetComponent<RTSAgentManager>();
        redBase = GameObject.Find("RedBase").GetComponent<cPointBehavior>();
        greenBase = GameObject.Find("GreenBase").GetComponent<cPointBehavior>();
        if(audioManager != null)
            audioManager.Play("BattleMusic");
    }

    public static bool isCpoint(GameObject obj)
    {
        return obj.GetComponent<cPointBehavior>();
    }
    // Update is called once per frame
    void Update()
    {
        if (!redBase.owner.Equals("Red") || !greenBase.owner.Equals("Green"))
        {
            Time.timeScale = 0;
            endScreen.SetActive(true);
        }
        int multR = 0;
        foreach (cPointBehavior c in cPoints)
        {
            if (c.owner.Equals("Red"))
            {
                multR++;
            }
        }
        management.resourceModifier = 1 +multR*0.5f;
    }
}
