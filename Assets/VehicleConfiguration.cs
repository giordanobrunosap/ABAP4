// Salva questo come VehicleConfiguration.cs
using UnityEngine;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "VehicleConfig", menuName = "Vehicle/Configuration")]
public class VehicleConfiguration : ScriptableObject
{
    [Header("Engine")]
    [Tooltip("Forza del motore per l'accelerazione")]
    public float motorForce = 1500f;
    [Tooltip("Forza di frenata")]
    public float brakeForce = 1000f;
    [Tooltip("Coppia di rotazione in aria")]
    public float airRotationTorque = 150f;
    [Tooltip("Moltiplicatore della coppia di rotazione a terra")]
    [Range(0.1f, 1f)] public float groundRotationTorqueMultiplier = 0.2f;

    [Header("Suspension")]
    [Tooltip("Rigidità delle sospensioni (più alto = più rigide)")]
    [Range(0.1f, 10f)] public float suspensionStiffness = 5f;
    [Tooltip("Smorzamento delle sospensioni (più alto = meno rimbalzo)")]
    [Range(0.1f, 5f)] public float suspensionDamping = 1f;

    // Aggiungi altri parametri secondo necessità

    [Header("Presets")]
    public List<VehiclePreset> presets = new List<VehiclePreset>();

    [Serializable]
    public class VehiclePreset
    {
        public string presetName;
        public float motorForce;
        public float brakeForce;
        public float airRotationTorque;
        public float groundRotationTorqueMultiplier;
        public float suspensionStiffness;
        public float suspensionDamping;

        public void CopyFrom(VehicleConfiguration config)
        {
            motorForce = config.motorForce;
            brakeForce = config.brakeForce;
            airRotationTorque = config.airRotationTorque;
            groundRotationTorqueMultiplier = config.groundRotationTorqueMultiplier;
            suspensionStiffness = config.suspensionStiffness;
            suspensionDamping = config.suspensionDamping;
        }

        public void ApplyTo(VehicleConfiguration config)
        {
            config.motorForce = motorForce;
            config.brakeForce = brakeForce;
            config.airRotationTorque = airRotationTorque;
            config.groundRotationTorqueMultiplier = groundRotationTorqueMultiplier;
            config.suspensionStiffness = suspensionStiffness;
            config.suspensionDamping = suspensionDamping;
        }
    }

    public void SavePreset(string name)
    {
        VehiclePreset newPreset = new VehiclePreset();
        newPreset.presetName = name;
        newPreset.CopyFrom(this);

        // Sostituisci se esiste già un preset con lo stesso nome
        bool found = false;
        for (int i = 0; i < presets.Count; i++)
        {
            if (presets[i].presetName == name)
            {
                presets[i] = newPreset;
                found = true;
                break;
            }
        }

        if (!found)
            presets.Add(newPreset);
    }

    public void LoadPreset(string name)
    {
        foreach (var preset in presets)
        {
            if (preset.presetName == name)
            {
                preset.ApplyTo(this);
                break;
            }
        }
    }
}

#if UNITY_EDITOR
// Editor personalizzato per gestire i preset
[CustomEditor(typeof(VehicleConfiguration))]
public class VehicleConfigurationEditor : Editor
{
    private string newPresetName = "New Preset";
    private int selectedPresetIndex = -1;
    
    public override void OnInspectorGUI()
    {
        VehicleConfiguration config = (VehicleConfiguration)target;
        
        // Disegna l'inspector standard
        DrawDefaultInspector();
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Preset Manager", EditorStyles.boldLabel);
        
        // Salva preset
        EditorGUILayout.BeginHorizontal();
        newPresetName = EditorGUILayout.TextField("Preset Name", newPresetName);
        if (GUILayout.Button("Save Preset"))
        {
            config.SavePreset(newPresetName);
            EditorUtility.SetDirty(config);
        }
        EditorGUILayout.EndHorizontal();
        
        // Lista dei preset
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Available Presets:");
        
        if (config.presets.Count == 0)
        {
            EditorGUILayout.HelpBox("No presets saved yet.", MessageType.Info);
        }
        else
        {
            string[] presetNames = new string[config.presets.Count];
            for (int i = 0; i < config.presets.Count; i++)
            {
                presetNames[i] = config.presets[i].presetName;
            }
            
            selectedPresetIndex = EditorGUILayout.Popup("Select Preset", selectedPresetIndex, presetNames);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Load Selected") && selectedPresetIndex >= 0)
            {
                config.LoadPreset(presetNames[selectedPresetIndex]);
                EditorUtility.SetDirty(config);
            }
            
            if (GUILayout.Button("Delete Selected") && selectedPresetIndex >= 0)
            {
                config.presets.RemoveAt(selectedPresetIndex);
                selectedPresetIndex = -1;
                EditorUtility.SetDirty(config);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
#endif