#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

/// <summary>
/// Custom Inspector for FinancialRecoveryTester.
/// </summary>
[CustomEditor(typeof(FinancialRecoveryTester))]
public class FinancialRecoveryTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        FinancialRecoveryTester tester =
            (FinancialRecoveryTester)target;

        DrawDefaultInspector();

        EditorGUILayout.Space(12f);

        EditorGUILayout.LabelField(
            "FINANCIAL RECOVERY TESTS",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "Use this only during development. " +
            "Prepare a scenario first, then run recovery.",
            MessageType.Info
        );

        EditorGUILayout.Space(6f);

        if (GUILayout.Button(
                "1. PREPARE SCENARIO",
                GUILayout.Height(32f)))
        {
            tester.PrepareScenario();
        }

        if (GUILayout.Button(
                "2. RUN RECOVERY",
                GUILayout.Height(32f)))
        {
            tester.RunRecovery();
        }

        EditorGUILayout.Space(4f);

        if (GUILayout.Button(
                "MORTGAGE SELECTED PROPERTY"))
        {
            tester.MortgageSelectedProperty();
        }

        if (GUILayout.Button(
                "MANUAL ELIMINATION TEST"))
        {
            tester.EliminateCurrentDebtor();
        }

        EditorGUILayout.Space(4f);

        if (GUILayout.Button(
                "LOG CURRENT STATE"))
        {
            tester.LogCurrentState();
        }

        if (GUILayout.Button(
                "RESTORE TEST STATE"))
        {
            tester.RestoreTestState();
        }

        EditorGUILayout.Space(10f);

        EditorGUILayout.LabelField(
            "LAST RESULT",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            string.IsNullOrEmpty(tester.LastResult)
                ? "No test executed."
                : tester.LastResult,
            tester.TestRunning
                ? MessageType.Warning
                : MessageType.None
        );

        FinancialRecoveryManager recovery =
            tester.RecoveryManager;

        if (recovery != null)
        {
            EditorGUILayout.Space(5f);

            EditorGUILayout.LabelField(
                "Recovery Active",
                recovery.IsRecoveryActive.ToString()
            );

            if (recovery.IsRecoveryActive)
            {
                EditorGUILayout.LabelField(
                    "Debtor",
                    recovery.CurrentDebtor != null
                        ? recovery.CurrentDebtor.PlayerName
                        : "None"
                );

                EditorGUILayout.LabelField(
                    "Creditor",
                    recovery.CurrentCreditor != null
                        ? recovery.CurrentCreditor.PlayerName
                        : "BANK"
                );

                EditorGUILayout.LabelField(
                    "Remaining Debt",
                    $"${recovery.RemainingDebt:N0}M"
                );
            }
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(tester);
        }
    }
}

#endif
