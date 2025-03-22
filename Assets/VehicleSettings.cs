using UnityEngine;
using System;

[Serializable]
public class VehicleSettings
{
    [Header("Engine")]
    [Tooltip("Forza del motore per l'accelerazione")]
    public float motorForce = 1500f;

    [Tooltip("Forza di frenata")]
    public float brakeForce = 1000f;

    [Tooltip("Coppia di rotazione in aria")]
    public float airRotationTorque = 150f;

    [Tooltip("Moltiplicatore della coppia di rotazione a terra")]
    [Range(0.1f, 1f)]
    public float groundRotationTorqueMultiplier = 0.2f;

    [Header("Suspension")]
    [Tooltip("Rigidità delle sospensioni (più alto = più rigide)")]
    [Range(0.1f, 10f)]
    public float suspensionStiffness = 5f;

    [Tooltip("Smorzamento delle sospensioni (più alto = meno rimbalzo)")]
    [Range(0.1f, 5f)]
    public float suspensionDamping = 1f;
}