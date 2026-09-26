using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CheatMenuManager : MonoBehaviour
{
    [Header("Open/Close Cheats Panel")]
    [SerializeField] private GameObject cheatsPanel;       
    [SerializeField] private Button openCheatsButton;     

    [Header("Invincibility")]
    [SerializeField] private Toggle invincibilityToggle;   

    [Header("Infinite Ammo")]
    [SerializeField] private Toggle infiniteAmmoToggle;     

    [Header("Instant Actions")]
    [SerializeField] private Button refillHealthButton;    
    [SerializeField] private Button fillMutationBarButton;  

    [Header("Skip To Level")]
    [SerializeField] private TMP_InputField skipToLevelInput; 

    [Header("Boons")]
    [SerializeField] private List<BoonCardData> allBoons;
    [SerializeField] private TMP_Dropdown boonDropdown;     
    [SerializeField] private Button applyBoonButton;        
    [SerializeField] private Toggle boonToggle;             

    [Header("Weapons")]
    [SerializeField] private List<WeaponData> allWeapons;
    [SerializeField] private TMP_Dropdown weaponDropdown;  
    [SerializeField] private Button equipButton;            

    private string invincibilityBaseLabel = "Invincibility";
    private string infiniteAmmoBaseLabel = "Infinite Ammo";
    private TMP_Text invincibilityLabelText;
    private TMP_Text infiniteAmmoLabelText;
    private TMP_Text boonToggleLabelText;

    private void Start()
    {
        SetupOpenCheatsButton();
        SetupInvincibilityToggle();
        SetupInfiniteAmmoToggle();
        SetupInstantActions();
        SetupSkipToLevel();
        SetupBoonControls();
        SetupWeaponControls();

        if (CheatManager.Instance != null)
        {
            CheatManager.Instance.OnInvincibilityChanged += OnInvincibilityChanged;
            CheatManager.Instance.OnInfiniteAmmoChanged += OnInfiniteAmmoChanged;
        }
    }

    private void OnDestroy()
    {
        if (CheatManager.Instance != null)
        {
            CheatManager.Instance.OnInvincibilityChanged -= OnInvincibilityChanged;
            CheatManager.Instance.OnInfiniteAmmoChanged -= OnInfiniteAmmoChanged;
        }
    }

    private void SetupOpenCheatsButton()
    {
        if (cheatsPanel != null)
            cheatsPanel.SetActive(false);

        if (openCheatsButton != null)
            openCheatsButton.onClick.AddListener(ToggleCheatsPanel);
    }

    private void ToggleCheatsPanel()
    {
        if (cheatsPanel == null) return;
        cheatsPanel.SetActive(!cheatsPanel.activeSelf);
    }

    // ------INVINCIBILITY

    private void SetupInvincibilityToggle()
    {
        if (invincibilityToggle == null) return;

        invincibilityLabelText = invincibilityToggle.GetComponentInChildren<TMP_Text>();
        if (invincibilityLabelText != null)
            invincibilityBaseLabel = StripOnOffSuffix(invincibilityLabelText.text);

        bool current = CheatManager.Instance != null && CheatManager.Instance.Invincibility;
        invincibilityToggle.SetIsOnWithoutNotify(current);
        UpdateToggleLabel(invincibilityLabelText, invincibilityBaseLabel, current);

        invincibilityToggle.onValueChanged.AddListener(value => CheatManager.Instance?.SetInvincibility(value));
    }

    private void OnInvincibilityChanged(bool value)
    {
        invincibilityToggle?.SetIsOnWithoutNotify(value);
        UpdateToggleLabel(invincibilityLabelText, invincibilityBaseLabel, value);
    }

    // ------- INFINITE AMMO 

    private void SetupInfiniteAmmoToggle()
    {
        if (infiniteAmmoToggle == null) return;

        infiniteAmmoLabelText = infiniteAmmoToggle.GetComponentInChildren<TMP_Text>();
        if (infiniteAmmoLabelText != null)
            infiniteAmmoBaseLabel = StripOnOffSuffix(infiniteAmmoLabelText.text);

        bool current = CheatManager.Instance != null && CheatManager.Instance.InfiniteAmmo;
        infiniteAmmoToggle.SetIsOnWithoutNotify(current);
        UpdateToggleLabel(infiniteAmmoLabelText, infiniteAmmoBaseLabel, current);

        infiniteAmmoToggle.onValueChanged.AddListener(value => CheatManager.Instance?.SetInfiniteAmmo(value));
    }

    private void OnInfiniteAmmoChanged(bool value)
    {
        infiniteAmmoToggle?.SetIsOnWithoutNotify(value);
        UpdateToggleLabel(infiniteAmmoLabelText, infiniteAmmoBaseLabel, value);
    }

    // --------- INSTANT ACTIONS 

    private void SetupInstantActions()
    {
        if (refillHealthButton != null)
            refillHealthButton.onClick.AddListener(() => CheatManager.Instance?.RefillHealth());

        if (fillMutationBarButton != null)
            fillMutationBarButton.onClick.AddListener(() => CheatManager.Instance?.FillMutationBar());
    }

    // ------ SKIP TO LEVEL 

    private void SetupSkipToLevel()
    {
        if (skipToLevelInput == null) return;
        skipToLevelInput.onEndEdit.AddListener(SubmitSkipToLevel);
    }

    private void SubmitSkipToLevel(string text)
    {
        if (string.IsNullOrEmpty(text)) return;

        if (int.TryParse(text, out int floorNumber))
        {
            CheatManager.Instance?.SkipToLevel(floorNumber);
        }
        else
        {
            Debug.LogWarning($"CheatMenuManager: '{text}' is not a valid floor number.");
        }
    }

    // --------- BOONS 

    private void SetupBoonControls()
    {
        if (boonDropdown == null || allBoons == null) return;

        boonDropdown.ClearOptions();

        List<string> options = new List<string>();
        foreach (BoonCardData boon in allBoons)
            options.Add(boon != null ? boon.displayName : "(missing)");

        boonDropdown.AddOptions(options);
        boonDropdown.onValueChanged.AddListener(_ => RefreshBoonToggle());

        if (applyBoonButton != null)
            applyBoonButton.onClick.AddListener(ApplySelectedBoon);

        if (boonToggle != null)
        {
            boonToggleLabelText = boonToggle.GetComponentInChildren<TMP_Text>();
            boonToggle.onValueChanged.AddListener(OnBoonToggleChanged);
        }

        RefreshBoonToggle();
    }

    private void RefreshBoonToggle()
    {
        BoonCardData selected = GetSelectedBoon();
        bool active = selected != null && CheatManager.Instance != null && CheatManager.Instance.IsBoonActive(selected.boonId);

        boonToggle?.SetIsOnWithoutNotify(active);

        if (boonToggleLabelText != null && selected != null)
            boonToggleLabelText.text = $"{selected.displayName} {(active ? "On" : "Off")}";
    }

    private void OnBoonToggleChanged(bool value)
    {
        BoonCardData selected = GetSelectedBoon();
        if (selected == null) return;

        CheatManager.Instance?.SetBoonCheat(selected.boonId, value);

        if (boonToggleLabelText != null)
            boonToggleLabelText.text = $"{selected.displayName} {(value ? "On" : "Off")}";
    }

    private void ApplySelectedBoon()
    {
        BoonCardData selected = GetSelectedBoon();
        if (selected == null) return;

        CheatManager.Instance?.SetBoonCheat(selected.boonId, true);
        RefreshBoonToggle();
    }

    private BoonCardData GetSelectedBoon()
    {
        if (boonDropdown == null || allBoons == null) return null;

        int index = boonDropdown.value;
        if (index < 0 || index >= allBoons.Count) return null;

        return allBoons[index];
    }

    // ------- WEAPONS 

    private void SetupWeaponControls()
    {
        if (weaponDropdown == null || allWeapons == null) return;

        weaponDropdown.ClearOptions();

        List<string> options = new List<string>();
        foreach (WeaponData weapon in allWeapons)
            options.Add(weapon != null ? weapon.gunName : "(missing)");

        weaponDropdown.AddOptions(options);

        if (equipButton != null)
            equipButton.onClick.AddListener(GiveSelectedWeapon);
    }

    private void GiveSelectedWeapon()
    {
        if (weaponDropdown == null || allWeapons == null) return;

        int index = weaponDropdown.value;
        if (index < 0 || index >= allWeapons.Count) return;

        CheatManager.Instance?.GiveWeapon(allWeapons[index]);
    }

    // -------- HELPERS 

    private void UpdateToggleLabel(TMP_Text label, string baseText, bool isOn)
    {
        if (label == null) return;
        label.text = $"{baseText} {(isOn ? "On" : "Off")}";
    }
    
    private string StripOnOffSuffix(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        if (text.EndsWith(" On")) return text.Substring(0, text.Length - 3);
        if (text.EndsWith(" Off")) return text.Substring(0, text.Length - 4);

        return text;
    }
}