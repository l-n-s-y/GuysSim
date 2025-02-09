using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WorldController : MonoBehaviour
{
    public GameObject adultPrefab;
    public GameObject childPrefab;


    private GameObject[] foods;
    public GameObject[] Foods {
        get {
            return foods;
        }
    }


    private GameObject[] guys;
    public GameObject[] Guys {
        get {
            return guys;
        }
    }

    private GameObject[] houses;
    public GameObject[] Houses {
        get {
            return houses;
        }
    }

    public GameObject[] dogs;
    public GameObject[] Dogs {
        get { return dogs; }
    }

    [Range(0,500)]
    public float TimeScale = Globals.INITIAL_TIMESCALE;

    float worldTime = 0;
    public float WorldTime {
        get { return worldTime; }
    }
    int years = 0;
    public int Years {
        get { return years; }
    }

    Color averageColour;

    float foodSpawnPeriod = 0.3f;
    float xRange;
    float yRange;

    bool spawnNewFood = false;

    BoxCollider2D rc;

    public GameObject foodTemplate;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rc = GetComponent<BoxCollider2D>();
        xRange = (rc.size.x/2) * transform.localScale.x;
        yRange = (rc.size.y/2) * transform.localScale.y;
    }

    Dictionary<Guy,List<(Food,float)>> guyFoodDistances;
    Dictionary<Guy,List<(Guy,float)>> guyPartnerDistances;
    
    Dictionary<Dog,List<(Food,float)>> dogFoodDistances;
    Dictionary<Dog,List<(Dog,float)>> dogPartnerDistances;
    // List<(Food,float)> guyFoodDistances;
    // List<(Guy,float)> guyPartnerDistances;

    void UpdatePartnerDistances() {
        guyPartnerDistances = new Dictionary<Guy,List<(Guy,float)>>();
        float distance;
        List<(Guy,float)> list;
        Guy guy;
        Guy partner;
        foreach (GameObject g in guys) {
            guy = g.GetComponent<Guy>();
            if (guy.IsChild()) { continue; }
            list = new List<(Guy,float)>();
            foreach (GameObject p in guys) {
                if (g==p) { continue; }
                partner = p.GetComponent<Guy>();
                if (partner.IsChild()) { continue; }
                distance = Vector3.Distance(g.transform.position,p.transform.position);
                // guyFoodDistances.Add(g,f,distance);
                list.Add((partner,distance));
            }
            guyPartnerDistances.Add(guy,list);
        }

        // TODO: add dog partner seeking
    }

    void UpdateFoodDistances() {

        // Guy -> Food distances
        guyFoodDistances = new Dictionary<Guy,List<(Food,float)>>();
        float distance = 0;
        List<(Food,float)> list;
        Guy guy;
        foreach (GameObject g in guys) {
            guy = g.GetComponent<Guy>();
            list = new List<(Food,float)>();
            foreach (GameObject f in foods) {
                distance = Vector3.Distance(g.transform.position,f.transform.position);
                // guyFoodDistances.Add(g,f,distance);
                list.Add((f.GetComponent<Food>(),distance));
            }
            guyFoodDistances.Add(guy,list);
        }

        // Dog -> Food distances
        dogFoodDistances = new Dictionary<Dog,List<(Food,float)>>();
        Dog dog;
        foreach (GameObject d in dogs) {
            dog = d.GetComponent<Dog>();
            list = new List<(Food,float)>();
            foreach (GameObject f in foods) {
                distance = Vector3.Distance(d.transform.position,f.transform.position);
                list.Add((f.GetComponent<Food>(),distance));
            }
            dogFoodDistances.Add(dog,list);
        } 
    }

    public List<(Food,float)> GetFoodDistances(Dog target) {
        if (!dogFoodDistances.ContainsKey(target)) { return null; }
        return dogFoodDistances[target];
    }

    public List<(Food,float)> GetFoodDistances(Guy target) {
        if (!guyFoodDistances.ContainsKey(target)) { return null; }
        return guyFoodDistances[target];
    }

    public List<(Guy,float)> GetPartnerDistances(Guy target) {
        if (!guyPartnerDistances.ContainsKey(target)) { return null; }
        return guyPartnerDistances[target];
    }

    public void UpdateFoods() {
        foods = GameObject.FindGameObjectsWithTag("food");
    }

    public void UpdateGuys() {
        guys = GameObject.FindGameObjectsWithTag("guy");
    }

    public void UpdateDogs() {
        dogs = GameObject.FindGameObjectsWithTag("dog");
    }

    public void UpdateHouses() {
        houses = GameObject.FindGameObjectsWithTag("house");
    }


    void Update()
    {
        // foods = GameObject.FindGameObjectsWithTag("food");
        // guys = GameObject.FindGameObjectsWithTag("guy");
        // dogs = GameObject.FindGameObjectsWithTag("dog");

        UpdateFoods();
        UpdateGuys();
        UpdateHouses();
        UpdateDogs();
        // UpdateHouses();     TODO: implement

        // DEBUG
        foreach (GameObject house in houses) { 
            Debug.Log($"{house.transform.name}: {house.GetComponent<House>().OccupantsInHouseCount}/{house.GetComponent<House>().OccupantsCount} ");
        }
        // DEBUG


        if (guys.Length == 0) {
            return;
        }

        // if (guyPartnerDistances != null && guyPartnerDistances.Count != guys.Length) {
        UpdatePartnerDistances();
        // }

        // if (guyFoodDistances != null && (guyFoodDistances.Count != guys.Length || guyFoodDistances[guys[0].GetComponent<Guy>()].Count != foods.Length)) {
        UpdateFoodDistances();
        // }

        // TODO: Implement a better method of tracking world time
        if (TimeScale == 0) { return; }

        worldTime += Time.deltaTime*(TimeScale/3);
        int yearCheck = (int)Mathf.Floor(worldTime/Globals.YEAR);
        if (yearCheck > years) { // A new year has passed? (number is arbitrary margin of error)
            // sendYearMsg = true;
            years = yearCheck;
            SendTimeUpdates();
        }


        if (!spawnNewFood) {
            spawnNewFood = true;
            StartCoroutine(SpawnFood());
            StartCoroutine(SpawnFood());
            StartCoroutine(SpawnFood());
            StartCoroutine(SpawnFood());
        }
    }

    public void UpdateTimeScale(float value) {
        StopCoroutine(SpawnFood());
        if (value < 0 || value == float.PositiveInfinity) { return; }
        TimeScale = value;
    }

    public void ResetScene() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void SendTimeUpdates() {
        foreach (GameObject guy in guys) {
            guy.SendMessage("Age",years);
        }
        foreach (GameObject dog in dogs) {
            dog.SendMessage("Age",years);
        }
    }

    IEnumerator SpawnFood() {
        yield return new WaitForSeconds(foodSpawnPeriod/TimeScale);
        // foodSpawnPeriod = Mathf.Max(0,foodSpawnPeriod-0.001f);

        Vector2 randomPos = GetRandomWorldPosition();

        Instantiate(foodTemplate, randomPos, Quaternion.identity);
        spawnNewFood = false;
    }

    public Color[] GetGuyColours(GameObject[] guys) {
        Color[] colours = new Color[guys.Length];

        if (guys.Length == 0) { return colours; }
        Guy guy;
        for (int i=0; i<guys.Length; i++) {
            guy = guys[i].GetComponent<Guy>();
            if (guys[i] == null || guy == null) { continue; }
            colours[i] = guy.Genes.Color;
        }

        return colours;

    }

    public Vector2 GetRandomWorldPosition() {
        float randX = Random.Range(-xRange,xRange);
        float randY = Random.Range(-yRange,yRange);
        Vector2 pos = new Vector2(randX,randY);

        return pos;
    }

    public Vector2 GetRandomWorldPositionInRadius(Vector2 source, float radius) {
        // pick a random angle
        float angle = Random.Range(1,360)*Globals.DEG2RAD;

        // march a random portion of radius
        float hypot = Random.Range(1,radius*10) % radius;
        float x = Mathf.Cos(angle) * hypot;
        float y = Mathf.Sin(angle) * hypot;
        

        // y = Mathf.Min(Mathf.Max(-yRange,y),yRange);

        Vector2 newDest = source + new Vector2(x,y);

        newDest.x = Mathf.Min(Mathf.Max(-xRange,newDest.x),xRange);
        newDest.y = Mathf.Min(Mathf.Max(-yRange,newDest.y),yRange);

        // resulting pos is returned
        return newDest;
    }



}
