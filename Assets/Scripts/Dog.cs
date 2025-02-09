using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Dog : MonoBehaviour
{
    public static float MAX_ENERGY = 100;
    public static float MAX_URGENCY = 100;

    public static float DEFAULT_BASE_ENERGY_LOSS = 1f;
    public static float DEFAULT_WALK_SPEED = 3;
    public static float DEFAULT_RUN_SPEED = 6;
    public static float DEFAULT_EAT_COOLDOWN = 0.5f;
    public static float DEFAULT_MATING_COOLDOWN = 2f;
    public static float DEFAULT_REFRACTORY_COOLDOWN = 4f;
    public static float DEFAULT_FOOD_FIND_COOLDOWN = 0.3f;
    public static float DEFAULT_PARTNER_FIND_COOLDOWN = 0.3f;

    public static Color DEFAULT_COLOR = Color.red;
    public static float DEFAULT_INTERACTION_DISTANCE = 0.75f;
    public static float DEFAULT_RANGE_OF_VISION = 25f;
    public const float DEFAULT_COLOUR_COMPATABILITY_RANGE = 2f;


    WorldController wc;
    UIController uic;

    public int age = Globals.ADULT_STARTING_AGE;

    int startingYear;
    float energy = 100f; // 0 > 100f (100 = full)
    public float Energy {
        get { return energy; }
    }
    protected GeneSequence genes;
    public GeneSequence Genes {
        get { return genes; }
    }
    float baseEnergyLoss = DEFAULT_BASE_ENERGY_LOSS;
    float walkSpeed = DEFAULT_WALK_SPEED;
    float runSpeed = DEFAULT_RUN_SPEED;
    float interactionDistance = DEFAULT_INTERACTION_DISTANCE;
    float urgency = 1; // 1 => 100 (100 = very urgent)

    float eatCooldownPeriod = DEFAULT_EAT_COOLDOWN;
    float matingCooldownPeriod = DEFAULT_MATING_COOLDOWN;
    float refractoryCooldownPeriod = DEFAULT_REFRACTORY_COOLDOWN;
    [Range(0.00f,DEFAULT_COLOUR_COMPATABILITY_RANGE)]
    public float partnerCompatabilityRange = DEFAULT_COLOUR_COMPATABILITY_RANGE;
    float rangeOfVision = DEFAULT_RANGE_OF_VISION;

    float foodFindCooldownPeriod = DEFAULT_FOOD_FIND_COOLDOWN;
    float partnerFindCooldownPeriod = DEFAULT_PARTNER_FIND_COOLDOWN;
    bool foodFindCooldown = false;
    bool partnerFindCooldown = false;

    float activeTimeScale = 1;

    int lastReactedYear;
    public Color color = Color.black;

    bool eating = false;
    bool alive = true;
    public bool Alive {
        get {
            return alive;
        }
    }
    bool isMating = false;
    public bool IsMating {
        get { return isMating; }
    }
    bool inRange = false;
    bool doResetTarget = false;
    bool canFlip = true;
    bool matingRoutineRunning = false;
    bool refractory = false;
    bool aged = false;

    SpriteRenderer renderer;
    List<Guy> children;
    public List<Guy> Children {
        get { return children; }
    }
    float targetDistance = 0;

    // string movementState = "still";
    string mood = "neutral";
    public string Mood {
        get { return mood; }
    }
    string matePosition = ""; // "","mother","father"
    public string MatePosition {
        get {return matePosition;}
    }

    Vector3 destination = Globals.UNSET_DESTINATION;
    Vector3 realScale = Vector3.one;
    GameObject foodTarget = null;
    public GameObject FoodTarget {
        get { return foodTarget; }
    }
    GameObject partnerTarget = null;
    public GameObject PartnerTarget {
        get { return partnerTarget; }
    }

    Vector3 walkTarget = Globals.UNSET_DESTINATION;
    public Vector3 WalkTarget {
        get { return walkTarget; }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        renderer = GetComponent<SpriteRenderer>();
        wc = GameObject.Find("WorldController").GetComponent<WorldController>();

        uic = GameObject.Find("Canvas").GetComponent<UIController>();

        realScale = transform.localScale;
        startingYear = wc.Years;
        lastReactedYear = startingYear;
        // children = new List<Guy>();
        Age(age);
        if (age>0) { // Starting adults generate new sequences, children get theirs from their parents
            genes = GeneSequence.GenerateNewSequence();
        }

        if (color == Color.white) {
            // genes.Color = UnityEngine.Random.ColorHSV();
        } else if (color != Color.black) {
            genes.Color = color;
        }
        renderer.color = genes.Color;

        // Deactivate object for a slight random delay to allow guys to desync
        StartCoroutine(DelayStart());
        this.enabled = false;
    }

    IEnumerator DelayStart() {
        float period = Random.Range(0.001f,0.00001f);
        yield return new WaitForSeconds(period);
        this.enabled = true;
    }

    // Update is called once per frame
    bool flip = false;
    float angle;
    void Update()
    {
        // LogDebugStats();
        if (!alive) { return; }
        
        // Move towards current target
        switch (mood) {
            // case "honry":
            //     if (partnerTarget == null || isMating) { break; }
            //     MoveTowardsTarget(partnerTarget.transform);
            //     break;
            case "hungry":
                if (foodTarget == null) { break; }
                MoveTowardsTarget(foodTarget.transform);
                break;

            case "neutral":
                if (walkTarget == Globals.UNSET_DESTINATION || isMating) { break; }
                // if (!flip) {
                angle = Globals.Vector2Angle(transform.position,walkTarget)*Globals.RAD2DEG;
                    // flip = true;
                // }
                if (angle >= 0) {
                    transform.rotation = Quaternion.Euler(new Vector3(0,180,0));
                } else {
                    transform.rotation = Quaternion.Euler(new Vector3(0,0,0));
                }
                MoveTowardsTarget(walkTarget);
                break;

            default:
                targetDistance = 0;
                break;
        }

        // Time acceleration
        if (activeTimeScale != wc.TimeScale) {
            AccelerateTime(wc.TimeScale);
            activeTimeScale = wc.TimeScale;
        }


        // Autonomous body functions
        BodyFunction();

        // Update mood and rect to environment
        Evaluate();

        // Move destination reset
        if (doResetTarget) {
            foodTarget = null;
            partnerTarget = null;
            doResetTarget = false;
        }


        // Scale sprite to camera zoom
        // Vector3 zoom = uic.CameraZoom * new Vector3(0.1f,0.1f,0.1f);
        // zoom = Globals.ClampVector3(zoom,Globals.MIN_ZOOM_SCALE,Globals.MAX_ZOOM_SCALE);
        transform.localScale = realScale * Mathf.Clamp((uic.CameraZoom * 0.1f),Globals.MIN_ZOOM_SCALE,Globals.MAX_ZOOM_SCALE);
        // transform.localScale = Mathf.Clamp(uic.CameraZoom*0.1f),Globals.MIN_ZOOM_SCALE,Globals.MAX_ZOOM_SCALE);
        // transform.localScale = Globals.ClampVector3(GrowChild((int)age) * uic.CameraZoom * 0.1f,Globals.MIN_ZOOM_SCALE,Globals.MAX_ZOOM_SCALE);

    }

    void AccelerateTime(float timeScale) {
        // Stop running coroutines and restart again with accurate offsets in current timescale
        StopCoroutine("MatingRoutine");
        StopCoroutine("RefractoryReset");
        StopCoroutine("EatCooldown");
        StopCoroutine("FindFoodCooldown");
        StopCoroutine("FindPartnerCooldown");

        if (timeScale == 0) {
            baseEnergyLoss = 0;
            walkSpeed = 0;
            runSpeed = 0;
            eatCooldownPeriod = float.PositiveInfinity;
            refractoryCooldownPeriod = float.PositiveInfinity;
            foodFindCooldownPeriod = float.PositiveInfinity;
            matingCooldownPeriod = float.PositiveInfinity;
            partnerFindCooldownPeriod = float.PositiveInfinity;
            return;
        }


        baseEnergyLoss = DEFAULT_BASE_ENERGY_LOSS * (timeScale/3);
        walkSpeed = DEFAULT_WALK_SPEED * timeScale;
        runSpeed = DEFAULT_RUN_SPEED * timeScale;
        eatCooldownPeriod = DEFAULT_EAT_COOLDOWN / timeScale;
        refractoryCooldownPeriod = DEFAULT_REFRACTORY_COOLDOWN / timeScale;
        foodFindCooldownPeriod = DEFAULT_FOOD_FIND_COOLDOWN / timeScale;
        matingCooldownPeriod = DEFAULT_MATING_COOLDOWN / timeScale;
        partnerFindCooldownPeriod = DEFAULT_PARTNER_FIND_COOLDOWN / timeScale;
    }

    public void Age(int years) {
        if (aged || !alive) { return; }

        // Children grow a little every year up to 20
        // if (IsChild()) {
        //     // TODO: grow code    
        //     transform.localScale = GrowChild(age);
        //     realScale = transform.localScale;
        // }

        // Elderly die
        if (age >= Globals.LIFE_EXPECTANCY) {
            Die();
        }

        age += years - lastReactedYear;
        lastReactedYear = years;
    }

    void BodyFunction() {
        // Use energy on metabolism
        energy -= baseEnergyLoss * Time.deltaTime;

        // TODO: finish this
        // Lose more energy for moving
        // (Hopefully this will help desync FindFood() and FindPartner() calls by 
        // forcing each Guy to use different amounts of energy and improve performance
        // as a result of ungrouping these calls)
        // if (walkTarget != null || foodTarget != null || partnerTarget != null) {
        //     energy -= (movementEnergyLossPerUnit * targetDistance) * Time.deltaTime;
        // }

        if (energy <= 0) {
            Die();
        }

        // Develop disease or other shit? idk
    }

    void Die() {
        Destroy(gameObject);
        // Turn colour black
        // renderer.color = Color.black;

        // Disable collision
        GetComponent<BoxCollider2D>().enabled = false;

        // Set state
        alive = false;
        mood = "dead";
        gameObject.tag = "dead";
    }

    void Evaluate() {
        // Change mood based on state and environment
        if (energy > 50 && mood == "hungry") {
            mood = "neutral";
        } else if (energy <= 50) {
            mood = "hungry";
        }

        // if (energy > 75 && mood == "neutral" && !refractory && !IsChild()) { // && children.Count < Globals.MAX_CHILD_COUNT) {
        //     mood = "honry";
        // }

        // Act on mood
        switch (mood) {
            // case "honry":
            //     // Find closest compatible guy and be friends :)
            //     if (partnerTarget == null) {
            //         if (partnerFindCooldown) { break; }
            //         StartCoroutine(FindPartnerCooldown());
            //         partnerFindCooldown = true;
            //         partnerTarget = FindPartner();
            //     } else if (inRange && !refractory) {
            //         MatePartner(partnerTarget.GetComponent<Guy>());
            //         partnerTarget = null;
            //     } else if (partnerTarget != null && partnerTarget.GetComponent<Guy>().IsMating) {
            //         mood = "neutral";
            //         partnerTarget = null;
            //     }
            //     break;

            case "hungry":
                // Find closest food and set dest
                if (foodTarget == null) {
                    // destination = FindFood();
                    if (foodFindCooldown) { break; }
                    StartCoroutine(FindFoodCooldown());
                    foodFindCooldown = true;
                    foodTarget = FindFood();
                    if (foodTarget == null) { mood = "neutral"; }
                    // destination = foodTarget.transform.position;
                } else if (inRange && !eating) {
                    EatFood(foodTarget);
                    foodTarget = null;
                    // destination = Globals.UNSET_DESTINATION;
                    // inRange = false;
                }
                break;

            case "neutral":
                if (walkTarget == Globals.UNSET_DESTINATION) {
                    walkTarget = wc.GetRandomWorldPositionInRadius(transform.position,rangeOfVision);
                } else if (inRange) {
                    walkTarget = Globals.UNSET_DESTINATION;
                    inRange = false;
                }
                break;

            default:
                break;
        }
    }

    GameObject FindFood() {
        GameObject newTarget = null;
        float minDistance = float.PositiveInfinity;
        float currDistance;

        List<(Food,float)> distances = wc.GetFoodDistances(this);
        if (distances == null) {
            return newTarget;
        }
        // distances = distances.OrderBy(t => t.Item2); // sort by distance
        distances.Sort((x,y) => x.Item2.CompareTo(y.Item2)); // sort by distance
        foreach ((Food,float) f in distances) {
            if (f.Item1 == null) { continue; }
            if (f.Item2 > rangeOfVision) { continue; }

            minDistance = f.Item2;
            newTarget = f.Item1.gameObject;
            break;
        }

        return newTarget;
    }

    IEnumerator EatCooldown(GameObject food) {
        yield return new WaitForSeconds(eatCooldownPeriod); // guy has to chew
        energy = Mathf.Min(Guy.MAX_ENERGY,energy+Globals.FOOD_ENERGY);
        Destroy(food);
        yield return new WaitForSeconds(eatCooldownPeriod/4); // chill after eating
        eating = false;
    }

    void EatFood(GameObject food) {
        Food fd;
        try {
            fd = food.GetComponent<Food>();
        } catch {
            return;
        }

        if (!eating && fd.EatMe()) {
            eating = true;
            StartCoroutine(EatCooldown(food));
            inRange = false;
        }
    }

    IEnumerator FindFoodCooldown() {
        yield return new WaitForSeconds(foodFindCooldownPeriod);
        foodFindCooldown = false;
    }

    void MoveTowardsTarget(Transform target) {
        MoveTowardsTarget(target.position);
    }

    
    void MoveTowardsTarget(Vector3 destination) {

        Debug.DrawLine(transform.position,destination,Color.red);

        float spd = Mathf.Lerp(walkSpeed,runSpeed,urgency/100); // adjust speed based on urgency
        // if (destination != Globals.UNSET_DESTINATION) {
        targetDistance = Vector3.Distance(transform.position, destination);
        inRange = targetDistance <= interactionDistance;    

        // } else {
            // inRange = false;
        // }

        // Invalid destination (computing sqrMagnitude is faster than regular magnitude)
        if (destination == Globals.UNSET_DESTINATION || destination.sqrMagnitude == float.PositiveInfinity) { 
            return; 
        }

        // Debug.DrawRay(transform.position,destination,Color.red);

        // Destination reached. No need for move
        if (Vector3.Distance(transform.position,destination) <= Mathf.Max(interactionDistance,spd*Time.deltaTime)) {
            return;
        }

        Vector3 destPos = Vector3.MoveTowards(transform.position,destination,walkSpeed*Time.deltaTime);
        transform.position = destPos;
    }

    void OnMouseOver() {
        if (Input.GetMouseButtonDown(0)) {
            Debug.Log($"CLICKED ME {transform.name}");
            GameObject.Find("Canvas").SendMessage("ClickEntity", transform);
        }
    }
}
