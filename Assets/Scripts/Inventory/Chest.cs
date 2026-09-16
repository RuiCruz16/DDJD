using UnityEngine;

public class Chest : MonoBehaviour
{
    [Header("Chest Storage")]
    public ItemSO[] storedItems = new ItemSO[15];
    public int[] storedAmounts = new int[15];
}
