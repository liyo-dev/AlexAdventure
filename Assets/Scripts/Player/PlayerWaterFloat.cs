using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerWaterFloat : MonoBehaviour
{
    [Header("Configuración de Flotación")]
    [SerializeField] private float buoyancyForce = 15f;
    [SerializeField] private float waterDrag = 2f;
    [SerializeField] private float waterAngularDrag = 5f;
    [SerializeField] private float maxSubmergedDepth = 2f;
    [SerializeField] private LayerMask waterLayerMask = -1;
    
    [Header("Debug")]
    [SerializeField] private bool showDebugInfo = false;
    
    private Rigidbody rb;
    private float originalDrag;
    private float originalAngularDrag;
    private bool isInWater = false;
    private float waterSurfaceY;
    private Collider waterCollider;
    
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
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
        if (IsWaterLayer(other.gameObject.layer) && isInWater)
        {
            ApplyBuoyancy(other);
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
    
    private void ApplyBuoyancy(Collider waterCol)
    {
        if (!rb || !isInWater) return;
        
        // Calcular qué tan sumergido está el jugador
        float playerBottom = transform.position.y - (GetComponent<Collider>()?.bounds.size.y * 0.5f ?? 1f);
        float submersionDepth = waterSurfaceY - playerBottom;
        
        // Solo aplicar flotación si está parcial o totalmente sumergido
        if (submersionDepth > 0f)
        {
            // Calcular factor de flotación (0-1 basado en profundidad)
            float submersionFactor = Mathf.Clamp01(submersionDepth / maxSubmergedDepth);
            
            // Aplicar fuerza de flotación hacia arriba
            Vector3 buoyancy = Vector3.up * buoyancyForce * submersionFactor * Time.fixedDeltaTime;
            rb.AddForce(buoyancy, ForceMode.Force);
            
            // Reducir velocidad vertical si está cayendo muy rápido
            if (rb.linearVelocity.y < -5f)
            {
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, rb.linearVelocity.y * 0.8f, rb.linearVelocity.z);
            }
            
            if (showDebugInfo)
            {
                Debug.Log($"[PlayerWaterFloat] Profundidad: {submersionDepth:F2}, Factor: {submersionFactor:F2}");
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
