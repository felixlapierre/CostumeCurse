using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseSelect : MonoBehaviour
{
    public GameObject selectedObject;

    void Update()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        RaycastHit hitInfo;

        if (Physics.Raycast(ray, out hitInfo)) {
            GameObject hitObject = hitInfo.transform.root.gameObject;
        }
        else 
        {
            ClearSelection();
        }
    }

    void SelectTarget(GameObject obj)
    {
        if (selectedObject != null)
        {
            if (obj == selectedObject)
                return;

            ClearSelection();
        }
    }

    void ClearSelection()
    {
        selectedObject = null;
    }
}
