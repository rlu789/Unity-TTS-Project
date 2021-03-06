﻿using UnityEngine;
using System.Collections.Generic;

//TODO: make this work for ally AI
public class AIHelper : MonoBehaviour {

    public static AIHelper Instance;

    List<PossibleAction> possibleActions = new List<PossibleAction>();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("AIHelper already exists!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AIGetTurn(Unit unit)    //finds a target to move towards and attack (need to add supporting allies)
    {
        possibleActions.Clear();    //clear previous units actions

        List<Unit> possibleTargets = new List<Unit>();
        int maxActionRange = 0;
        int maxActionAndMoveRange = 0;
        UnitAction maxAction = new UnitAction();

        maxAction = GetMaxRangeAction(unit, ref maxActionRange);
        maxActionAndMoveRange = maxActionRange + unit.stats.moveSpeed;   //the full distance the unit could move + its max attack range;

        possibleTargets = GetPossibleTargets(unit, maxActionAndMoveRange);  //get the targets 

        if (maxAction == null) Debug.Log("No actions available!");
        else foreach (Unit target in possibleTargets)
        {
            List<Node> pathList = new List<Node>();
            Path<Node> path = NodeManager.Instance.CheckPath(unit.currentNode, target.currentNode, unit);    //find the closest node to the target we can get to
            if (path == null)
            {
                pathList.Add(unit.currentNode);
            }
            else pathList = unit.GetValidPath(path.ToList());

            List<Node> nodesInRange = maxAction.GetNodesInRange(pathList[pathList.Count - 1]);  //is this enemy in range from the closest we can get
            if (nodesInRange.Contains(target.currentNode)) AssignActions(unit, target, pathList); //get possible actions for this path
        }

        HehIGuessItsTimeIMadeMyChoice(unit);
    }

    List<Unit> GetPossibleTargets(Unit unit, int range)
    {
        List<Unit> possibleTargets = new List<Unit>();
        foreach (GameObject enemyGO in Map.Instance.teamZero)
        {
            Unit enemy = enemyGO.GetComponent<Unit>();

            if (Pathfindingv2.EstimateXY(unit.currentNode, enemy.currentNode) > range) continue; //out of range, skip this target

            possibleTargets.Add(enemy); //enemy within max possible range, going to pathfind towards it
        }

        foreach (GameObject allyGO in Map.Instance.teamOne) //get allies (for healing)
        {
            Unit ally = allyGO.GetComponent<Unit>();

            if (Pathfindingv2.EstimateXY(unit.currentNode, ally.currentNode) > range) continue;

            possibleTargets.Add(ally);
        }
        return possibleTargets;
    }

    void AssignActions(Unit unit, Unit target, List<Node> path)
    {
        PossibleAction act;
        //List<Node> nodesInAOE;

        for (int i = 0; i < unit.cards.selectedActions.Count; ++i)
        {
            if (unit.cards.selectedActions[i].range == 0)
            {
                act = new PossibleAction(path, unit.cards.selectedActions[i], unit.currentNode, unit, 0);
                act.DetermineFitness();
                if (act.fitness > 0) possibleActions.Add(act);
                continue;
            }

            List<Node> nodesInRange = unit.cards.selectedActions[i].GetNodesInRange(path[path.Count - 1]);
            if (!nodesInRange.Contains(target.currentNode)) continue;

            act = new PossibleAction(path, unit.cards.selectedActions[i], target.currentNode, unit, 0);
            act.DetermineFitness();
            if (act.fitness > 0) possibleActions.Add(act);

            /*
            if (unit.cards.selectedActions[i].aoe > 0)  //if action has an AOE, consider each node that could hit this target
            {
                nodesInAOE = unit.cards.selectedActions[i].GetNodesInRange(target.currentNode, true);
                foreach (Node n in nodesInAOE)
                {
                    if (!nodesInRange.Contains(n)) continue;

                    act = new PossibleAction(path, unit.cards.selectedActions[i], n, 0);
                    act.DetermineFitness();
                    possibleActions.Add(act);
                }
            }
            //The above does check the nodes around each target so it works but its really slow because of all the fitness checking that takes place for every node.
            //TODO: Should save the fitness of a node/action combo to save processing
            */
        }
    }

    public void ConfirmBestAction(Unit unit)    //Call on enemy action turn
    {
        if (unit.targetActionNode != null && unit.targetActionNode.currentUnit != null) return;  //if we have a valid target, we can continue with the action we planned

        possibleActions.Clear();

        int maxActionRange = 0;
        List<Unit> possibleTargets = new List<Unit>();
        List<Node> start = new List<Node>();
        start.Add(unit.currentNode);

        GetMaxRangeAction(unit, ref maxActionRange);

        possibleTargets = GetPossibleTargets(unit, maxActionRange);

        foreach (Unit target in possibleTargets)
        {
            AssignActions(unit, target, start);
        }

        HehIGuessItsTimeIMadeMyChoice(unit, false);
    }

    void HehIGuessItsTimeIMadeMyChoice(Unit unit, bool move = true)
    {
        List<PossibleAction> trueActions = new List<PossibleAction>();

        possibleActions.Shuffle(new System.Random()); //shuffle list so we don't bias certain nodes when choosing equal fitness targets
        int trimCount = GetMinActionCount(unit);
        possibleActions = TrimActionList(trimCount, unit);
        int highestFitness = 0;

        foreach (PossibleAction pAct in possibleActions)
            if (pAct.fitness > highestFitness) highestFitness = pAct.fitness;

        foreach (PossibleAction pAct in possibleActions)
        {
            if (pAct.fitness <= highestFitness / 4) continue;   //skip actions that are far less optimal than the best action
            for (int i = 0; i < pAct.fitness; ++i) trueActions.Add(pAct); //add based on fitness 
        }

        int index = 0;

        if ((possibleActions.Count == 0 || trueActions.Count == 0) && move) //just move towards a random enemy (or ally)
        {
            if (!unit.stats.hugFriends && Map.Instance.teamZero.Count != 0 && unit.team != 0)
            {
                index = Random.Range(0, Map.Instance.teamZero.Count);
                NodeManager.Instance.AssignPath(unit.currentNode, Map.Instance.teamZero[index].GetComponent<Unit>().currentNode);
            }
            else if (!unit.stats.hugFriends && Map.Instance.teamOne.Count != 0 && unit.team == 0)
            {
                index = Random.Range(0, Map.Instance.teamOne.Count);
                NodeManager.Instance.AssignPath(unit.currentNode, Map.Instance.teamOne[index].GetComponent<Unit>().currentNode);
            }
            else if (Map.Instance.teamOne.Count != 0 && unit.team != 0)
            {
                index = Random.Range(0, Map.Instance.teamOne.Count);
                NodeManager.Instance.AssignPath(unit.currentNode, Map.Instance.teamOne[index].GetComponent<Unit>().currentNode);
            }
            else if (Map.Instance.teamZero.Count != 0 && unit.team == 0)
            {
                index = Random.Range(0, Map.Instance.teamZero.Count);
                NodeManager.Instance.AssignPath(unit.currentNode, Map.Instance.teamZero[index].GetComponent<Unit>().currentNode);
            }
            return;
        }
        else if ((possibleActions.Count == 0 || trueActions.Count == 0) && !move)
        {
            return;
        }

        index = Random.Range(0, trueActions.Count);

        PossibleAction act = trueActions[index];

        //Get a node at the this actions max range if possible
        if (move && act.action.range != 0) FindBetterPath(ref act, unit);

        if (move) unit.SetUnitPath(act.path);
        unit.SetAction(act.action, act.target);
    }

    void FindBetterPath(ref PossibleAction act, Unit unit)
    {
        double currentRange = Pathfindingv2.EstimateXY(act.path[act.path.Count - 1], act.target);
        List<Node> newPath = null;
        bool updatingPath = true;

        while (currentRange < act.action.range && updatingPath)
        {
            bool validNodeFound = false;
            updatingPath = false;
            newPath = null;

            foreach (Node n in act.path[act.path.Count - 1].neighbours)
            {
                if (Pathfindingv2.Estimate(unit.currentNode, n) < unit.stats.moveSpeed)
                {
                    Path<Node> path = NodeManager.Instance.CheckPath(unit.currentNode, n, unit);
                    if (path != null)
                    {
                        newPath = unit.GetValidPath(path.ToList());

                        if (newPath[newPath.Count - 1].DistanceToEnemy() > act.path[act.path.Count - 1].DistanceToEnemy())
                        {
                            List<Node> nodesInRange = act.action.GetNodesInRange(newPath[newPath.Count - 1]);
                            if (nodesInRange.Contains(act.target))
                            {
                                validNodeFound = true;
                                break;
                            }
                        }
                    }
                }
            }
            if (validNodeFound)
            {
                act.path = newPath;
                updatingPath = true;
            }
        }
        //TODO: make this a function
    }

    List<PossibleAction> TrimActionList(int goal, Unit unit)
    {
        List<PossibleAction> possibleActionsTrimmed = new List<PossibleAction>();
        List<PossibleAction> currentActionList = new List<PossibleAction>();

        foreach (UnitAction act in unit.cards.selectedActions)
        {
            currentActionList = new List<PossibleAction>();

            foreach (PossibleAction pAct in possibleActions)
            {
                if (pAct.action == act) currentActionList.Add(pAct);
            }
            if (currentActionList.Count == 0) continue;

            currentActionList.Sort();   //sort by descending fitness (still random in the case of equal values since we randomised possible actions earlier)

            for (int i = 0; i < goal; ++i)
            {
                possibleActionsTrimmed.Add(currentActionList[i]);
            }
        }

        return possibleActionsTrimmed;
    }

    int GetMinActionCount(Unit unit)    //by balancing the action possibilities to the lowest count, we only value the fitness not how many possible targets we have available
    {
        int min = int.MaxValue;
        int count;
        foreach (UnitAction act in unit.cards.selectedActions)
        {
            count = GetActionCount(act);
            if (count != 0 && count < min) min = count;
            count = 0;
        }
        return min;
    }

    int GetActionCount(UnitAction action)   //how many times have we considered this action this round?
    {
        int count = 0;
        foreach (PossibleAction pAct in possibleActions)
        {
            if (action == pAct.action) count++;
        }
        return count;
    }

    UnitAction GetMaxRangeAction(Unit unit, ref int maxActionRange) //returns highest range action of unit, sets maxActionRange to that actions range
    {
        UnitAction ret = null;
        foreach (UnitAction act in unit.cards.selectedActions)    //get the max range of all units actions
        {
            if (act.range > maxActionRange)
            {
                maxActionRange = act.range;
                ret = act;
            }
        }
        return ret;
    }
}
