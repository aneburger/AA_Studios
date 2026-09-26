// Portobello's brain.

using System.Collections;
using UnityEngine;

public enum PortobelloAttack
{
    GoldBarGun,
    CoinGun,
    DollarBursts,
    GoldSpikes,
    Burrow
}

[System.Serializable]
public class PortobelloPhaseSettings
{
    [Header("Timing")]
    public float downtimeMin = 1.5f;
    public float downtimeMax = 2.5f;

    [Header("Animation")]
    public float animSpeed = 1f;
}

public class PortobelloBoss : BossBrain
{
    [Header("Phase Settings")]
    [SerializeField] private PortobelloPhaseSettings phase1 = new PortobelloPhaseSettings();
    [SerializeField] private PortobelloPhaseSettings phase2 = new PortobelloPhaseSettings
    {
        downtimeMin = 1.0f,
        downtimeMax = 1.8f,
        animSpeed = 1.15f
    };
    [SerializeField] private PortobelloPhaseSettings phase3 = new PortobelloPhaseSettings
    {
        downtimeMin = 0.7f,
        downtimeMax = 1.3f,
        animSpeed = 1.3f
    };

    protected override int LastPhase => 3;

    private PortobelloPhaseSettings CurrentSettings =>
        phase == 1 ? phase1 : phase == 2 ? phase2 : phase3;

    private BossContactDamage contact;

    // -- AWAKE --
    protected override void Awake()
    {
        base.Awake();
        contact = GetComponent<BossContactDamage>();
    }

    // -- PHASE --
    protected override void ApplyPhase(int newPhase)
    {
        SetAnimSpeed(CurrentSettings.animSpeed);
    }

    protected override void OnFightCancelled()
    {
    }

    // ==================== DIRECTOR ====================
    protected override IEnumerator FightLoop()
    {
        yield return new WaitForSeconds(openingDelay);

        if (contact != null) contact.SetContact(2f, 5f);

        while (true)
        {
            facePlayer = true;

            float wait = Random.Range(CurrentSettings.downtimeMin, CurrentSettings.downtimeMax);
            yield return new WaitForSeconds(wait);
        }
    }
}