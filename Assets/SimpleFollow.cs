using UnityEngine;

public class SimpleFollow : MonoBehaviour
{
    public Transform target;
    public bool debugMode = true;

    void Start()
    {
        if (target == null)
        {
            Debug.LogError("Target è null! Assegna il veicolo come target.");
            return;
        }

        Debug.Log("SimpleFollow inizializzato. Target: " + target.name);

        // Posiziona immediatamente la camera sulla posizione X del target
        Vector3 newPos = transform.position;
        newPos.x = target.position.x;
        transform.position = newPos;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // Posizione attuale della camera
        Vector3 currentPos = transform.position;

        // Aggiorna solo la coordinata X
        Vector3 newPos = new Vector3(target.position.x, currentPos.y, currentPos.z);

        // Applica la nuova posizione
        transform.position = newPos;

        if (debugMode)
        {
            Debug.Log("Camera: " + currentPos.x.ToString("F2") + " → " +
                      newPos.x.ToString("F2") + ", Target: " + target.position.x.ToString("F2"));
        }
    }
}