using System.Collections.Generic;
using KnightsAndGM.Shared;
using UnityEngine;

public class TravelHud : MonoBehaviour
{
    public static TravelHud Instance { get; private set; }

    private readonly List<string> logEntries = new List<string>();

    private void Awake()
    {
        Instance = this;
    }

    public void Append(TravelPhaseResult result)
    {
        if (result == null)
        {
            return;
        }

        foreach (var entry in result.LogEntries)
        {
            logEntries.Insert(0, entry);
        }

        if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
        {
            logEntries.Insert(0, result.ErrorMessage);
        }

        while (logEntries.Count > 8)
        {
            logEntries.RemoveAt(logEntries.Count - 1);
        }
    }

    private void OnGUI()
    {
        if (PartyCursorController.Instance == null || PartyCursorController.Instance.cursor == null)
        {
            return;
        }

        var cursor = PartyCursorController.Instance.cursor;
        GUILayout.BeginArea(new Rect(16f, 16f, 340f, 280f), GUI.skin.box);
        GUILayout.Label($"Travel Method: {cursor.SelectedTravelMethod}");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Trek")) cursor.SetTravelMethod(TravelMethod.Trek);
        if (GUILayout.Button("Gallop")) cursor.SetTravelMethod(TravelMethod.Gallop);
        if (GUILayout.Button("Cruise")) cursor.SetTravelMethod(TravelMethod.Cruise);
        GUILayout.EndHorizontal();

        cursor.TravelAtNight = GUILayout.Toggle(cursor.TravelAtNight, "Travel at night");
        cursor.CampOutdoors = GUILayout.Toggle(cursor.CampOutdoors, "Camp outdoors");
        cursor.SleptIndoors = GUILayout.Toggle(cursor.SleptIndoors, "Slept indoors");
        cursor.IsWinter = GUILayout.Toggle(cursor.IsWinter, "Winter conditions");
        cursor.TravelingBlind = GUILayout.Toggle(cursor.TravelingBlind, "Travelling blind");

        GUILayout.Space(8f);
        GUILayout.Label("Travel log");
        foreach (var entry in logEntries)
        {
            GUILayout.Label(entry);
        }

        GUILayout.EndArea();
    }
}
