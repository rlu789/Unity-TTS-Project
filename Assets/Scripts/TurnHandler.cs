﻿using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

//public enum TurnHandlerStates2
//{
//    PLAYERMOVE,
//    ENEMYMOVE,
//    PLAYERACT,
//    ENEMYACT,
//    BATTLEACT
//}

public enum TurnHandlerStates
{
    ENEMYDRAW,
    PLAYERDRAW,
    PLAYERSELECT,
    PLAYERTURN,
    ENEMYTURN,
    END
}

public class TurnHandler : MonoBehaviour
{
    public static TurnHandler Instance;
    public int unitTurnCount;
    public TurnHandlerStates currentState;
    public List<Unit> actionQueue = new List<Unit>();
    public SortedDictionary<float, Unit> orderedActions = new SortedDictionary<float, Unit>();
    
    float haveYetToCrossTheBridge = 0.01f;

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("TurnHandler already exists!");
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Setup()
    {
        Invoke("DelaySwitch", 1f);
    }

    void DelaySwitch()  //VERY bad code
    {
        SwitchState(TurnHandlerStates.ENEMYDRAW);
    }

    public void NextState()
    {
        switch (currentState)
        {
            case TurnHandlerStates.ENEMYDRAW:
                SwitchState(TurnHandlerStates.PLAYERDRAW);
                break;
            case TurnHandlerStates.PLAYERDRAW:
                SwitchState(TurnHandlerStates.PLAYERSELECT);
                break;
            case TurnHandlerStates.PLAYERSELECT:
                SwitchState(DetermineTurn());
                break;
            case TurnHandlerStates.PLAYERTURN:
                SwitchState(DetermineTurn());
                break;
            case TurnHandlerStates.ENEMYTURN:
                SwitchState(DetermineTurn());
                break;
            case TurnHandlerStates.END:
                SwitchState(TurnHandlerStates.ENEMYDRAW);
                break;
        }
    }

    TurnHandlerStates DetermineTurn()
    {
        if (orderedActions.Count == 0)
        {
            Debug.Log("Dgf");
            return TurnHandlerStates.END;
        }
        if (!orderedActions[orderedActions.Keys.First()].isEnemy)
        {
            orderedActions[orderedActions.Keys.First()].GetComponent<UnitStateMachine>().state = States.B_SELECTING;
            NodeManager.Instance.SetSelectedNode(orderedActions[orderedActions.Keys.First()].GetComponent<Unit>().currentNode);
            return TurnHandlerStates.PLAYERTURN;
        }
        else
        {
            orderedActions[orderedActions.Keys.First()].GetComponent<UnitStateMachine>().state = States.B_SELECTING;
            return TurnHandlerStates.ENEMYTURN;
        }
    }

    void SwitchState(TurnHandlerStates state)
    {
        UIHelper.Instance.SetTurnValues(state);
        switch (state)
        {
            case TurnHandlerStates.ENEMYDRAW:
                //function to have enemy draw their cards
                SetAllStates(States.START, States.DRAW);
                currentState = TurnHandlerStates.ENEMYDRAW;
                Debug.Log("heklo im in enemydarwa");
                //HandleStatus();
                NextState();
                break;
            case TurnHandlerStates.PLAYERDRAW:
                SetAllStates(States.DRAW, States.WAIT);
                currentState = TurnHandlerStates.PLAYERDRAW;
                Debug.Log("heklo im in player drawing cards");
                DrawCards();
                NextState();
                break;
            case TurnHandlerStates.PLAYERSELECT:
                SetAllStates(States.SELECT, States.WAIT);
                currentState = TurnHandlerStates.PLAYERSELECT;
                Debug.Log("heklo im in playe rsalaected");
                //defs not cheating lol
                LazyTestingCBFFunction();
                DetermineTurnOrder();
                NextState();
                break;
            case TurnHandlerStates.PLAYERTURN:
                //SetAllStates(States.END, States.ACT);
                currentState = TurnHandlerStates.PLAYERTURN;
                Debug.Log("heklo im in player turn");
                break;
            case TurnHandlerStates.ENEMYTURN:
                //SetAllStates(States.END, States.ACT);
                currentState = TurnHandlerStates.ENEMYTURN;
                Debug.Log("heklo im in enemyturn");
                //HandleEnemyAct();
                HandleEnemyTurn();
                break;
            case TurnHandlerStates.END:
                SetAllStates(States.END, States.END);
                currentState = TurnHandlerStates.END;
                Debug.Log("heklo im in end");
                break;
        }
        //UIHelper.Instance.SetTurnValues(currentState);
    }

    void SetAllStates(States playerState, States enemyState)
    {
        for (int i = 0; i < Map.Instance.unitDudeFriends.Count; i++)
        {
            Map.Instance.unitDudeFriends[i].GetComponent<UnitStateMachine>().SetState(playerState);
        }
        for (int i = 0; i < Map.Instance.unitDudeEnemies.Count; i++)
        {
            Map.Instance.unitDudeEnemies[i].GetComponent<UnitStateMachine>().SetState(enemyState);
        }
    }

    public void MoveButton()
    {
        for (int i = 0; i < Map.Instance.unitDudeFriends.Count; i++)
        {
            if (Map.Instance.unitDudeFriends[i].GetComponent<Unit>().isEnemy == false)
            {
                Map.Instance.unitDudeFriends[i].GetComponent<Unit>().MoveUnit();
            }
        }
        NextState();
        if (NodeManager.Instance.selectedNode != null)
            NodeManager.Instance.Deselect(true);
    }

    void DrawCards()
    {
        for (int i = 0; i < Map.Instance.unitDudeFriends.Count; i++)
        {
            if (Map.Instance.unitDudeFriends[i].GetComponent<Unit>().isEnemy == false)
            {
                Map.Instance.unitDudeFriends[i].GetComponent<Unit>().DrawCards(1);
            }
        }
    }

    public void PerformButton()
    {
        NextState();
    }

    void HandleEnemyTurn()
    {
        for (int i = 0; i < Map.Instance.unitDudeEnemies.Count; i++)
        {
            AIHelper.Instance.AIGetTurn(Map.Instance.unitDudeEnemies[i].GetComponent<Unit>());
            Map.Instance.unitDudeEnemies[i].GetComponent<Unit>().MoveUnit();
            if(Map.Instance.unitDudeEnemies.Count != i) NodeManager.Instance.SetSelectedNode(Map.Instance.unitDudeEnemies[i].GetComponent<Unit>().currentNode);
        }
    }

    void HandleEnemyAct()
    {
        for (int i = 0; i < Map.Instance.unitDudeEnemies.Count; i++)
        {
            Unit enemy = Map.Instance.unitDudeEnemies[i].GetComponent<Unit>();
            //AIHelper.Instance.ConfirmBestAction(enemy);
            if (enemy.readyAction != null && !enemy.readyAction.isEmpty()) actionQueue.Add(enemy);
        }
    }

    //IEnumerator BattleAct() //TODO: need to ignore units that were killed
    //{
    //    foreach(Unit u in actionQueue)
    //    {
    //        if (u.readyAction == null) continue;
    //        if (orderedActions.ContainsKey(u.readyAction.initiative))
    //        {
    //            orderedActions.Add(u.readyAction.initiative + haveYetToCrossTheBridge, u);
    //            haveYetToCrossTheBridge += 0.01f;
    //        }
    //        else
    //            orderedActions.Add(u.readyAction.initiative, u);
    //    }
    //    //TODO CROSS THIS BRIDGE WHEN WE GET THERE
    //    foreach (KeyValuePair<float, Unit> unit in orderedActions)
    //    {
    //        //Debug.Log("Key: " + unit.Key + ", Value: {1} " + unit.Value + " using action " + unit.Value.readyAction.name);
    //        if (unit.Value == null) continue;
    //        unit.Value.PerformAction();
    //        yield return new WaitForSeconds(1f);
    //    }

    //    haveYetToCrossTheBridge = 0.01f;
    //    actionQueue.Clear();
    //    orderedActions.Clear();
    //    NextState();
    //}

    void HandleStatus()
    {
        foreach (GameObject unit in Map.Instance.unitDudeFriends)
        {
            StatusHelper.Instance.CheckStatus(unit.GetComponent<Unit>());
        }
    }

    void DetermineTurnOrder()
    {
        for (int i = 0; i < Map.Instance.unitDudeFriends.Count; i++)
        {
            orderedActions.Add(i + 10, Map.Instance.unitDudeFriends[i].GetComponent<Unit>());
        }
        for (int i = 0; i < Map.Instance.unitDudeEnemies.Count; i++)
        {
            orderedActions.Add(i, Map.Instance.unitDudeEnemies[i].GetComponent<Unit>());
        }
        NodeManager.Instance.SetSelectedNode(Map.Instance.unitDudeEnemies[0].GetComponent<Unit>().currentNode);
    }

    public void yeap()
    {
        NodeManager.Instance.selectedNode.currentUnit.unitStateMachine.state = States.B_SELECTINGACTION;
        Node CLONEDNODEgetHAckedNode = NodeManager.Instance.selectedNode;
        NodeManager.Instance.SetSelectedNode(CLONEDNODEgetHAckedNode);
    }

    void LazyTestingCBFFunction()
    {
        for (int i = 0; i < Map.Instance.unitDudeFriends.Count; i++)
        {
            Map.Instance.unitDudeFriends[i].GetComponent<Unit>().selectedActions = Map.Instance.unitDudeFriends[i].GetComponent<Unit>().availableActions;
            Map.Instance.unitDudeFriends[i].GetComponent<Unit>().GetComponent<UnitStateMachine>().state = States.WAIT;
        }
        for (int i = 0; i < Map.Instance.unitDudeEnemies.Count; i++)
        {
            Map.Instance.unitDudeEnemies[i].GetComponent<Unit>().selectedActions = Map.Instance.unitDudeEnemies[i].GetComponent<Unit>().availableActions;
            Map.Instance.unitDudeEnemies[i].GetComponent<Unit>().GetComponent<UnitStateMachine>().state = States.WAIT;
        }
    }
}
