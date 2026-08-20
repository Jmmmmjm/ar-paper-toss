using UnityEngine;

/// <summary>
/// Manages tiered gameplay visual effects using Cartoon FX Remaster:
/// - Dynamic score celebrations scaling with combo streaks (1x Swish -> 2x Fireworks -> 3x Fire -> 4x Lightning -> 5x+ Mega Rainbow Fireworks)
/// - Subtle impact dust puffs on paper ball collisions
/// </summary>
public class VFXManager : MonoBehaviour
{
    public static VFXManager Instance { get; private set; }

    [Header("Combo Tier 1: Single Swish (1x)")]
    [SerializeField] private GameObject _tier1LightBurst;
    [SerializeField] private GameObject _tier1FallingStars;

    [Header("Combo Tier 2: Double Combo (2x)")]
    [SerializeField] private GameObject _tier2Firework;
    [SerializeField] private GameObject _tier2Stars;

    [Header("Combo Tier 3: Triple Fire Combo (3x)")]
    [SerializeField] private GameObject _tier3FireBurst;
    [SerializeField] private GameObject _tier3SparksRain;

    [Header("Combo Tier 4: Lightning Combo (4x)")]
    [SerializeField] private GameObject _tier4ElectricBurst;
    [SerializeField] private GameObject _tier4Sparks;

    [Header("Combo Tier 5+: Godlike Mega Fireworks (5x+)")]
    [SerializeField] private GameObject _tier5MegaFirework;
    [SerializeField] private GameObject _tier5ShinyRays;

    [Header("Impact VFX Prefabs")]
    [SerializeField] private GameObject _impactPoofPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        ScoreTrigger.OnSuccessfulScore += HandleScore;
    }

    private void OnDisable()
    {
        ScoreTrigger.OnSuccessfulScore -= HandleScore;
    }

    private void HandleScore()
    {
        // Get current streak from ScoreboardUI if available
        int streak = 1;
        var scoreboard = FindFirstObjectByType<ScoreboardUI>();
        if (scoreboard != null)
        {
            streak = scoreboard.CurrentStreak;
        }

        // Find trash can top opening
        Vector3 spawnPos = Vector3.zero;
        bool found = false;

        var placement = FindFirstObjectByType<PlacementController>();
        if (placement != null)
        {
            GameObject trashCan = placement.GetSpawnedTrashCan();
            if (trashCan != null)
            {
                spawnPos = trashCan.transform.position + Vector3.up * 0.38f;
                found = true;
            }
        }

        if (!found)
        {
            var trigger = FindFirstObjectByType<ScoreTrigger>();
            if (trigger != null)
            {
                spawnPos = trigger.transform.position;
                found = true;
            }
        }

        if (found)
        {
            PlayScoreVFX(spawnPos, streak);
        }
    }

    public void PlayScoreVFX(Vector3 position, int streak)
    {
        Debug.Log($"[VFXManager] Playing Score VFX for Combo Streak: {streak}x");

        if (streak <= 1)
        {
            // TIER 1 (1x Swish): Clean, crisp light pop & gentle stars
            SpawnVFX(_tier1LightBurst, position, 0.40f, 2.0f);
            SpawnVFX(_tier1FallingStars, position + Vector3.up * 0.1f, 0.35f, 2.5f);
        }
        else if (streak == 2)
        {
            // TIER 2 (2x Double): Cyan-Purple Fireworks explosion & falling stars
            SpawnVFX(_tier2Firework, position, 0.45f, 3.0f);
            SpawnVFX(_tier2Stars, position + Vector3.up * 0.1f, 0.45f, 3.0f);
        }
        else if (streak == 3)
        {
            // TIER 3 (3x On Fire!): Intense fire explosion & golden spark shower
            SpawnVFX(_tier3FireBurst, position, 0.48f, 3.0f);
            SpawnVFX(_tier3SparksRain, position + Vector3.up * 0.15f, 0.42f, 3.5f);
        }
        else if (streak == 4)
        {
            // TIER 4 (4x Lightning!): Electric plasma burst & high-voltage sparks
            SpawnVFX(_tier4ElectricBurst, position, 0.48f, 3.0f);
            SpawnVFX(_tier4Sparks, position + Vector3.up * 0.1f, 0.45f, 3.0f);
        }
        else
        {
            // TIER 5+ (5x+ UNSTOPPABLE!): Mega rainbow fireworks explosion & golden celebratory rays
            SpawnVFX(_tier5MegaFirework, position, 0.55f, 4.0f);
            SpawnVFX(_tier2Firework, position + Vector3.up * 0.12f, 0.50f, 3.5f);
            SpawnVFX(_tier5ShinyRays, position + Vector3.up * 0.05f, 0.40f, 2.5f);
        }
    }

    private void SpawnVFX(GameObject prefab, Vector3 position, float scale, float duration)
    {
        if (prefab == null) return;
        GameObject fx = Instantiate(prefab, position, Quaternion.identity);
        fx.transform.localScale = Vector3.one * scale;
        Destroy(fx, duration);
    }

    public void PlayImpactPoof(Vector3 position, Vector3 normal, float impactSpeed = 1.0f)
    {
        if (_impactPoofPrefab == null) return;

        Quaternion rot = normal != Vector3.zero ? Quaternion.LookRotation(normal) : Quaternion.identity;
        GameObject poof = Instantiate(_impactPoofPrefab, position, rot);

        float scale = Mathf.Clamp(0.06f + impactSpeed * 0.02f, 0.06f, 0.11f);
        poof.transform.localScale = Vector3.one * scale;

        Destroy(poof, 1.2f);
    }
}
