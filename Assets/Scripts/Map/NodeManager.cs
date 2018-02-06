﻿using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class NodeManager : MonoBehaviour {

    public static NodeManager Instance;

    public GameObject movementUIObjectLine;
    public GameObject movementUIObjectTarget;
    public GameObject movementUIObjectTargetGO;
    public Material moveGood;
    public Material moveBad;
    [Space(10)]
    [Header("Don't change these v")]
    public Node selectedNode;

    public List<Unit> unitsWithAssignedPaths;

    public List<Node> nodesInRange = new List<Node>();
    public List<Node> nodesInAOE = new List<Node>();

    //BANDAID
    int selectingCount = 0;
    //

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("NodeManager already exists!");
            Destroy(gameObject);
            return;
        }
        Instance = this;

    }

    public void SetSelectedNode(Node node)
    {
        if (selectedNode != null)
            Deselect();
        Select(node);

    }

    public void SelectNode(Node node)
    {
        if (TurnHandler.Instance.currentState == TurnHandlerStates.ENEMYDRAW || TurnHandler.Instance.currentState == TurnHandlerStates.ENEMYTURN) return;
        if (node.currentUnit != null && PlayerInfo.Instance != null && node.currentUnit.ownerID != PlayerInfo.Instance.playerID) return;
        if (TurnHandler.Instance.currentState == TurnHandlerStates.PLAYERSELECT)
        {
            SelectPlayerSelect(node);
        }
        if (TurnHandler.Instance.currentState == TurnHandlerStates.PLAYERTURN)
        {
            SelectPlayerTurn(node);
        }
    }

    //TODO MAKE NEW CLASS FOR DIS ONE FUNCTION
    //BANDAID
    void TurnEndHandler()
    {
        selectedNode.currentUnit.unitStateMachine.state = States.B_SELECTING;
        if (selectingCount >= 1) //if the player has played two cards, goto next turn
        {
            //BANDAID
            selectingCount = -1;
            selectedNode.currentUnit.IEndMyEndTurnPegasus();
            TurnHandler.Instance.orderedActions.Remove(TurnHandler.Instance.orderedActions.Keys.First());
            UIHelper.Instance.ToggleVisible(UIType.UnitActions, false);
            TurnHandler.Instance.NextState();
        }
        selectingCount++;
    }

    public void TurnEndHandler(Unit unit)   //for other players units
    {
        unit.unitStateMachine.state = States.B_SELECTING;
        if (selectingCount >= 1) //if the player has played two cards, goto next turn
        {
            selectingCount = 0;
            unit.IEndMyEndTurnPegasus();
            TurnHandler.Instance.orderedActions.Remove(TurnHandler.Instance.orderedActions.Keys.First());
            TurnHandler.Instance.NextState();
            return;
        }
        selectingCount++;
    }

    void SelectPlayerTurn(Node node)
    {
        //assume theres a selectednode

        //if (selectedNode == node) return;
        if (selectedNode == node || selectedNode != node)
        {
            if (selectedNode.currentUnit.unitStateMachine.state == States.B_SELECTING) return; // player still picking cards
            if (selectedNode.currentUnit.unitStateMachine.state == States.B_SELECTINGMOVE) //player has selected a movement card
            {
                if (node.currentUnit != null)
                {
                    Debug.Log("Node has a unit! Unit: " + node.currentUnit);
                    return;
                }
                AssignPath(selectedNode, node);
                Unit theU = selectedNode.currentUnit;
                selectedNode.currentUnit.MoveUnit();
                SetSelectedNode(theU.currentNode);
                TurnEndHandler();
                return;
            }
            if (selectedNode.currentUnit.unitStateMachine.state == States.B_SELECTINGACTION) //player has selected an action card
            {
                if (nodesInRange.Contains(node))
                {
                    selectedNode.currentUnit.targetActionNode = node;
                    selectedNode.currentUnit.PerformAction();
                    // Add action to a queue of actions, clear nodes in range and arrow
                    //TurnHandler.Instance.actionQueue.Add(selectedNode.currentUnit);
                    Destroy(movementUIObjectTargetGO);
                    foreach (Node n in nodesInRange)
                    {
                        n.SetHexDefault();
                    }
                    nodesInRange.Clear();

                    TurnEndHandler();
                }
                else
                {
                    Debug.Log("out of range");
                    selectedNode.currentUnit.GetComponent<Unit>().targetActionNode = null;
                }
            }
        }
    }

    void SelectPlayerSelect(Node node)
    {
        if (selectedNode == null)   //selecting a node with no other nodes selected
        {
            if (node.currentUnit != null && node.currentUnit.isEnemy)   //cant select an enemy node without a reason
            {
                return;
            }
            if (node.currentUnit != null && node.currentUnit.unitStateMachine.state == States.WAIT)
            {
                Debug.Log("this unit already has cards selected");
                return; // cant select unit when its in preform state
            }
            Select(node);
            return;
        }
        if (selectedNode == node)   //selecting a node that is already selected
        {
            Deselect(true);
            return;
        }
        if (selectedNode != null)   //selecting a node with another node selected
        {
            if (selectedNode.currentUnitGO == null)
            {
                if (node.currentUnit != null && node.currentUnit.isEnemy)    //cant switch to an enemy node
                {
                    return;
                }
                Deselect();
                Select(node);
                return;
            }
        }
    }

    void Select(Node node)
    {
        //if (node.currentUnit != null && node.currentUnit.unitStateMachine.state == States.END) return; // Cannot select unit if its turn is over
        node.SetHexReady(false);
        node.SetHexSelected();
        selectedNode = node;

        if (node.currentUnit != null)
        {
            UIHelper.Instance.SetStatistics(node.currentUnit);
            UIHelper.Instance.SetUnitActions(node.currentUnit); //if (!node.currentUnit.isEnemy || TurnHandler.Instance.currentState != TurnHandlerStates.ENEMYTURN) 
            if (selectedNode.currentUnit.unitStateMachine.state == States.B_SELECTINGACTION)
            {
                if (selectedNode.currentUnitGO != null)
                {
                    ShowUnitActionRange(node);
                }
            }
        }
    }

    public void Deselect(bool hovering = false)
    {
        if (selectedNode == null) return;
        Destroy(movementUIObjectTargetGO);
        foreach (Node n in nodesInRange)
        {
            n.SetHexDefault();
        }
        nodesInRange.Clear();
        UIHelper.Instance.ToggleAllVisible(false);
        if (!hovering) selectedNode.SetHexDefault();
        else selectedNode.SetHexHighlighted(); //if you are still hovering over this node, return to hovering material
        PathHelper.Instance.DeleteCurrentPath();
        selectedNode = null;
    }

    public void AssignPath(Node init, Node dest)
    {
        Unit unit = init.currentUnit;
        if (unit == null) return;

        Path<Node> path = CheckPath(init, dest, unit);
        if (path == null)
            return;

        unit.SetUnitPath(path.ToList());
        PathHelper.Instance.DeleteCurrentPath();

        if (unitsWithAssignedPaths.Contains(unit))
            unitsWithAssignedPaths.Remove(unit);
        unitsWithAssignedPaths.Add(unit);
    }

    public void ShowPath(Node init, Node dest)
    {
        Unit unit = init.currentUnit;
        if (unit == null) return;
        Path<Node> path = CheckPath(init, dest, unit);
        if (path == null)
            return;
        PathHelper.Instance.DrawCurrentPath(path.ToList(), unit.stats.moveSpeed);
    }

    public Path<Node> CheckPath(Node init, Node dest, Unit unit)
    {
        Path<Node> path = null;
        List<Node> BLACKLISTNEVERENTERTHESENODESEVER = new List<Node>();
        do
        {
            if (path != null)
            {
                List<Node> pathList = unit.GetValidPath(path.ToList()); //if the path is not null, we got a path that ended on a bad hex. Get that final hex and add it to the blacklist
                BLACKLISTNEVERENTERTHESENODESEVER.Add(pathList[pathList.Count - 1]);
            }

            path = Pathfindingv2.FindPath(init, dest, BLACKLISTNEVERENTERTHESENODESEVER);
            if (path == null) return null;   //couldn't path there
        }
        while (!unit.IsPathValid(path.ToList()));
        return path;
    }

    public void UnassignUnitPath()
    {
        unitsWithAssignedPaths[unitsWithAssignedPaths.Count - 1].DeleteUnitPath();
        unitsWithAssignedPaths.RemoveAt(unitsWithAssignedPaths.Count - 1);
    }

    public void NodeHoverEnter(Node node)
    {
        if (selectedNode != node && !nodesInRange.Contains(node)) node.SetHexHighlighted();    //if node isnt selected and we arent range checking -> show hover material

        if (node.currentUnit != null)
        {
            UIHelper.Instance.SetStatistics(node.currentUnit);
        }

        if (selectedNode != null)
        {
            if (TurnHandler.Instance.currentState == TurnHandlerStates.PLAYERTURN && selectedNode.currentUnit.unitStateMachine.state == States.B_SELECTINGMOVE) ShowPath(selectedNode, node);    //show path if we are in the move turn

            if (selectedNode.currentUnit != null)
            {
                if (selectedNode.currentUnit.unitStateMachine.state == States.B_SELECTINGACTION)  //if we are in ACT state, move the targetting object to node
                {
                    movementUIObjectTargetGO.transform.position = node.transform.position;
                    selectedNode.currentUnitGO.transform.LookAt(new Vector3(node.transform.position.x, selectedNode.currentUnitGO.transform.position.y, node.transform.position.z));
                    // AOE HANDLING
                    if (selectedNode.currentUnit.readyAction.aoe > 0)
                    {
                        ShowUnitActionAOE(node);
                    }
                }
            }
        }
    }

    public void NodeHoverExit(Node node)
    {
        if (selectedNode != node && !nodesInRange.Contains(node)) node.SetHexDefault();

        if (selectedNode != null)
        {
            UIHelper.Instance.SetStatistics(selectedNode);  //set the windows back to the selected unit
            //if (selectedNode.currentUnit != UIHelper.Instance.GetCurrentActingUnit()) UIHelper.Instance.SetUnitActions(selectedNode);   //if the unit is the same as the acting one, dont reset its action window (because i made it get new range and so that makes it get new target which puts the target over the untis head and it still works but its not good looking anyway this whole thing needs some refactoring after some serious paint design docs :rage:
        }
        else UIHelper.Instance.ToggleAllVisible(false); //if there is no selected node, turn off the windows
    }

    public void ShowUnitActionRange(Node node)
    {
        if (selectedNode.currentUnit.unitStateMachine.state != States.B_SELECTINGACTION || selectedNode != node) return;   //only show range for selected units while in ACT turn

        if (movementUIObjectTargetGO != null)   //clean any previous AOE
        {
            Destroy(movementUIObjectTargetGO);
            foreach (Node n in nodesInRange)
            {
                n.SetHexDefault();
            }
        }

        nodesInRange = node.currentUnit.FindRange();
        foreach (Node n in nodesInRange)
        {
            n.SetHexSelectedBad();
        }
        movementUIObjectTargetGO = Instantiate(movementUIObjectTarget, node.transform.position, Quaternion.identity);
    }

    //Bandaid
    public void ShowUnitActionAOE(Node node)
    {
        if (selectedNode == null) return;
        if (nodesInAOE.Count > 0) // clear prev list of aoe nodes
        {
            foreach (Node n in nodesInAOE)
            {
                if (!nodesInRange.Contains(n)) n.SetHexDefault();
                else n.SetHexSelectedBad();
            }
            nodesInAOE.Clear();
        }
        // set new list of aoe nodes
        // code stolen from unitaction
        int _aoe = selectedNode.currentUnit.readyAction.aoe;

        List<Node> tempNodes = new List<Node>();
        nodesInAOE.Add(node); node.SetHexHighlighted();
        while (_aoe > 0)
        {
            foreach (Node n in nodesInAOE)
            {
                foreach (Node m in n.neighbours)
                    tempNodes.Add(m);
            }
            foreach (Node n in tempNodes)
            {
                if (!nodesInAOE.Contains(n))
                {
                    nodesInAOE.Add(n);
                    n.SetHexHighlighted();
                }
            }
            tempNodes.Clear();
            _aoe--;
        }
    }

    public void ClearActionAOE()
    {
        foreach (Node n in nodesInAOE)
        {
            n.SetHexDefault();
        }
        nodesInAOE.Clear();
    }
}

