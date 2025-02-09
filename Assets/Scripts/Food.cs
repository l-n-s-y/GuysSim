using System.Collections;
using UnityEngine;

public class Food : MonoBehaviour
{
    WorldController wc;
    UIController uic;

    bool beingEaten = false;
    public bool BeingEaten {
        get { return beingEaten; }
    }

    IEnumerator WaitToDecay() {
        float cnt = 0;
        float period = Globals.FOOD_DECAY_RATE/wc.TimeScale;
        yield return new WaitForSeconds(period);
        if (wc.TimeScale == 0) { // if game is paused, just keep trying to decay until unpaused
            StartCoroutine(WaitToDecay());
        } else {
            Destroy(gameObject);
        }
    }

    void Start() {
        wc = GameObject.Find("WorldController").GetComponent<WorldController>();
        uic = GameObject.Find("Canvas").GetComponent<UIController>();
        StartCoroutine(WaitToDecay());
    }

    void Update() {
        Vector3 zoom = uic.CameraZoom * new Vector3(0.1f,0.1f,0.1f);
        zoom = Globals.Vector3Clamp(zoom, Globals.MIN_ZOOM_SCALE, Globals.MAX_ZOOM_SCALE);
        transform.localScale = zoom;
    }

    public bool EatMe() {
        if (beingEaten) { return false; }
        beingEaten = true;
        return true;
    }
}
