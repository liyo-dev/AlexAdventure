using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerWaterFloat : MonoBehaviour
{
    [Header("Configuración de Flotación")]
    [SerializeField] private float buoyancyForce = 15f; // obsoleto (no usado directamente), mantenido por compatibilidad
    [SerializeField] private float waterDrag = 2f;
    [SerializeField] private float waterAngularDrag = 5f;
    [SerializeField] private float maxSubmergedDepth = 2f;
    [SerializeField] private LayerMask waterLayerMask = -1;
    [Tooltip("Multiplicador de flotación respecto al peso (1 = igual al peso, >1 flota más rápido)")]
    [SerializeField, Min(0f)] private float buoyancyMultiplier = 1.25f;
    [Tooltip("Límite de velocidad vertical descendente dentro del agua")]
    [SerializeField, Min(0f)] private float maxDownwardSpeed = 6f;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Rigidbody rb;
    private Collider playerCollider;
    private float originalDrag;
    private float originalAngularDrag;
    private bool isInWater = false;
    private float waterSurfaceY;
    private Collider waterCollider;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        playerCollider = GetComponent<Collider>();
        originalDrag = rb.linearDamping;
        originalAngularDrag = rb.angularDamping;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        // Verificar si es agua usando el layer mask
        if (IsWaterLayer(other.gameObject.layer))
        {
            EnterWater(other);
        }
    }
    
    private void OnTriggerStay(Collider other)
    {
        // Actualizar la referencia/superficie si seguimos en el agua
        if (IsWaterLayer(other.gameObject.layer))
        {
            if (!isInWater)
                EnterWater(other);
            else if (waterCollider == other)
                waterSurfaceY = other.bounds.max.y;
        }
    }
    
    private void FixedUpdate()
    {
        if (isInWater && waterCollider)
        {
            ApplyBuoyancy();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsWaterLayer(other.gameObject.layer) && waterCollider == other)
        {
            ExitWater();
        }
    }
    
    private bool IsWaterLayer(int layer)
    {
        return (waterLayerMask.value & (1 << layer)) != 0;
    }
    
    private void EnterWater(Collider waterCol)
    {
        if (isInWater) return;
        
        isInWater = true;
        waterCollider = waterCol;
        waterSurfaceY = waterCol.bounds.max.y;
        
        // Cambiar propiedades físicas para simular resistencia del agua
        rb.linearDamping = waterDrag;
        rb.angularDamping = waterAngularDrag;
        
        if (showDebugInfo)
            Debug.Log($"[PlayerWaterFloat] Jugador entró al agua. Superficie Y: {waterSurfaceY}");
    }
    
    private void ExitWater()
    {
        if (!isInWater) return;
        
        isInWater = false;
        waterCollider = null;
        
        // Restaurar propiedades físicas originales
        rb.linearDamping = originalDrag;
        rb.angularDamping = originalAngularDrag;
        
        if (showDebugInfo)
            Debug.Log("[PlayerWaterFloat] Jugador salió del agua");
    }
    
    private void ApplyBuoyancy()
    {
        if (!rb || !isInWater) return;
        
        // Calcular qué tan sumergido está el jugador (desde el fondo del collider)
        float halfHeight = playerCollider ? playerCollider.bounds.extents.y : 0.5f;
        float playerBottom = (playerCollider ? playerCollider.bounds.center.y : transform.position.y) - halfHeight;
        float submersionDepth = waterSurfaceY - playerBottom;
        
        // Solo aplicar flotación si está parcial o totalmente sumergido
        if (submersionDepth > 0f)
        {
            // Calcular factor de flotación (0-1 basado en profundidad)
            float submersionFactor = Mathf.Clamp01(submersionDepth / maxSubmergedDepth);
            
            // Fuerza de flotación: contrarresta el peso proporcionalmente a la inmersión
            float gravity = Physics.gravity.magnitude;
            float upForce = rb.mass * gravity * buoyancyMultiplier * submersionFactor;
            rb.AddForce(Vector3.up * upForce, ForceMode.Force); // NO multiplicar por dt
            
            // Limitar velocidad vertical descendente dentro del agua
            if (rb.linearVelocity.y < -maxDownwardSpeed)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, -maxDownwardSpeed, rb.linearVelocity.z);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerWaterFloat] Profundidad: {submersionDepth:F2}, Factor: {submersionFactor:F2}, UpF: {upForce:F1}");
            }
        }
    }
    
    private void OnDrawGizmosSelected()
    {
        if (!isInWater) return;
        
        // Dibujar superficie del agua
        Gizmos.color = Color.cyan;
        Vector3 center = transform.position;
        center.y = waterSurfaceY;
        Gizmos.DrawWireCube(center, new Vector3(4f, 0.1f, 4f));
        
        // Dibujar línea de profundidad máxima
        Gizmos.color = Color.red;
        center.y = waterSurfaceY - maxSubmergedDepth;
        Gizmos.DrawWireCube(center, new Vector3(4f, 0.1f, 4f));
    }
}
