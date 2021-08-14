using GestureRecognizer;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Runtime.CompilerServices;
using System;

public enum LabelPos { Up, Down };

public class SheetController : MonoBehaviour
{
    public AnimationCurve animationCurve;
    public OVRInput.Controller ControllerType = OVRInput.Controller.Touch;
    public GameObject matchingObj;

    //public Vector3 gridOrigin;
    public float gridSpacing;
    public float gridCubeScale;
    public float lineThickness = 4; // mm

    private List<Gesture> gestures;
    private GameObject m_centerEyeGO = null;
    private GestureMatchingController matchingController;
    private GestureComposer gestureComposer;
    private List<GestureVisualizer> visualizers = new List<GestureVisualizer>();
    private Transform[] labTrans;
    private static Transform[] visTrans;
    private List<TextMeshPro> textLabels = new List<TextMeshPro>();
    private GameObject parentVis;
    private GameObject parentLabels;
    private GameObject parentGrid;
    private float MaxScale = 0.3f;

    // Start is called before the first frame update
    void Start()
    {
        matchingController = matchingObj.GetComponent<GestureMatchingController>();
        gestureComposer = matchingObj.GetComponent<GestureComposer>();

        m_centerEyeGO = GameObject.Find("CenterEyeAnchor");

        parentVis = new GameObject("ParentVis");
        parentVis.transform.parent = transform;

        parentLabels = new GameObject("ParentLabels");
        parentLabels.transform.parent = transform;

        parentGrid = new GameObject("ParentGrid");
        parentGrid.transform.parent = transform;

        PrepareGrid();
        SpawnGrid();
        gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        foreach (TextMeshPro textLabel in textLabels)
        {
            Vector3 forwardVec = textLabel.transform.position - m_centerEyeGO.transform.position;
            textLabel.transform.rotation = Quaternion.LookRotation(forwardVec);
        }
    }

    private void PrepareGrid()
    {
        gestures = gestureComposer.GestureSet.Gestures;
        for (int i = 0; i < gestures.Count; i++)
        {
            var go = new GameObject("Vis_" + gestures[i].Name);
            go.transform.parent = parentVis.transform;
            go.transform.position = Vector3.zero;
            go.transform.rotation = Quaternion.identity;

            var gvisualiser = go.AddComponent<GestureVisualizer>();
            gvisualiser.GestureColor = matchingController.GestureColors[i];
            gvisualiser.ShowLabels = false;
            gvisualiser.SetGesture(gestures[i]);
            gvisualiser.LineThickness = lineThickness;
            visualizers.Add(gvisualiser);

            var label = new GameObject("Label_" + gestures[i].Name);
            label.transform.parent = parentLabels.transform;
            label.transform.position = Vector3.zero;
            label.transform.rotation = Quaternion.identity;

            TextMeshPro textLabel = label.AddComponent<TextMeshPro>();
            textLabel.fontSize = 0.5f;
            textLabel.text = gestures[i].Name;
            textLabel.color = matchingController.GestureColors[i];
            textLabel.alpha = 1.0f;
            textLabel.alignment = TextAlignmentOptions.Center;
            textLabel.outlineColor = Color.black;
            textLabel.outlineWidth = 0.3f;
            textLabel.fontMaterial.SetFloat(ShaderUtilities.ID_FaceDilate, 0.7f);
            textLabel.UpdateMeshPadding(); // to force re-computation and prevent clipping

            textLabels.Add(textLabel);
        }
    }

    private void SpawnGrid()
    {
        int i = 0;
        visTrans = parentVis.gameObject.GetComponentsInChildren<Transform>();
        labTrans = parentLabels.gameObject.GetComponentsInChildren<Transform>();


        // Get Grid dimens 
        int len = gestures.Count;
        float sqrLen = Mathf.Sqrt(len);
        int xLen = (int)Mathf.Ceil(sqrLen);
        int yLen = (int)Mathf.Floor(sqrLen);

        Vector3 gridOrigin = new Vector3((float)-(xLen - 1)/2.0f * gridSpacing, (float)-(yLen - 1) / 2.0f * gridSpacing, -0.1f);
        gridOrigin.y += 0.2f;

        for (int y = yLen - 1; y >= 0; y--)
        {
            for (int x = 0; x < xLen; x++)
            {
                Vector3 spawnPos = new Vector3(x * gridSpacing, y * gridSpacing, 0) + gridOrigin;
                GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.localScale = new Vector3(gridCubeScale, gridCubeScale, gridCubeScale);
                cube.transform.parent = parentGrid.transform;
                cube.transform.localPosition = spawnPos;
                Color col = Color.white;
                col.a = 0.8f;
                cube.GetComponent<Renderer>().material.color = col;
                cube.GetComponent<Renderer>().material.shader = Shader.Find("Transparent/Diffuse");

                if (i >= len)
                    break;

                visTrans[i + 1].localPosition = spawnPos;

                spawnPos.z += -0.1f;
                labTrans[i + 1].localPosition = spawnPos;

                i++;
            }
        }
        gameObject.transform.localScale = Vector3.one * MaxScale;
    }

    public void ShowSheet()
    {
        gameObject.SetActive(true);
        StartCoroutine(AnimateSheet(0.0f , MaxScale, true));
    }

    public void HideSheet()
    {
        StartCoroutine(AnimateSheet(MaxScale, 0, false));
    }


    private IEnumerator AnimateSheet(float startScale, float endScale, Boolean boo)
    {
        float journey = 0f;
        float duration = 0.5f;
        while (journey <= duration)
        {
            journey += Time.deltaTime;
            float percent = Mathf.Clamp01(journey / duration);
            float curvePercent = animationCurve.Evaluate(percent);
            Vector3 scale = Vector3.one * Mathf.Lerp(startScale, endScale, curvePercent);
            transform.localScale = scale;
            yield return null;
        }
        gameObject.SetActive(boo);
    }

}
