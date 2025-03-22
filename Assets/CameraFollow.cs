using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;             // Il veicolo da seguire
    public float smoothSpeed = 0.125f;   // Velocità di smoothing della camera (valore più basso = più fluido)
    public Vector3 offset = new Vector3(0, 0, -10);   // Offset della camera rispetto al target
    public bool followOnlyX = true;      // Segui solo l'asse X (per movimento laterale)
    public float lookAheadDistance = 3f; // Distanza di anticipazione nella direzione di movimento

    // Limiti opzionali della camera
    public bool useBounds = false;
    public float minX = -10f;
    public float maxX = 10f;
    public float minY = -10f;
    public float maxY = 10f;

    private Vector3 velocity = Vector3.zero;
    private Rigidbody2D targetRigidbody;

    void Start()
    {
        if (target != null)
        {
            targetRigidbody = target.GetComponent<Rigidbody2D>();
        }
        else
        {
            Debug.LogError("Camera Follow: Nessun target assegnato!");
        }
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        // Posizione di base del target
        Vector3 desiredPosition = target.position + offset;

        // Se il target ha un Rigidbody, anticipiamo la sua posizione in base alla velocità
        if (targetRigidbody != null && lookAheadDistance > 0)
        {
            Vector3 lookAheadPos = new Vector3(
                targetRigidbody.velocity.x * lookAheadDistance,
                followOnlyX ? 0 : targetRigidbody.velocity.y * lookAheadDistance,
                0
            );
            desiredPosition += lookAheadPos;
        }

        // Se vogliamo seguire solo l'asse X, manteniamo la Y e Z originali
        if (followOnlyX)
        {
            desiredPosition.y = transform.position.y;
        }

        // Applicare i limiti se abilitati
        if (useBounds)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minX, maxX);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minY, maxY);
        }

        // Movimento fluido verso la posizione desiderata
        Vector3 smoothedPosition = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref velocity,
            smoothSpeed
        );

        // Aggiorniamo la posizione della camera
        transform.position = smoothedPosition;
    }

    // Funzione per visualizzare i limiti nella finestra Scene di Unity
    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(maxX, minY, 0));
            Gizmos.DrawLine(new Vector3(maxX, minY, 0), new Vector3(maxX, maxY, 0));
            Gizmos.DrawLine(new Vector3(maxX, maxY, 0), new Vector3(minX, maxY, 0));
            Gizmos.DrawLine(new Vector3(minX, maxY, 0), new Vector3(minX, minY, 0));
        }
    }
}