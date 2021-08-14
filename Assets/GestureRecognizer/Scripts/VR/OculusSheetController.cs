using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OculusSheetController : MonoBehaviour
{
    public OVRInput.Controller ControllerType = OVRInput.Controller.LTouch;
    public GameObject Anchor = null;

    private SheetController sheetController = null;
    private bool sheetToggle = false;
    // Start is called before the first frame update
    void Start()
    {
        if (Anchor == null)
        {
            string handType = ControllerType == OVRInput.Controller.RTouch ? "RightHandAnchor" : "LeftHandAnchor";
            Anchor = GameObject.Find(handType);
        }
        else
        {
            ControllerType = OVRInput.Controller.None;
        }

        sheetController = GetComponentInChildren<SheetController>(true);

    }

    // Update is called once per frame
    void Update()
    {
        if (Anchor)
        {
            transform.position = Anchor.transform.position;
            transform.rotation = Anchor.transform.rotation;
        }

        if (OVRInput.GetDown(OVRInput.Button.One, ControllerType))
        {
            if (!sheetToggle)
            {
                sheetController.ShowSheet();
                sheetToggle = true;
            }
            else
            {
                sheetController.HideSheet();
                sheetToggle = false;
            }

        }

    }
}
