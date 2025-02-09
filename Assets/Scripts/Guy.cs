using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Guy : MonoBehaviour
{
    public static float MAX_ENERGY = 100;
    public static float MAX_URGENCY = 100;

    public static float DEFAULT_BASE_ENERGY_LOSS = 2f;
    public static float DEFAULT_WALK_SPEED = 3f;
    public static float DEFAULT_RUN_SPEED = 6f;
    public static float DEFAULT_EAT_COOLDOWN = 0.5f;
    public static float DEFAULT_MATING_COOLDOWN = 2f;
    public static float DEFAULT_REFRACTORY_COOLDOWN = 4f;
    public static float DEFAULT_FOOD_FIND_COOLDOWN = 0.3f;
    public static float DEFAULT_PARTNER_FIND_COOLDOWN = 0.3f;
    public static float DEFAULT_HOUSE_FIND_COOLDOWN = 0.5f;

    public static Color DEFAULT_COLOR = Color.red;
    public static float DEFAULT_INTERACTION_DISTANCE = 0.75f;
    public static float DEFAULT_RANGE_OF_VISION = 30f;
    public const float DEFAULT_COLOUR_COMPATABILITY_RANGE = 2f;



    // Genetics
    protected GeneSequence genes;
    public GeneSequence Genes {
        get { return genes; }
    }

    // Move randomly
    // Eat food
    // Create new guys

    WorldController wc;

    Guid id;
    public Guid Id {
        get { return id; }
    }

    public int age = Globals.ADULT_STARTING_AGE;

    protected int startingYear;

    float energy = 100f; // 0 > 100f (100 = full)
    public float Energy {
        get { return energy; }
    }
    protected float baseEnergyLoss = DEFAULT_BASE_ENERGY_LOSS;
    protected float walkSpeed = DEFAULT_WALK_SPEED;
    protected float runSpeed = DEFAULT_RUN_SPEED;
    protected float interactionDistance = DEFAULT_INTERACTION_DISTANCE;
    protected float urgency = 1; // 1 => 100 (100 = very urgent)

    float eatCooldownPeriod = DEFAULT_EAT_COOLDOWN;
    float matingCooldownPeriod = DEFAULT_MATING_COOLDOWN;
    float refractoryCooldownPeriod = DEFAULT_REFRACTORY_COOLDOWN;
    [Range(0.00f,DEFAULT_COLOUR_COMPATABILITY_RANGE)]
    public float partnerCompatabilityRange = DEFAULT_COLOUR_COMPATABILITY_RANGE;
    float rangeOfVision = DEFAULT_RANGE_OF_VISION;

    float foodFindCooldownPeriod = DEFAULT_FOOD_FIND_COOLDOWN;
    float partnerFindCooldownPeriod = DEFAULT_PARTNER_FIND_COOLDOWN;
    float houseFindCooldownPeriod = DEFAULT_HOUSE_FIND_COOLDOWN;
    bool foodFindCooldown = false;
    bool partnerFindCooldown = false;
    bool houseFindCooldown = false;

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
    bool isHome = false;
    bool antiHomesick = false;
    bool canFlip = true;
    bool matingRoutineRunning = false;
    bool refractory = false;
    bool aged = false;
    bool hasHouse = false;
    public bool HasHouse {
        get { return hasHouse; }
        set { hasHouse = value; }
    }

    SpriteRenderer renderer;


    Guy spouse = null;
    public Guy Spouse {
        get { return spouse; }
    }

    List<Guy> children;
    public List<Guy> Children {
        get { return children; }
    }
    float targetDistance = 0;

    // string movementState = "still";
    bool bb = false;
    string mood = "neutral";
    public string Mood {
        get { return mood; }
    }
    string matePosition = ""; // "","mother","father"
    public string MatePosition {
        get {return matePosition;}
    }

    Vector3 destination = Globals.UNSET_DESTINATION;
    GameObject foodTarget = null;
    public GameObject FoodTarget {
        get { return foodTarget; }
    }
    GameObject partnerTarget = null;
    public GameObject PartnerTarget {
        get { return partnerTarget; }
    }
    GameObject houseTarget = null;
    public GameObject HouseTarget {
        get { return houseTarget; }
    }

    Vector3 walkTarget = Globals.UNSET_DESTINATION;
    public Vector3 WalkTarget {
        get { return walkTarget; }
    }

    UIController uic; 

    bool cloned = false;
    public void DuplicateGuy(Guy toDuplicate) {
        cloned = true;
        genes = toDuplicate.Genes;
        age = toDuplicate.age;

        Start();

        mood = toDuplicate.Mood;
        startingYear = toDuplicate.startingYear;
        energy = toDuplicate.Energy;
        baseEnergyLoss = toDuplicate.baseEnergyLoss;
        walkSpeed = toDuplicate.walkSpeed;
        runSpeed = toDuplicate.runSpeed;
        interactionDistance = toDuplicate.interactionDistance;
        urgency = toDuplicate.urgency;
        activeTimeScale = toDuplicate.activeTimeScale;
        id = toDuplicate.Id;
        walkTarget = toDuplicate.WalkTarget;
        foodTarget = toDuplicate.FoodTarget;
        partnerTarget = toDuplicate.PartnerTarget;
        houseTarget = toDuplicate.HouseTarget;
        hasHouse = toDuplicate.HasHouse;
    }


    public static Guid GenerateGuyID() {
        return Guid.NewGuid();
    }


    void Start() {
        renderer = GetComponent<SpriteRenderer>();
        wc = GameObject.Find("WorldController").GetComponent<WorldController>();

        uic = GameObject.Find("Canvas").GetComponent<UIController>();

        realScale = transform.localScale;
        startingYear = wc.Years;
        lastReactedYear = startingYear;
        children = new List<Guy>();
        // Age(age);
        Age(wc.Years);
        if (age>0 && !cloned) { // Starting adults generate new sequences, children get theirs from their parents
            genes = GeneSequence.GenerateNewSequence();
        }

        if (color == Color.white) {
            // Debug.Log("WHITE");
            genes.Color = UnityEngine.Random.ColorHSV();
        } else if (color != Color.black) {
            genes.Color = color;
        }
        renderer.color = genes.Color;

        // Deactivate object for a slight random delay to allow guys to desync
        if (cloned) { return; } // skip delay start if cloned
        id = GenerateGuyID();

        StartCoroutine(DelayStart());
        this.enabled = false;
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

    public void RefreshGeneTraits() {
        // TODO: this is a duct tape patch for null renderers
        if (renderer == null) { renderer = GetComponent<SpriteRenderer>(); }
        renderer.color = genes.Color;
    }

    // Wait a very small random period before reenabling self
    IEnumerator DelayStart() {
        float period = UnityEngine.Random.Range(0.001f,0.00001f);
        yield return new WaitForSeconds(period);
        this.enabled = true;
    }

    void LogDebugStats() {
        Debug.ClearDeveloperConsole(); // nvm this only clears errors because unity is run by pigs and garbagemen. useless dogshit
        Debug.Log($"Energy: {energy}");
        Debug.Log($"Mood: {mood}");
        Debug.Log($"Alive: {alive}");
        Debug.Log($"Target: {(foodTarget == null ? "NULL" : foodTarget.transform.name)}");
        Debug.Log($"Destination: {(destination == Globals.UNSET_DESTINATION ? "NULL" : Vector3.Distance(transform.position,destination))}");
        Debug.Log($"InRange: {inRange}");
    }

    Vector3 GrowChild(int years) {
        Vector3 newScale = realScale;

        // total difference
        Vector3 scaleDiff = wc.adultPrefab.transform.localScale - wc.childPrefab.transform.localScale;
        float ageDiff = Globals.ADULT_STARTING_AGE - Globals.CHILD_STARTING_AGE;

        newScale += scaleDiff/ageDiff;

        return newScale;
    }


    Vector3 realScale = Vector3.one;
    public void Age(int years) {
        if (aged || !alive) { return; }

        // Children grow a little every year up to 20
        if (IsChild()) {
            // TODO: grow code    
            transform.localScale = GrowChild(age);
            realScale = transform.localScale;
        }

        // Elderly die
        if (age >= Globals.LIFE_EXPECTANCY) {
            Die();
        }

        // Debug.Log(age);
        age += years - lastReactedYear;
        // Debug.Log($"{years} - {lastReactedYear} += {age}");
        // Debug.Log(age);
        lastReactedYear = years;
    }

    public bool IsChild() {
        return age <= 20;
    }

    void Update() {
        // LogDebugStats();
        if (!alive) { return; }
        
        // Move towards current target
        switch (mood) {
            case "honry":
                if (isMating) { 
                    // MoveTowardsTarget(walkTarget);
                    break; 
                }

                if (partnerTarget == null) {
                    // mood = "BB";
                    mood = "neutral";
                    bb = true;
                    return;
                }

                MoveTowardsTarget(partnerTarget.transform);
                break;
            case "hungry":
                if (foodTarget == null) { break; }
                MoveTowardsTarget(foodTarget.transform);
                break;

            case "homesick":
                if (houseTarget == null || isHome) { break; }
                MoveTowardsTarget(houseTarget.transform);
                break;

            case "neutral":
                if (walkTarget == Globals.UNSET_DESTINATION || isMating) { break; }
                MoveTowardsTarget(walkTarget);

                // If a guy is frustrated they will proceed with neutral movement
                // but continue to check for a partner every frame
                // Changing mood here will ensure it is processed in Evaluate
                // and either a partner will be found and interacted with 
                // or not and this check will run again
                if (bb) { mood = "honry"; }
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

    // Update guy's mood and objectives
    void Evaluate() {
        // Change mood based on state and environment
        if (energy > 50 && mood == "hungry") {
            mood = "neutral";
        } else if (energy <= 50) {
            mood = "hungry";
        }

        if (energy > 75 && mood == "neutral") {
            bool isChild = IsChild();
            if (!refractory && !isChild) { // && children.Count < Globals.MAX_CHILD_COUNT) {
                mood = "honry";
            }
            if (refractory || isChild) {
                mood = "homesick";
            }
        }

        // Act on mood
        switch (mood) {
            case "honry":
                // Find closest compatible guy and be friends :)
                if (partnerTarget == null) {
                    if (partnerFindCooldown) { break; }
                    StartCoroutine(FindPartnerCooldown());
                    partnerFindCooldown = true;
                    partnerTarget = FindPartner();
                } else if (inRange && !refractory) {
                    // Debug.Log("Mating partner");
                    MatePartner(partnerTarget.GetComponent<Guy>());
                    partnerTarget = null;
                } else if (partnerTarget != null && partnerTarget.GetComponent<Guy>().IsMating) {
                    // Debug.Log("Potential partner is already mating");
                    // mood = "neutral";
                    partnerTarget = null;
                }
                break;

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

            case "homesick":
                if (houseTarget == null) {
                    if (houseFindCooldown) { break; }
                    StartCoroutine(FindHouseCooldown());
                    houseFindCooldown = true;
                    houseTarget = FindHouse();
                    if (houseTarget == null) { mood = "neutral"; }
                } else if (!hasHouse && houseTarget != null && !houseTarget.GetComponent<House>().Vacant) {
                    houseTarget = null;
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

    // A guy's autonomous body functions
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

    bool SendPartnerRequest(Guy from) {
        if (isMating || refractory) {
            // Debug.Log($"{transform.name}: {from.transform.name} wants but targetting {partnerTarget.transform.name}");
            return false; 
        }

        if (partnerTarget == null) {
            partnerTarget = from.gameObject;
            return true;
        }

        return false;
    }

    public void ResetMood() {
        mood = "neutral";
    }

        // Vector3 FindFood() {
    // TODO: REFACTOR THIS FUCKING MESS
    // (GameObject,float)[] foundFoods = null; // cache of foods THIS IS NT EVEN GONNA HELP
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
            if (f.Item2 > minDistance) { continue; }

            minDistance = f.Item2;
            newTarget = f.Item1.gameObject;
            // break;
        }

        return newTarget;
    }

    GameObject FindHouse() {
        GameObject newTarget = null;
        if (hasHouse) { return houseTarget; }
        float minDistance = float.PositiveInfinity;
        float currDistance;

        // List<(Guy,float)> distances = wc.GetHouseDistances(this);

        // SHITTY METHOD

        foreach (GameObject house in wc.Houses) {
            if (house == null) { continue; }

            currDistance = Vector3.Distance(house.transform.position,transform.position);
            if (currDistance > rangeOfVision) { continue; }
            if (currDistance > minDistance) { continue; }
            if (!house.GetComponent<House>().Vacant) { continue; }

            minDistance = currDistance;
            newTarget = house;
            // break;
        }

        return newTarget;
    }

    // Vector3 FindPartner() {
    GameObject FindPartner() {
        GameObject newTarget = null;
        float minDistance = float.PositiveInfinity;
        float currDistance;

        List<(Guy,float)> distances = wc.GetPartnerDistances(this);
        if (distances == null) {
            return newTarget;
        }
        // distances = distances.OrderBy(t => t.Item2); // sort by distance
        distances.Sort((x,y) => x.Item2.CompareTo(y.Item2)); // sort by distance
        foreach ((Guy,float) p in distances) {
            if (p.Item1 == null) { 
                // Debug.Log("NULL FROM WC 1");
                continue; 
            }
            if (p.Item1.Mood != "honry") { continue; }
            if (!GeneSequence.AreParentsCompatible(p.Item1.genes,genes,partnerCompatabilityRange)) {
                // Debug.Log("NOT COMPATIBLE");
                continue;
            }
            if (p.Item2 > rangeOfVision) { continue; }
            if (p.Item2 > minDistance) { continue; }

            minDistance = p.Item2;
            newTarget = p.Item1.gameObject;
            // break;
        }

        // if (newTarget == null) { Debug.Log("NO VALID PARTNER FOUND"); }
        return newTarget;

        // Vector3 newDest = Globals.UNSET_DESTINATION;

        // GameObject newTarget = null;

        // float minDistance = float.PositiveInfinity;
        // float currDistance;
        // Guy currGuy;
        // if (wc.Guys == null) { return null; }
        // foreach (GameObject guy in wc.Guys) {
        //     if (guy == gameObject) { continue; } // skip self
        //     if (Vector3.Distance(guy.transform.position,transform.position) > rangeOfVision) { continue; }
        //     currGuy = guy.GetComponent<Guy>();
        //     if (guy == null || !currGuy.Alive) { continue; } // skip destroyed or dead guys
        //     if (currGuy.Mood != "honry") { continue; } // skip uinterested guys


        //     // Ping target, if no keep looking
        //     if (currGuy.SendPartnerRequest(this)) {
        //         newTarget = guy;
        //     }


        //     // currDistance = Vector3.Distance(transform.position,guy.transform.position);
        //     // if (currDistance < minDistance) {
        //     //     minDistance = currDistance;
        //     //     newDest = guy.transform.position;
        //     //     newTarget = guy;
        //     // }
        // }

        // return newTarget;
        // foodTarget = newTarget;

        // return newDest;
    }

    // Used to decide the mother/father between identical partners
    bool FlipCoin() {
        // remote caller disables local guy's abililty to flip
        // thus ensuring a method to distinguish guy's behaviour
        // and avoid same-answer conflicts with mating
        canFlip = false;
        return UnityEngine.Random.Range(1,2) == 1;
    }

    void MatePartner(Guy partner) {
        // if (partner.IsMating) {
        //     // doResetTarget = true;
        //     return;
        // }

        // isMating = true;
        // matePosition = "father";
        // if (partner.MatePosition == "father") {
        //     matePosition = "mother";
        // }

        // Debug.Log($"{transform.name} - {partner.transform.name}");

        if (isMating) { // cannot mate while already mating
            return;
        }

        // FlipCoin unsets the canFlip flag on the partner
        // so that only one call is ever made and we can
        // decide who the mother/father are

        if (canFlip) { // flipper's result
            matePosition = partner. FlipCoin() ? "father" : "mother";
        } else { // receiver picks the opposites
            matePosition = partner.MatePosition == "father" ? "mother" : "father";
        }

        isMating = true;
        if (!matingRoutineRunning) {
            StartCoroutine(MatingRoutine(partner));
            matingRoutineRunning = true;
        }
    }


    // This is very basic, might add a pregnancy period before
    // birth in future or something. Also proper gene transmittance
    
    void ConceiveChild(Guy mother, Guy father) {
        if (!alive) { return; } // Don't give birth if mother has died
        GameObject childGO =  Instantiate(wc.childPrefab, mother.transform.position, Quaternion.identity);
        childGO.transform.name = wc.Guys.Length.ToString();
        Guy child = childGO.GetComponent<Guy>();
        child.genes = GeneSequence.GenerateSequenceFromParents(child,mother.genes,father.genes); // Generate sequence
        child.RefreshGeneTraits(); // Flush default traits to reflect new genes
        children.Add(child);
    }


    IEnumerator MatingRoutine(Guy partner) {
        refractory = true;
        yield return new WaitForSeconds(matingCooldownPeriod);

        if (matePosition == "mother") {
            ConceiveChild(this,partner);
        }

        // Reset that junk
        StartCoroutine(RefractoryReset());
        isMating = false;
        mood = "neutral";
        partnerTarget = null;
        inRange = false;
    }

    IEnumerator RefractoryReset() {
        yield return new WaitForSeconds(refractoryCooldownPeriod);
        matePosition = "";
        canFlip = true;
        refractory = false;
        matingRoutineRunning = false;
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

    IEnumerator FindPartnerCooldown() {
        yield return new WaitForSeconds(partnerFindCooldownPeriod);
        partnerFindCooldown = false;
    }

    IEnumerator FindHouseCooldown() {
        yield return new WaitForSeconds(houseFindCooldownPeriod);
        houseFindCooldown = false;
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

    // bool mousedOver = false;
    void OnMouseOver() {
        // Check for mouse click
        if (Input.GetMouseButtonDown(0)) {
            GameObject.Find("Canvas").SendMessage("ClickEntity", transform);
        }
    }

    // void OnMouseExit() {
    //     Debug.Log($"MOUSE LEFT: {transform.name}");
    //     mousedOver = false;
    // }
}
