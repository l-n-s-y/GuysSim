using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class House : MonoBehaviour
{
    public static float DEFAULT_MIN_EXIT_TIMER = 2;
    public static float DEFAULT_MAX_EXIT_TIMER = 10;

    public static int DEFAULT_MAX_OCCUPANTS = 3;

    // On collision by Guy: if Guy.target == this && Guy.HouseId == this.id:
    //      Destroy Guy object and record struct
    //      Do house stuff
    //      Run calc to determine if Guy wants to leave
    //      If true, spawn new Guy object with previously recorded struct



    Dictionary<Guy,bool> occupants; // (guy, allowedToEnter)
    List<Guy> occupantsInHouse;
    WorldController wc;
    UIController uic;

    public int OccupantsCount {
        get { return occupants.Count; }
    }

    public int OccupantsInHouseCount {
        get { return occupantsInHouse.Count; }
    }

    public bool Vacant {
        get { return occupants.Count<DEFAULT_MAX_OCCUPANTS; }
    }

    float interactionDistance = 1f;
    float enterCooldownPeriod = 3f;


    float exitTimerMinPeriod = DEFAULT_MIN_EXIT_TIMER;
    float exitTimerMaxPeriod = DEFAULT_MAX_EXIT_TIMER;

    Vector3 realScale;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        wc = GameObject.Find("WorldController").GetComponent<WorldController>();
        uic = GameObject.Find("Canvas").GetComponent<UIController>();

        occupants = new Dictionary<Guy,bool>();
        occupantsInHouse = new List<Guy>();

        realScale = transform.localScale;
    }

    // Update is called once per frame
    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.Y)) {
        //     RespawnOccupant(occupantsInHouse[^1]);
        // }



        // if (Input.GetKeyDown(KeyCode.U)) {
        //     Debug.Log(occupants[^1].transform.name);
        // }

        SurveyNearbyGuys();

        // Guy[] guysInRange = GetGuysInRange();
        // // if (guysInRange.Length > 0) {
        // foreach (Guy guy in guysInRange) {
        //     // if (guy NOT IN occupants) { continue; }     <========= IMPLEMENT THIS
        //     if (occupants.ContainsKey(guy) && occupants[guy]) {
        //         OccupantEnter(guy);
        //         wc.UpdateGuys();
        //     } else if (!occupants.ContainsKey(guy)) {
        //         occupants.Add(guy,true);
        //     }
        // }

        transform.localScale = realScale * Mathf.Clamp((uic.CameraZoom * 0.1f),Globals.MIN_ZOOM_SCALE,Globals.MAX_ZOOM_SCALE);
    }

    void AddNewOccupant(Guy newOccupant) {
        occupants.Add(newOccupant,true);
    }

    bool IsGuyInOccupants(Guy target) {
        // Debug.Log(occupants.Keys.ToString());
        // return occupants.ContainsKey(target);
        foreach (var k in occupants) {
            if (target.Id == k.Key.Id) { return true; }
        }

        return false;
    }

    // Guy[] GetGuysInRange() {
    void SurveyNearbyGuys() {
        List<Guy> inRange = new List<Guy>();
        if (wc.Guys == null || wc.Guys.Length == 0) { return; } // inRange.ToArray(); }

        // TODO: this is a temporary solution so i dont have to implement this in WC
        Guy currGuy;
        foreach (GameObject guy in wc.Guys) {
            if (guy == null) { continue; }
            if (Vector3.Distance(guy.transform.position,transform.position) > interactionDistance) {
                // inRange.Add(guy.GetComponent<Guy>());
                continue;
            }

            currGuy = guy.GetComponent<Guy>();
            if (occupants.ContainsKey(currGuy) && occupants[currGuy] == true && occupants.Count < DEFAULT_MAX_OCCUPANTS) {

                Debug.Log($"{occupants.Count} - {occupantsInHouse.Count} / {DEFAULT_MAX_OCCUPANTS} WWW");
                OccupantEnter(currGuy);
                wc.UpdateGuys();
            // } else if (!occupants.ContainsKey(currGuy)) {
            } else if (!IsGuyInOccupants(currGuy) && !currGuy.HasHouse && occupants.Count < DEFAULT_MAX_OCCUPANTS && occupantsInHouse.Count < DEFAULT_MAX_OCCUPANTS) {
                // occupants.Add(currGuy,true);
                AddNewOccupant(currGuy);
            }
        }

        // occupantsInHouse = inRange.ToArray();

        // return inRange.ToArray();
    }

    void RespawnOccupant(Guy occupant) {
        // target.gameObject.SetActive(true);
        // Debug.Log(occupants[occupant]);
        // occupants[occupant] = false;
        // Debug.Log(occupants[occupant]);
        GameObject respawnedGO = Instantiate(wc.adultPrefab, transform.position, Quaternion.identity);
        Guy respawned = respawnedGO.GetComponent<Guy>();
        respawned.DuplicateGuy(occupant); // Copy across stats
        respawned.ResetMood();
        respawned.HasHouse = true;
        // occupants.Remove(occupant);
        // occupants.Add(respawned,false);
        occupants[respawned] = false;
        occupantsInHouse.Remove(occupant);
        StartCoroutine(OccupantEnterCooldown(respawned));

        // Guy guy = respawned.GetComponent<Guy>();
        // guy = new Guy(target);
        // Destroy(respawn.GetComponent<Guy>());
        // respawned.AddComponent<Guy>(new Guy(target));
        // Globals.CopyValues<Guy>(respawned.GetComponent<Guy>(),target); // Apply stored characteristics to blank template
        // Globals.CopyValues<GeneSequence>(respawned.GetComponent<Guy>().Genes,target.Genes);
    }

    void OccupantEnter(Guy occupant) {
        occupantsInHouse.Add(occupant);
        StartCoroutine(OccupantExitTimer(occupant));
        GameObject.Destroy(occupant.gameObject);

        // target.gameObject.SetActive(false);
    }

    IEnumerator OccupantExitTimer(Guy occupant) {
        yield return new WaitForSeconds(UnityEngine.Random.Range(exitTimerMinPeriod,exitTimerMaxPeriod));
        RespawnOccupant(occupant);
    }

    IEnumerator OccupantEnterCooldown(Guy occupant) {
        if (!occupants.ContainsKey(occupant)) {
            // guy might've been destroyed or otherwise removed from array so we can skip
            yield return null;
        }
        yield return new WaitForSeconds(enterCooldownPeriod);
        occupants[occupant] = true; // re-allow entrance to house
    }
}
