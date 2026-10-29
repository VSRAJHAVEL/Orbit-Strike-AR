using UnityEngine;
using UnityEngine.UI;
using TMPro;
using OrbitStrike.Core;
using OrbitStrike.Data;

namespace OrbitStrike.UI
{
    /// <summary>
    /// Rocket Hangar & Upgrade Shop interface:
    /// - Archetype selector (Basic, Heavy, Speedster, Precision)
    /// - Stat bars (Mass, Thrust, Burn Time, Blast Radius)
    /// - Purchase and upgrade actions with persistent coin economy
    /// </summary>
    public class ShopUI : MonoBehaviour
    {
        [Header("Coin Header")]
        [SerializeField] private TextMeshProUGUI coinsText;

        [Header("Rocket Info")]
        [SerializeField] private TextMeshProUGUI rocketNameText;
        [SerializeField] private TextMeshProUGUI rocketDescText;
        [SerializeField] private Image thrustStatBar;
        [SerializeField] private Image massStatBar;
        [SerializeField] private Image blastStatBar;
        [SerializeField] private Button selectOrUnlockButton;
        [SerializeField] private TextMeshProUGUI selectOrUnlockText;

        [Header("Upgrade Buttons")]
        [SerializeField] private Button upgradeThrustButton;
        [SerializeField] private TextMeshProUGUI upgradeThrustCostText;
        [SerializeField] private Button upgradeBlastButton;
        [SerializeField] private TextMeshProUGUI upgradeBlastCostText;

        private RocketType _inspectedRocket = RocketType.Basic;

        private void OnEnable()
        {
            EventBus.OnCoinsChanged += HandleCoinsChanged;
            UpdateCoinDisplay();
            InspectRocket(RocketType.Basic);
        }

        private void OnDisable()
        {
            EventBus.OnCoinsChanged -= HandleCoinsChanged;
        }

        private void HandleCoinsChanged(int currentCoins, int delta)
        {
            UpdateCoinDisplay();
        }

        private void UpdateCoinDisplay()
        {
            if (coinsText != null && SaveManager.Instance != null)
            {
                coinsText.text = $"🪙 {SaveManager.Instance.Data.coins:N0}";
            }
        }

        public void InspectRocket(int rocketTypeIndex)
        {
            InspectRocket((RocketType)rocketTypeIndex);
        }

        public void InspectRocket(RocketType type)
        {
            _inspectedRocket = type;

            switch (type)
            {
                case RocketType.Basic:
                    SetInfo("STRIKE-1 (BASIC)", "Balanced workhorse rocket. Moderate thrust and standard aerodynamic drag.", 0.6f, 0.5f, 0.5f, 0);
                    break;
                case RocketType.Heavy:
                    SetInfo("TITAN-IV (HEAVY)", "Heavily armored warhead with massive blast radius. Requires high angle to clear obstacles.", 0.9f, 0.9f, 1.0f, 300);
                    break;
                case RocketType.Speedster:
                    SetInfo("VELOCITY-X (SPEED)", "Ultra-lightweight sprint rocket with blistering acceleration.", 0.85f, 0.25f, 0.4f, 450);
                    break;
                case RocketType.Precision:
                    SetInfo("HAWK-EYE (PRECISION)", "Equipped with advanced aerodynamic fin stabilization. Superior trajectory predictability.", 0.55f, 0.4f, 0.6f, 600);
                    break;
            }
        }

        private void SetInfo(string name, string desc, float thrust, float mass, float blast, int unlockCost)
        {
            if (rocketNameText != null) rocketNameText.text = name;
            if (rocketDescText != null) rocketDescText.text = desc;
            if (thrustStatBar != null) thrustStatBar.fillAmount = thrust;
            if (massStatBar != null) massStatBar.fillAmount = mass;
            if (blastStatBar != null) blastStatBar.fillAmount = blast;

            bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsRocketUnlocked(_inspectedRocket.ToString());

            if (selectOrUnlockButton != null && selectOrUnlockText != null)
            {
                if (isUnlocked)
                {
                    bool isEquipped = GameManager.Instance != null && GameManager.Instance.ActiveRocketType == _inspectedRocket;
                    selectOrUnlockText.text = isEquipped ? "EQUIPPED" : "EQUIP";
                    selectOrUnlockButton.interactable = !isEquipped;
                }
                else
                {
                    selectOrUnlockText.text = $"UNLOCK (🪙{unlockCost})";
                    bool canAfford = SaveManager.Instance != null && SaveManager.Instance.Data.coins >= unlockCost;
                    selectOrUnlockButton.interactable = canAfford;
                }
            }
        }

        public void OnSelectOrUnlockClicked()
        {
            bool isUnlocked = SaveManager.Instance != null && SaveManager.Instance.IsRocketUnlocked(_inspectedRocket.ToString());

            if (isUnlocked)
            {
                EventBus.TriggerRocketSelected(_inspectedRocket);
                InspectRocket(_inspectedRocket);
            }
            else
            {
                int cost = _inspectedRocket switch
                {
                    RocketType.Heavy => 300,
                    RocketType.Speedster => 450,
                    RocketType.Precision => 600,
                    _ => 0
                };

                if (SaveManager.Instance != null && SaveManager.Instance.SpendCoins(cost))
                {
                    SaveManager.Instance.UnlockRocket(_inspectedRocket.ToString());
                    EventBus.TriggerRocketSelected(_inspectedRocket);
                    InspectRocket(_inspectedRocket);
                }
            }
        }

        public void BuyThrustUpgrade()
        {
            if (SaveManager.Instance != null && SaveManager.Instance.SpendCoins(150))
            {
                SaveManager.Instance.Data.thrustUpgradeLevel++;
                SaveManager.Instance.Save();
                UpdateCoinDisplay();
            }
        }
    }
}

