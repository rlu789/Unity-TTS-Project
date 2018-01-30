﻿using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using UnityEditor;

public class CardManager : MonoBehaviour
{
    public static CardManager Instance;
    public Dictionary<int, List<UnitAction>> decks = new Dictionary<int, List<UnitAction>>();

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

    // Use this for initialization
    void Start()
    {
        ReadTextFile("Assets/Scripts/ClassVERYSmart.txt", (int)Class.VERYSmart);
        ReadTextFile("Assets/Scripts/ClassDude.txt", (int)Class.Dude);
        ReadTextFile("Assets/Scripts/ClassHealthyBoy.txt", (int)Class.HealthyBoy);

    }

    void ReadTextFile(string file_path, int classNo)
    {
        List<UnitAction> cards = new List<UnitAction>();
        StreamReader inp_stm = new StreamReader(file_path);

        while (!inp_stm.EndOfStream)
        {
            string inp_ln = inp_stm.ReadLine();
            Debug.Log(inp_ln);
            cards.Add(HandleLine(inp_ln));
            // Do Something with the input. 
        }
        decks.Add(classNo, cards);
        inp_stm.Close();
    }

    UnitAction HandleLine(string line)
    {
        UnitAction action = new UnitAction();
        bool takingName = true, takingPrefab = false;
        string actionName = "", prefabName = "Assets/Prefabs/Projectiles/";

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i].Equals('-'))
            {
                takingName = false; takingPrefab = true;
                i++;
            }
            if (line[i].Equals(':')) takingPrefab = false;

            if (takingName)
            {
                actionName += line[i];
            }
            else if (takingPrefab)
            {
                prefabName += line[i];
            }
            else
            {
                action.name = actionName;
                Debug.Log(prefabName);
                action.projectile = (GameObject)AssetDatabase.LoadAssetAtPath(prefabName, typeof(GameObject));
                action.manaCost = (int)Char.GetNumericValue(line[i + 2]);
                action.healthCost = (int)Char.GetNumericValue(line[i + 4]);
                action.range = (int)Char.GetNumericValue(line[i + 6]);
                action.damage = (int)Char.GetNumericValue(line[i + 8]);
                action.aoe = (int)Char.GetNumericValue(line[i + 10]);
                action.cooldown = (int)Char.GetNumericValue(line[i + 12]);
                action.initiative = (int)Char.GetNumericValue(line[i + 14]);
                return action;
            }
        }
        return null;
    }
}