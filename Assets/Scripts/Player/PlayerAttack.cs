using UnityEngine;

[RequireComponent(typeof(Transform))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Melee Settings")]
    public float meleeRange = 5f;
    public float meleeRadius = 0.5f;
    public LayerMask hitMask = ~0;

    [Header("References")]
    public Inventory inventory;

    void Start()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<Inventory>();
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            DoMeleeAttack();
        }
    }

    void DoMeleeAttack()
    {
        // Only allow attacking when the equipped item is a weapon.
        ItemSO equipped = inventory != null ? inventory.GetEquippedItem() : null;
        if (equipped == null || !equipped.isWeapon)
        {
            return;
        }

        Collider[] overlaps = Physics.OverlapSphere(transform.position, meleeRange, hitMask.value);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Shark shark = overlaps[i].GetComponentInParent<Shark>();
            if (shark != null)
            {
                shark.OnAttacked(transform.position);
                return;
            }
        }
    }
}