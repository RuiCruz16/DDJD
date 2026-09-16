using UnityEngine;

public class DebrisMovement : MonoBehaviour
{
    [Header("Movement")]
    public float speed = 1f;           
    private Vector3 moveDirection;

    [Header("Avoidance")]
    public float detectionRadius = 0.5f;
    public float detectionDistance = 1.5f;
    public float turnSpeed = 5f;

    [Header("Floating")]
    public float waterLevel = 0f;      
    public float floatAmplitude = 0.02f; 
    public float floatSpeed = 2f;      

    [Header("Rotation")]
    public float minSpinSpeed = 5f;    
    public float maxSpinSpeed = 20f;   
    private float actualSpinSpeed;     

    [Header("Optimization")]
    public float lifeTime = 80f; 
    private float currentLifeTime = 0f;

    private float randomOffset;

    public void SetMoveDirection(Vector3 newDir)
    {
        moveDirection = newDir;
    }

    void Start()
    {
        randomOffset = Random.Range(0f, 100f);
        actualSpinSpeed = Random.Range(minSpinSpeed, maxSpinSpeed);
        if (Random.value > 0.5f) actualSpinSpeed = -actualSpinSpeed;
    }

    void Update()
    {
        if (transform.parent != null) return;

        currentLifeTime += Time.deltaTime;
        if (currentLifeTime >= lifeTime)
        {
            Destroy(gameObject);
            return; 
        }

        if (moveDirection != Vector3.zero)
        {
            RaycastHit hit;
            if (Physics.SphereCast(transform.position, detectionRadius, moveDirection, out hit, detectionDistance))
            {
                if (hit.collider.CompareTag("Raft") || hit.collider.CompareTag("Barricade"))
                {
                    if (hit.collider.GetComponentInParent<CollectionNet>() == null)
                    {
                        Vector3 flatNormal = hit.normal;
                        flatNormal.y = 0;
                        flatNormal.Normalize();

                        Vector3 slideDirection = Vector3.ProjectOnPlane(moveDirection, flatNormal).normalized;

                        if (slideDirection.sqrMagnitude < 0.01f)
                        {
                            slideDirection = Vector3.Cross(Vector3.up, moveDirection).normalized;
                        }

                        moveDirection = Vector3.Lerp(moveDirection, slideDirection, Time.deltaTime * turnSpeed).normalized;
                    }
                }
            }

            transform.Translate(speed * Time.deltaTime * moveDirection, Space.World);
        }

        float newY = waterLevel + Mathf.Sin(Time.time * floatSpeed + randomOffset) * floatAmplitude;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        transform.Rotate(actualSpinSpeed * Time.deltaTime * Vector3.up, Space.World);
    }
}
