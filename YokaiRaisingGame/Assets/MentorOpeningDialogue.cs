using System.Collections;
using TMPro;
using UnityEngine;

public class MentorOpeningDialogue : MonoBehaviour
{
    [Header("表示")]
    [SerializeField] private TMP_Text dialogueText;

    [Header("デバッグ用フラグ")]
    [SerializeField] private bool showOpening = false;

    [Header("表示間隔")]
    [SerializeField] private float lineDisplayDuration = 3.5f;

    [Header("オープニング文言")]
    [SerializeField] private string[] openingLines =
    {
        "おやおや、よく来たのう。\nわしは おんみょうじいじゃ。",
        "わしは のう、\nおぬしの じいちゃんで、\n妖怪を みまもる 陰陽師なんじゃ。",
        "この子は、\nきちんと そだてんと\nモノノケに なってしまう。",
        "さあ、\nおぬしの てで\nだいじに みてやってくれ。",
    };

    Coroutine openingRoutine;

    void Start()
    {
        if (dialogueText == null)
        {
            return;
        }

        if (!showOpening)
        {
            dialogueText.text = string.Empty;
            return;
        }

        if (openingRoutine != null)
        {
            StopCoroutine(openingRoutine);
        }

        openingRoutine = StartCoroutine(PlayOpening());
    }

    IEnumerator PlayOpening()
    {
        foreach (string line in openingLines)
        {
            dialogueText.text = MentorSpeechFormatter.Format(line);
            yield return new WaitForSeconds(lineDisplayDuration);
        }
    }
}
