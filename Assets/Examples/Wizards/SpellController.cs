using GestureRecognizer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellController : MonoBehaviour
{
    public GameObject VRHeadGO = null;
    public GameObject Spell = null;

    [Header("Result Events")]
    [SerializeField] private GestureRecognizerEventSO m_gestureMatcherRestultEvent = default;

    // Start is called before the first frame update
    void Start()
    {
        m_gestureMatcherRestultEvent.OnLoadingRequested += OnSpellDetected;
    }

    private void OnDestroy()
    {
        m_gestureMatcherRestultEvent.OnLoadingRequested -= OnSpellDetected;
    }

    // Update is called once per frame
    void Update()
    {
        
    }


    private void OnSpellDetected(GestureMatcherResult result)
    {
        string spell = result.Gesture.AssignedGesture.Name;

        Color c = Color.white;

        if (spell == "IceAttack")
        {
            c = Color.blue;
        }
        if (spell == "FireAttack")
        {
            c = Color.red;
        }
        if (spell == "VoidAttack")
        {
            c = Color.black;
        }

        if (result.LikeliHood > 0.7f && result.RatioWeight > 0.3f && result.Weight > 0.01)
        {
            var go = GameObject.Instantiate(Spell);
            go.GetComponent<SpellEffect>().SpellColor = c;
            go.transform.position = VRHeadGO.transform.position;

            var dir = VRHeadGO.transform.TransformDirection(Vector3.forward);
            dir.y = 0;

            dir.Normalize();
            go.GetComponent<SpellEffect>().Direction = dir;
        }
    }

}
