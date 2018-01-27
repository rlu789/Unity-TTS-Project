﻿using UnityEngine;

public class CameraController : MonoBehaviour
{
    public bool doMovement = true;

    public float panSpeed = 30f;
    public float panBorderThickness = 10f;

    public float scrollSpeed = 5f;
    public float minY = 10f;
    public float maxY = 80f;

    void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        Vector3 pos = transform.position;

        /* //perspective zoom
        pos.y -= scroll * 1000 * scrollSpeed * Time.deltaTime;
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        transform.position = pos;
        */

        //orthographic zoom
        if (scroll != 0)
        {
            if (scroll < 0) scroll = 1;
            else scroll = -1;
            scroll = Mathf.Clamp(GetComponent<Camera>().orthographicSize + scroll, 1, 10);

            GetComponent<Camera>().orthographicSize = scroll;
        }

        //if (Input.GetKeyDown(KeyCode.Backspace)) doMovement = !doMovement; //for disabling/enabling movement

        if (!doMovement) return;

        if (Input.GetKey("w")/* || Input.mousePosition.y >= Screen.height - panBorderThickness*/)
        {
            transform.Translate(Vector3.forward * panSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey("s")/* || Input.mousePosition.y <= panBorderThickness */)
        {
            transform.Translate(Vector3.back * panSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey("d")/* || Input.mousePosition.x >= Screen.width - panBorderThickness */)
        {
            transform.Translate(Vector3.right * panSpeed * Time.deltaTime, Space.World);
        }
        if (Input.GetKey("a")/* || Input.mousePosition.x <= panBorderThickness */)
        {
            transform.Translate(Vector3.left * panSpeed * Time.deltaTime, Space.World);
        }
        //these shouldnt be in camera, just temporary
        if (Input.GetKeyUp(KeyCode.Escape))
        {
            if (TurnHandler.Instance.currentState == TurnHandlerStates.PLAYERMOVE)
            {
                if (NodeManager.Instance.unitsWithAssignedPaths.Count > 0)
                {
                    NodeManager.Instance.UnassignUnitPath();
                }
            }
            else if (TurnHandler.Instance.currentState == TurnHandlerStates.PLAYERACT && NodeManager.Instance.selectedNode != null && NodeManager.Instance.selectedNode.currentUnit.unitStateMachine.state == States.ACT)
            {
                NodeManager.Instance.Deselect();
            }
            else if (TurnHandler.Instance.currentState == TurnHandlerStates.PLAYERACT && NodeManager.Instance.selectedNode != null && NodeManager.Instance.selectedNode.currentUnit.unitStateMachine.state == States.PERFORM)
            {
                NodeManager.Instance.selectedNode.currentUnit.unitStateMachine.state = States.ACT;
            }
            else if (TurnHandler.Instance.currentState == TurnHandlerStates.PLAYERACT && TurnHandler.Instance.actionQueue.Count > 0) // if in playeract and there are actions in action queue
            {
                Debug.Log("Removing action for unit: " + TurnHandler.Instance.actionQueue[TurnHandler.Instance.actionQueue.Count - 1]);
                TurnHandler.Instance.actionQueue[TurnHandler.Instance.actionQueue.Count - 1].unitStateMachine.state = States.ACT;
                TurnHandler.Instance.actionQueue.RemoveAt(TurnHandler.Instance.actionQueue.Count - 1);
            }
        }
        if (Input.GetKeyUp(KeyCode.R))
        {
            UIHelper.Instance.NextUnitAction();
        }
    }
}