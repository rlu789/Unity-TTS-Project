﻿using UnityEngine;
using System.Collections.Generic;

public class Unit : MonoBehaviour {

    [Header("Stats")]
    public int maxHealth = 100;
    int currentHealth;
    public int moveSpeed = 2;
    int currentMovement;
    public bool isEnemy;

    //Setup fields
    [Header("For unity and debug, don't change")]
    public Vector2Int XY = new Vector2Int(0, 0);
    public int currentNodeID = -1;
    public List<Node> currentPath = new List<Node>();
    public GameObject __testObject;
    //fresh fIelds
    List<Node> movePath = new List<Node>();
    int currMoveIndex = 0;
    public Node currentNode;

    bool pathHasChanged = false;
    List<GameObject> pathVisual = new List<GameObject>();

    GameObject[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,][,,,,,,,,,,,,,,,,,,,,,,,,,,,][,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,] loadBearingArray;

    private void Start()
    {
        currentHealth = maxHealth;
        currentMovement = moveSpeed;
    }

    void Update()
    {
        DrawPath();
        if (movePath.Count != 0)
        {
            MoveStep();
        }
    }

    void DrawPath()
    {
        if (currentPath != null && pathHasChanged)
        {
            foreach (GameObject objectIns in pathVisual)
            {
                if (objectIns != null) Destroy(objectIns);
            }

            pathHasChanged = false;

            pathVisual = new List<GameObject>();

            int currNode = 0;
            int movementRemaining = moveSpeed;
            while (currNode < currentPath.Count - 1)
            {
                //Vector3 start = new Vector3(currentPath[currNode].transform.position.x, 1f, currentPath[currNode].transform.position.z);
                //Vector3 end = new Vector3(currentPath[currNode + 1].transform.position.x, 1f, currentPath[currNode + 1].transform.position.z);

                movementRemaining -= currentPath[currNode + 1].moveCost;
                currNode++;

                DrawLines(movementRemaining, currNode - 1, NodeManager.Instance.movementUIObjectLine);
                //set direction for line
                Vector3 dir = currentPath[currNode - 1].transform.position - currentPath[currNode].transform.position;
                pathVisual[currNode - 1].transform.rotation = Quaternion.LookRotation(dir);
                //final node
                if (currNode == currentPath.Count - 1)
                {
                    DrawLines(movementRemaining, currNode, NodeManager.Instance.movementUIObjectTarget);
                }
            }
        }
    }

    void DrawLines(int movement, int current, GameObject GO)
    {
        if (movement >= 0)
        {
            pathVisual.Add(Instantiate(GO, currentPath[current].transform.position, Quaternion.identity));
        }
        else
        {
            pathVisual.Add(Instantiate(GO, currentPath[current].transform.position, Quaternion.identity));

            Renderer[] rends = pathVisual[current].GetComponentsInChildren<Renderer>();
            foreach (Renderer rendo in rends)
            {
                rendo.material = NodeManager.Instance.moveBad;
            }
        }
    }

    public List<Node> GetPath() //moves as far along the set path as its movement can go
    {
        int movementRemaining = moveSpeed;
        int currentNode = 0;
        List<Node> pathToFollow = new List<Node>();

        while (currentNode < currentPath.Count - 1 && movementRemaining > 0)
        {
            movementRemaining -= currentPath[currentNode+1].moveCost;    //reduce our remaning movement by the cost

            if (movementRemaining < 0) break;   //if you can't make it to the node, stop adding to the path

            currentNode++;
            pathToFollow.Add(currentPath[currentNode]);
        }
        currentPath.RemoveRange(0, currentNode);  //remove the path we are taking from the planned path

        movePath.AddRange(pathToFollow);    //adds the path to the path that we will animate through
        return pathToFollow;
    }

    void MoveStep()
    {
        Vector3 dir = movePath[currMoveIndex].transform.position - transform.position;      //sets our target direction to the next node along the path
        transform.Translate(dir.normalized * Time.deltaTime * (moveSpeed), Space.World);    //moves towards our target
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(dir), (moveSpeed * 2) * Time.deltaTime); //rotates in the direction we are going

        if ( Vector3.Distance(transform.position, movePath[currMoveIndex].transform.position) <= 0.1f)  //if we are close to the node, we can start moving towards the next node
        {
            GetNextStep();
        }
    }

    void GetNextStep()
    {
        currMoveIndex++;
        if (currMoveIndex == movePath.Count)
        {
            transform.position = movePath[currMoveIndex - 1].transform.position;    //make sure we are right on the node when we are finished
            movePath.RemoveRange(0, movePath.Count);                                //clear out the list of nodes to move to
            currMoveIndex = 0;                                                      //reset our move index when finished
            return;
        }
    }

    public void MoveUnit()    //moves unit on selected tile
    {
        if ((TurnHandler.Instance.currentState == TurnHandlerStates.PLAYERTURN && !isEnemy) || (TurnHandler.Instance.currentState == TurnHandlerStates.ENEMYTURN && isEnemy))
        {
            List<Node> pathToFollow = GetPath();   //get the path to follow, based on the max distance the unit can move this turn
            if (pathToFollow == null) return;
            if (pathToFollow.Count == 0) return;

            Node _destNode = pathToFollow[pathToFollow.Count - 1];  //the destination is the furthest node we can reach

            //set values on initial and destination nodes
            _destNode.currentUnitGO = gameObject;
            _destNode.currentUnit = this;
            _destNode.potientalUnit = null;
            Map.Instance.nodes[XY.x, XY.y].currentUnitGO = null;
            Map.Instance.nodes[XY.x, XY.y].currentUnit = null;

            //set units new node values
            Unit unitComponent = _destNode.currentUnitGO.GetComponent<Unit>();
            unitComponent.XY = _destNode.XY;
            unitComponent.currentNodeID = _destNode.nodeID;
            currentNode = _destNode;

            pathHasChanged = true;
            //NodeManager.Instance.Deselect();
            //Select(_destNode);
            //initNode = _destNode;
        }
    }

    public void SetUnitPath(List<Node> path)
    {
        pathHasChanged = true;
        currentPath = path;
    }

    public void TogglePathVisual(bool toggle)
    {
        if (pathVisual.Count == 0) return;

        foreach (GameObject GO in pathVisual)
        {
            GO.SetActive(toggle);
        }
    }
}