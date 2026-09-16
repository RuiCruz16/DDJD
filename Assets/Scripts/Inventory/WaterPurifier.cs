using UnityEngine;

public class WaterPurifier : MonoBehaviour
{
    public enum State { Empty, Purifying, Ready }
    public State currentState = State.Empty;

    [Header("Purification Settings")]
    [Tooltip("Seconds it takes to fully purify the water")]
    public float purifyTime = 8f; 
    private float purifyTimer = 0f;

    [Header("Assets")]
    public ItemSO drinkableWaterItem; 

    void Update()
    {
        if (currentState == State.Purifying)
        {
            purifyTimer += Time.deltaTime;
            if (purifyTimer >= purifyTime)
            {
                currentState = State.Ready;
            }
        }
    }

    public void InsertSaltWater()
    {
        if (currentState == State.Empty)
        {
            currentState = State.Purifying;
            purifyTimer = 0f;
        }
    }

    public ItemSO CollectWater()
    {
        if (currentState == State.Ready)
        {
            currentState = State.Empty;
            purifyTimer = 0f;
            return drinkableWaterItem;
        }
        return null;
    }

    public float GetProgressPercentage()
    {
        if (currentState == State.Purifying)
        {
            return Mathf.Clamp01(purifyTimer / purifyTime) * 100f;
        }
        else if (currentState == State.Ready)
        {
            return 100f;
        }
        return 0f;
    }
}