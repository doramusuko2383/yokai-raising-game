using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Yokai;

public class TutorialManager : MonoBehaviour
{
    const string PlayerPrefsKey = "TutorialStep";
    const string CanvasName = "TutorialCanvas";
    const string TextName = "TutorialText";

    static TutorialManager instance;

    [SerializeField]
    Text tutorialText;

    [SerializeField]
    CanvasGroup canvasGroup;

    TutorialStep currentStep = TutorialStep.None;
    TutorialStep displayedStep = TutorialStep.None;

    KegareManager kegareManager;
    YokaiStateController stateController;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        if (FindObjectOfType<TutorialManager>() != null)
            return;

        var managerObject = new GameObject("TutorialManager");
        managerObject.AddComponent<TutorialManager>();
        DontDestroyOnLoad(managerObject);
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadStep();
        EnsureUI();
        BindDependencies();
        UpdateDisplay(force: true);
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        UnbindKegareManager();
    }

    void Update()
    {
        UpdateDisplay(force: false);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BindDependencies();
        UpdateDisplay(force: true);
    }

    void BindDependencies()
    {
        var foundKegareManager = FindObjectOfType<KegareManager>();
        if (foundKegareManager != kegareManager)
        {
            UnbindKegareManager();
            kegareManager = foundKegareManager;
            if (kegareManager != null)
                kegareManager.KegareChanged += OnKegareChanged;
        }

        if (stateController == null)
            stateController = FindObjectOfType<YokaiStateController>();
    }

    void UnbindKegareManager()
    {
        if (kegareManager != null)
            kegareManager.KegareChanged -= OnKegareChanged;
    }

    void OnKegareChanged(float current, float max)
    {
        if (currentStep == TutorialStep.KegareNotice && current > 0f)
            CompleteIfCurrent(TutorialStep.KegareNotice);
    }

    void EnsureUI()
    {
        if (tutorialText != null && canvasGroup != null)
            return;

        GameObject canvasObject = GameObject.Find(CanvasName);
        if (canvasObject == null)
        {
            canvasObject = new GameObject(CanvasName);
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            canvasObject.AddComponent<CanvasScaler>();
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        canvasGroup = canvasObject.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = canvasObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Transform textTransform = canvasObject.transform.Find(TextName);
        if (textTransform == null)
        {
            var textObject = new GameObject(TextName);
            textObject.transform.SetParent(canvasObject.transform, false);
            textTransform = textObject.transform;
        }

        tutorialText = textTransform.GetComponent<Text>();
        if (tutorialText == null)
            tutorialText = textTransform.gameObject.AddComponent<Text>();

        var rectTransform = tutorialText.rectTransform;
        rectTransform.anchorMin = new Vector2(0.1f, 0.8f);
        rectTransform.anchorMax = new Vector2(0.9f, 0.98f);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;

        tutorialText.alignment = TextAnchor.MiddleCenter;
        tutorialText.fontSize = 32;
        tutorialText.color = Color.white;
        tutorialText.horizontalOverflow = HorizontalWrapMode.Wrap;
        tutorialText.verticalOverflow = VerticalWrapMode.Overflow;
        tutorialText.raycastTarget = false;

        if (tutorialText.font == null)
            tutorialText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");

        var outline = tutorialText.GetComponent<Outline>();
        if (outline == null)
            outline = tutorialText.gameObject.AddComponent<Outline>();

        outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
    }

    void LoadStep()
    {
        if (PlayerPrefs.HasKey(PlayerPrefsKey))
            currentStep = (TutorialStep)PlayerPrefs.GetInt(PlayerPrefsKey);
        else
            currentStep = TutorialStep.Dango;

        if (currentStep < TutorialStep.Dango || currentStep > TutorialStep.Completed)
            currentStep = TutorialStep.Dango;
    }

    void SaveStep()
    {
        PlayerPrefs.SetInt(PlayerPrefsKey, (int)currentStep);
        PlayerPrefs.Save();
    }

    void UpdateDisplay(bool force)
    {
        if (currentStep == TutorialStep.Completed)
        {
            Hide();
            return;
        }

        TutorialStep stepToShow = DetermineDisplayStep();
        if (stepToShow == TutorialStep.None)
        {
            Hide();
            return;
        }

        if (force || displayedStep != stepToShow)
            Show(stepToShow);
    }

    TutorialStep DetermineDisplayStep()
    {
        switch (currentStep)
        {
            case TutorialStep.Dango:
                return TutorialStep.Dango;
            case TutorialStep.KegareNotice:
                return TutorialStep.KegareNotice;
            case TutorialStep.Purify:
                return TutorialStep.Purify;
            case TutorialStep.Evolution:
                if (stateController != null && stateController.currentState == YokaiState.EvolutionReady)
                    return TutorialStep.Evolution;
                return TutorialStep.None;
            default:
                return TutorialStep.None;
        }
    }

    void Show(TutorialStep step)
    {
        EnsureUI();
        displayedStep = step;
        if (tutorialText != null)
            tutorialText.text = GetStepMessage(step);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

    }

    void Hide()
    {
        if (displayedStep == TutorialStep.None)
            return;

        displayedStep = TutorialStep.None;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    string GetStepMessage(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.Dango:
                return "だんごをあげてみよう！";
            case TutorialStep.KegareNotice:
                return "放っておくと穢れがたまるよ。ゲージを見てね！";
            case TutorialStep.Purify:
                return "おきよめして穢れを減らそう！";
            case TutorialStep.Evolution:
                return "進化できるよ！ヨウカイをタップ！";
            default:
                return string.Empty;
        }
    }

    void CompleteIfCurrent(TutorialStep step)
    {
        if (currentStep != step)
            return;

        currentStep = GetNextStep(step);
        SaveStep();
        UpdateDisplay(force: true);
    }

    TutorialStep GetNextStep(TutorialStep step)
    {
        switch (step)
        {
            case TutorialStep.Dango:
                return TutorialStep.KegareNotice;
            case TutorialStep.KegareNotice:
                return TutorialStep.Purify;
            case TutorialStep.Purify:
                return TutorialStep.Evolution;
            case TutorialStep.Evolution:
                return TutorialStep.Completed;
            default:
                return TutorialStep.Completed;
        }
    }

    public static void NotifyDangoUsed()
    {
        instance?.CompleteIfCurrent(TutorialStep.Dango);
    }

    public static void NotifyPurifyUsed()
    {
        instance?.CompleteIfCurrent(TutorialStep.Purify);
    }

    public static void NotifyEvolutionStarted()
    {
        instance?.CompleteIfCurrent(TutorialStep.Evolution);
    }
}
