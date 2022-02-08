using UnityEngine;
using UnityEngine.UI;

public class cPointBehavior : MonoBehaviour
{
    // Start is called before the first frame update

    public int max_health = 40;
    public int health = 40;
    public string owner = "Red";
    public TextMesh text;
    public Material redSide;
    public Material greenSide;
    public Slider slider;

    private RTSAgentManager agentManager;
    private RTSEAgentManager EagentManager;
    private float time = 0;
    void Start()
    {
        agentManager = GameObject.Find("Managers").GetComponent<RTSAgentManager>();
        EagentManager = GameObject.Find("Managers").GetComponent<RTSEAgentManager>();
        text = this.transform.Find("textMesh").GetComponent<TextMesh>();
        text.text = max_health.ToString() + "/" + max_health.ToString();
    }

    public void onTriggerEnter(Collider Other)
    {
        GameObject ot = Other.gameObject;
        if (agentManager.IsAgent(Other.gameObject) || EagentManager.IsAgent(Other.gameObject))
        {
            Agent ag = Other.gameObject.GetComponent<Agent>();
            if (!ag.alignment.Equals(owner))
            {
                health -= 1;
            }
            if (health <= 0)
            {
                this.owner = ag.alignment;
                health = max_health;
            }
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (slider)
        {
            slider.value = health;
        }
        if (health < max_health)
        {
            time += Time.deltaTime;
            if (time >= 5.0f)
            {
                health++;
                time = 0;
            }

        }
        text.text = health.ToString() + "/" + max_health.ToString();
        if (this.owner.Equals("Red"))
        {
            gameObject.GetComponent<Renderer>().material = redSide;
            if(transform.childCount > 1){
                transform.GetChild(1).gameObject.GetComponent<ParticleSystem>().startColor = new Color(1, 0, 0, 1);
            }
        }
        if (this.owner.Equals("Green"))
        {
            gameObject.GetComponent<Renderer>().material = greenSide;
            if(transform.childCount > 1){
                transform.GetChild(1).gameObject.GetComponent<ParticleSystem>().startColor = new Color(0, 1, 0, 1);
            }
        }
    }
}
