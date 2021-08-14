using GestureRecognizer;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VisualizeResult : MonoBehaviour
{
    public AnimationCurve animationCurve;
    public float Duration = 2.0f;

    public GameObject LabelAnchor = null;
    public GameObject LookAtObject = null;

    [Header("Result Events")]
    [SerializeField] private GestureRecognizerEventSO m_gestureMatcherRestultEvent = default;

    private GameObject m_label = null;


    // Start is called before the first frame update
    void Start()
    {
        m_gestureMatcherRestultEvent.OnLoadingRequested += OnResultReported;

        // Create label
        m_label = new GameObject("Label");
        m_label.transform.parent = gameObject.transform;
        m_label.transform.localPosition = Vector3.zero;

        var canvas = m_label.AddComponent<Canvas>();

        var tempGO = new GameObject("Text");
        tempGO.transform.parent = m_label.transform;

        Vector3 pos = Vector3.zero;
        pos.y = 0.05f;
        tempGO.transform.localPosition = pos;
        tempGO.transform.localRotation = Quaternion.identity;

        var tmp = tempGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "";
        tmp.fontSize = 0.03f;
        tmp.color = Color.white;
        tmp.outlineColor = Color.black;
        tmp.outlineWidth = 0.2f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;

        Vector2 rect = new Vector2();
        rect.x = 0.2f;
        rect.y = 0.03f;
        m_label.GetComponent<RectTransform>().sizeDelta = rect;
        tempGO.GetComponent<RectTransform>().sizeDelta = rect;

        m_label.SetActive(false);


    }

    private void OnDestroy()
    {
        m_gestureMatcherRestultEvent.OnLoadingRequested -= OnResultReported;
    }


    private void OnResultReported(GestureMatcherResult result)
    {
        if (result.LikeliHood > 0.7f && result.RatioWeight > 0.3f && result.Weight > 0.01)
        {
            var tmp = m_label.GetComponentInChildren<TextMeshProUGUI>();
            tmp.text = result.Gesture.AssignedGesture.Name;
            tmp.color = result.Gesture.GestureColor;

            m_label.transform.position =  LabelAnchor != null ? LabelAnchor.transform.position : result.Gesture.transform.position;

            // Set rotations
            if (LookAtObject != null)
            {
                m_label.transform.rotation = Quaternion.LookRotation(m_label.transform.position - LookAtObject.transform.position);
            }

            StartCoroutine(AnimateText(1.0f, 5.0f));
        }
    }


    private IEnumerator AnimateText(float startScale, float endScale)
    {
        m_label.SetActive(true);

        float journey = 0f;
        var tmp = m_label.GetComponentInChildren<TextMeshProUGUI>();
        Color c = tmp.color;
        while (journey <= Duration)
        {
            journey += Time.deltaTime;
            float percent = Mathf.Clamp01(journey / Duration);
            float curvePercent = animationCurve.Evaluate(percent);
            Vector3 scale = Vector3.one * Mathf.Lerp(startScale, endScale, curvePercent);
            m_label.transform.localScale = scale;
            c.a = 1.0f - curvePercent;
            tmp.color = c;

            yield return null;
        }

        m_label.SetActive(false);
    }



}
