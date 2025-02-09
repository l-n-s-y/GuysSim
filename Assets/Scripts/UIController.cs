using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UIController : MonoBehaviour
{
    WorldController wc;
    Guy singleTargetGuy = null;
    Dog singleTargetDog = null;

    TMP_Text fpsTx;
    TMP_Text worldTimeTx;
    TMP_Text yearsTx;
    TMP_Text guysAlive;

    Image avgColour;
    Image rColour;
    Image gColour;
    Image bColour;
    TMP_Text rText;
    TMP_Text gText;
    TMP_Text bText;

    [SerializeField]
    Slider timeScaleSlider;

    [SerializeField]
    Button resetButton;

    [SerializeField]
    Button pauseButton;
    [SerializeField]
    Sprite pauseSprite;
    [SerializeField]
    Sprite playSprite;
    bool paused = false;
    Image pauseButtonSprite;

    float cameraZoom = 1;
    public float CameraZoom {
        get { return cameraZoom; }
    }
    float scrollDampening = 10f;
    float cameraPanDampening = 12f;

    static float panelOffsetX = 30;
    static float panelOffsetY = 10;
    Vector2 panelOffset = (Vector2.right * panelOffsetX) + (Vector2.up * panelOffsetY);


    // public Transform statPanel;
    public Transform statPanel;
    public Transform singleTargetStatPanel;
    Vector2 statPanelBaseScale;
    // Image statPanelImg;
    RectTransform healthBarRectT;

    GameObject[] statPanels;

    Transform hbInd;
    Rect HBRect;
    TMP_Text nameTx;
    TMP_Text aliveTx;
    TMP_Text ageTx;
    TMP_Text moodTx;
    TMP_Text isMatingTx;
    TMP_Text matePositionTx;
    TMP_Text partnerTarget;
    TMP_Text foodTarget;

    // Transform statPanelInd;
    // Image statPanelIndImg;
    // RectTransform statPanelIndRectT;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        wc = GameObject.Find("WorldController").GetComponent<WorldController>();

        // Camera setup
        cameraZoom = Camera.main.orthographicSize;
        // cameraPanDampening /= (cameraZoom > 0 ? cameraZoom : 1);

        // HUD Setup
        fpsTx = transform.Find("fps").GetComponent<TMP_Text>();
        worldTimeTx = transform.Find("worldTime").GetComponent<TMP_Text>();
        yearsTx = transform.Find("years").GetComponent<TMP_Text>();
        guysAlive = transform.Find("guysAlive").GetComponent<TMP_Text>();

        // Setup time scale listener on timeScaleSlider
        timeScaleSlider.onValueChanged.AddListener(delegate { TimeScaleSliderUpdate(); });

        resetButton.onClick.AddListener(delegate { ResetButtonEvent(); });

        pauseButton.onClick.AddListener(delegate { PauseButtonToggle(); });
        pauseButtonSprite = pauseButton.transform.GetComponent<Image>();

        Transform avgCol = transform.Find("avgColour");
        avgColour = avgCol.GetChild(0).GetComponent<Image>();
        rColour = avgCol.GetChild(1).GetComponent<Image>();
        gColour = avgCol.GetChild(2).GetComponent<Image>();
        bColour = avgCol.GetChild(3).GetComponent<Image>();
        rText = avgCol.GetChild(4).GetComponent<TMP_Text>();
        gText = avgCol.GetChild(5).GetComponent<TMP_Text>();
        bText = avgCol.GetChild(6).GetComponent<TMP_Text>();

        singleTargetStatPanel = Instantiate(statPanel.gameObject,transform).transform;
        statPanelBaseScale = statPanel.transform.localScale;

        InitPanel(singleTargetStatPanel);

        // statPanelImg = statPanel.GetComponent<Image>();
        healthBarRectT = singleTargetStatPanel.transform.GetChild(0).GetComponent<RectTransform>();

        // statPanelInd = statPanel.GetChild(0);
        // statPanelIndImg = statPanelInd.GetComponent<Image>();
        // statPanelIndRectT = statPanelInd.GetComponent<RectTransform>();
    }

    public void PauseButtonToggle() {
        paused = !paused;
        if (paused) {
            pauseButtonSprite.sprite = playSprite;
            wc.UpdateTimeScale(0);
        } else {
            pauseButtonSprite.sprite = pauseSprite;
            wc.UpdateTimeScale(1);
        }
    }

    public void TimeScaleSliderUpdate() {
        wc.UpdateTimeScale(timeScaleSlider.value);
    }

    public void ResetButtonEvent() {
        wc.ResetScene();
    }

    void InitPanel(Transform panel) {
        hbInd = panel.Find("hunger_bar").GetChild(0);
        HBRect = panel.Find("hunger_bar").GetComponent<RectTransform>().rect;
        nameTx = panel.Find("name").GetComponent<TMP_Text>();
        aliveTx = panel.Find("alive").GetComponent<TMP_Text>();
        ageTx = panel.Find("age").GetComponent<TMP_Text>();
        moodTx = panel.Find("mood").GetComponent<TMP_Text>();
        isMatingTx = panel.Find("isMating").GetComponent<TMP_Text>();
        matePositionTx = panel.Find("matePosition").GetComponent<TMP_Text>();
        partnerTarget = panel.Find("partnerTarget").GetComponent<TMP_Text>();
        foodTarget = panel.Find("foodTarget").GetComponent<TMP_Text>();
    }

    bool clickedEntity = false;
    void ClickEntity(Transform target) {
        clickedEntity = true;
        Debug.Log($"Clicked: {target.name}");
        singleTargetGuy = target.GetComponent<Guy>();
        singleTargetDog = target.GetComponent<Dog>();
    }

    bool updateFps = true;
    float fpsUpdatePeriod = 1f;
    IEnumerator ResetUpdateFPS() {
        yield return new WaitForSeconds(fpsUpdatePeriod);
        updateFps = true;
    }

    bool panelsActive = false;
    bool panning = false;
    bool screenClickRunning = false;
    void Update()
    {

        // Update HUD

        if (updateFps) {
            fpsTx.text = $"FPS: {(int)(1.0f/Time.deltaTime)}";
            updateFps = false;
            StartCoroutine(ResetUpdateFPS());
        }
        worldTimeTx.text = $"TIME: {wc.WorldTime}";
        yearsTx.text = $"Years: {wc.Years}";
        guysAlive.text = $"Alive: {wc.Guys.Length}";

        Color average = GeneSequence.AverageColours(wc.GetGuyColours(wc.Guys));
        avgColour.color = average;
        rColour.color = new Color(average.r,0,0);
        gColour.color = new Color(0,average.g,0);
        bColour.color = new Color(0,0,average.b);
        rText.text = average.r.ToString();
        gText.text = average.g.ToString();
        bText.text = average.b.ToString();

        if (singleTargetGuy != null) {
            if (!singleTargetStatPanel.gameObject.activeSelf) { // reenable
                EnableStatPanel(singleTargetStatPanel);
            }
            UpdateStatPanel(singleTargetStatPanel, singleTargetGuy);
        } else if (singleTargetDog != null) {
            if (!singleTargetStatPanel.gameObject.activeSelf) { // reenable
                EnableStatPanel(singleTargetStatPanel);
            }
            UpdateStatPanel(singleTargetStatPanel, singleTargetDog);
        } else {
            DisableStatPanel(singleTargetStatPanel);
        }


        // Pan camera
        if (Input.GetMouseButtonDown(1)) {
            panning = true;
        }

        if (Input.GetMouseButtonUp(1)) {
            panning = false;
        }

        if (panning) {
            Vector3 newCamPos = Camera.main.transform.position;
            float adjustedPanning = cameraPanDampening / cameraZoom;
            // float adjustedPanning = 0.25f;
            // float adjustedPanning = cameraPanDampening;
            newCamPos.x -= Input.GetAxis("Mouse X")/adjustedPanning;
            newCamPos.y -= Input.GetAxis("Mouse Y")/adjustedPanning;
            Camera.main.transform.position = newCamPos;
        }

        float scrollIncrement = (-Input.mouseScrollDelta.y)*(cameraZoom/scrollDampening);
        cameraZoom += scrollIncrement;
        cameraZoom = Mathf.Max(cameraZoom,0);

        // Zoom camera
        if (cameraZoom > 0)
            Camera.main.orthographicSize = cameraZoom;

        // Offset UI
        panelOffset += new Vector2(scrollIncrement,scrollIncrement);
        // panelOffset = (Vector2)Globals.Vector3Clamp(panelOffset,Globals.MIN_ZOOM_SCALE,Globals.MAX_ZOOM_SCALE);



        // Target deselection
        
        if (Input.GetMouseButtonDown(0) && !screenClickRunning) {
            screenClickRunning = true;
            StartCoroutine(CheckScreenClick());
        }

        // if (!Input.GetKey(KeyCode.Space)) {
        //     if (panelsActive) {
        //         // foreach (GameObject panel in statPanels) {
        //         for (int i=0;i<statPanels.Length;i++) {
        //             DisableStatPanel(statPanels[i].transform);
        //         }
        //         panelsActive = false;
        //     }
        //     return;
        // }

        // // Update Guy stat panels
        // if (statPanels == null || wc.Guys.Length != statPanels.Length) {
        //     if (statPanels != null) { // if number of guys has updated
        //         foreach (GameObject panel in statPanels) { // cleanup old panels
        //             Destroy(panel);
        //         }
        //     }
        //     statPanels = new GameObject[wc.Guys.Length];
        // }

        // int c = 0;
        // foreach (GameObject g in wc.Guys) {
        //     if (statPanels[c] == null) {
        //         statPanels[c] = Instantiate(statPanel.gameObject,transform);
        //     }
        //     if (!statPanels[c].activeSelf) {
        //         EnableStatPanel(statPanels[c].transform);
        //         panelsActive = true;
        //     }

        //     Guy guy = g.GetComponent<Guy>();
        //     InitPanel(statPanels[c].transform);
        //     UpdateStatPanel(statPanels[c].transform,guy);

        //     c++;
        // }
    }

    IEnumerator CheckScreenClick() {
        yield return new WaitForSeconds(0.01f);
        if (!clickedEntity) {
            singleTargetGuy = null;
        }
        clickedEntity = false;
        screenClickRunning = false;
    }

    void DisableStatPanel(Transform panel) {
        panel.gameObject.SetActive(false);
    }

    void EnableStatPanel(Transform panel) {
        panel.gameObject.SetActive(true);
    }

    void UpdateStatPanel(Transform panel, Dog dog) {
        RectTransform hbIndRectT = hbInd.GetComponent<RectTransform>();

        Rect HBFillRect = hbIndRectT.rect;
        HBFillRect.height = (dog.Energy/Guy.MAX_ENERGY) * healthBarRectT.rect.height;
        hbIndRectT.sizeDelta = new Vector2(HBFillRect.width,HBFillRect.height);
        
        Vector2 pos = Camera.main.WorldToScreenPoint(dog.transform.position);
        pos += panelOffset;
        panel.position = pos;

        string partnerTargetText = dog.PartnerTarget == null ? "NULL" : dog.PartnerTarget.transform.name;
        string foodTargetText = dog.FoodTarget == null ? "NULL" : dog.FoodTarget.transform.name;

        nameTx.text = dog.transform.name;
        aliveTx.text = $"alive: {dog.Alive}";
        ageTx.text = $"age: {dog.age}";

        moodTx.text = $"mood: {dog.Mood}";
        isMatingTx.text = $"isMating: {dog.IsMating}";
        matePositionTx.text = $"matePosition: {(dog.MatePosition == "" ? "N/A" : dog.MatePosition)}";

        partnerTarget.text = $"partnerTarget: {partnerTargetText}";
        foodTarget.text = $"foodTarget: {foodTargetText}";
    }

    void UpdateStatPanel(Transform panel, Guy guy) {
        RectTransform hbIndRectT = hbInd.GetComponent<RectTransform>();

        Rect HBFillRect = hbIndRectT.rect;
        HBFillRect.height = (guy.Energy/Guy.MAX_ENERGY) * healthBarRectT.rect.height;
        hbIndRectT.sizeDelta = new Vector2(HBFillRect.width,HBFillRect.height);
        
        Vector2 pos = Camera.main.WorldToScreenPoint(guy.transform.position);
        pos += panelOffset;
        panel.position = pos;

        string partnerTargetText = guy.PartnerTarget == null ? "NULL" : guy.PartnerTarget.transform.name;
        string foodTargetText = guy.FoodTarget == null ? "NULL" : guy.FoodTarget.transform.name;

        nameTx.text = guy.transform.name;
        aliveTx.text = $"alive: {guy.Alive}";
        ageTx.text = $"age: {guy.age}";

        moodTx.text = $"mood: {guy.Mood}";
        isMatingTx.text = $"isMating: {guy.IsMating}";
        matePositionTx.text = $"matePosition: {(guy.MatePosition == "" ? "N/A" : guy.MatePosition)}";

        partnerTarget.text = $"partnerTarget: {partnerTargetText}";
        foodTarget.text = $"foodTarget: {foodTargetText}";
    }
}
