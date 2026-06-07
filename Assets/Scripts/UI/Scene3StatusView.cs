using Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class Scene3StatusView : MonoBehaviour
    {
        public PlayerStateManager playerStateManager;
        public InGameManager inGameManager;
        public Phase2Director phase2Director;
        public TMP_Text physicalText;
        public TMP_Text mentalText;
        public TMP_Text healthText;
        public TMP_Text turnText;
        public TMP_Text omenText;
        public TMP_Text phaseText;
        public GameObject patienceRoot;
        public TMP_Text patienceText;
        public Image patienceFillImage;

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            RefreshAll();
        }

        private void Start()
        {
            ResolveReferences();
            RefreshAll();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (playerStateManager == null)
            {
                playerStateManager = PlayerStateManager.Instance != null ? PlayerStateManager.Instance : FindObjectOfType<PlayerStateManager>();
            }

            if (inGameManager == null)
            {
                inGameManager = InGameManager.Instance != null ? InGameManager.Instance : FindObjectOfType<InGameManager>();
            }

            if (phase2Director == null)
            {
                phase2Director = inGameManager != null && inGameManager.phase2Director != null
                    ? inGameManager.phase2Director
                    : FindObjectOfType<Phase2Director>();
            }
        }

        private void Subscribe()
        {
            if (playerStateManager != null)
            {
                playerStateManager.OnStateChanged += RefreshPlayerState;
            }

            if (inGameManager != null)
            {
                inGameManager.OnTurnCountChanged += RefreshTurn;
                inGameManager.OnOmenCountChanged += RefreshOmen;
                inGameManager.OnPhaseChanged += RefreshPhase;
            }

            if (phase2Director != null)
            {
                phase2Director.OnPhase2Started += RefreshPhase2;
                phase2Director.OnPatienceChanged += RefreshPatience;
                phase2Director.OnDifficultyBonusChanged += RefreshDifficultyBonus;
                phase2Director.OnPhase2Completed += RefreshPhase2Completed;
            }
        }

        private void Unsubscribe()
        {
            if (playerStateManager != null)
            {
                playerStateManager.OnStateChanged -= RefreshPlayerState;
            }

            if (inGameManager != null)
            {
                inGameManager.OnTurnCountChanged -= RefreshTurn;
                inGameManager.OnOmenCountChanged -= RefreshOmen;
                inGameManager.OnPhaseChanged -= RefreshPhase;
            }

            if (phase2Director != null)
            {
                phase2Director.OnPhase2Started -= RefreshPhase2;
                phase2Director.OnPatienceChanged -= RefreshPatience;
                phase2Director.OnDifficultyBonusChanged -= RefreshDifficultyBonus;
                phase2Director.OnPhase2Completed -= RefreshPhase2Completed;
            }
        }

        private void RefreshAll()
        {
            RefreshPlayerState();
            if (inGameManager != null)
            {
                RefreshTurn(inGameManager.TurnCount);
                RefreshOmen(inGameManager.OmenCount);
                RefreshPhase(inGameManager.Phase);
            }

            RefreshPatience(phase2Director != null ? phase2Director.CurrentPatience : 0);
        }

        private void RefreshPlayerState()
        {
            if (playerStateManager == null)
                return;

            SetText(physicalText, playerStateManager.Physical.ToString());
            SetText(mentalText, playerStateManager.Mental.ToString());
            SetText(healthText, $"{playerStateManager.Health}/{playerStateManager.MaxHealth}");
        }

        private void RefreshTurn(int value)
        {
            SetText(turnText, $"当前回合： {value + 1}");
        }

        private void RefreshOmen(int value)
        {
            SetText(omenText, $"预兆 {value}");
        }

        private void RefreshPhase(InGamePhase phase)
        {
            switch (phase)
            {
                case InGamePhase.Exploring:
                    SetText(phaseText, "一阶段");
                    break;
                case InGamePhase.ResolvingRoomEvent:
                    SetText(phaseText, "事件中");
                    break;
                case InGamePhase.TruthRevealed:
                    SetText(phaseText, "真相揭露");
                    break;
                case InGamePhase.Phase2:
                    SetText(phaseText, "二阶段");
                    break;
                case InGamePhase.GameOver:
                    SetText(phaseText, "结局");
                    break;
                default:
                    SetText(phaseText, phase.ToString());
                    break;
            }

            RefreshPatience(phase2Director != null ? phase2Director.CurrentPatience : 0);
        }

        private void RefreshPhase2(Phase2Route route)
        {
            RefreshPhase(InGamePhase.Phase2);
        }

        private void RefreshDifficultyBonus(int value)
        {
            RefreshPatience(phase2Director != null ? phase2Director.CurrentPatience : 0);
        }

        private void RefreshPhase2Completed(bool isWin)
        {
            RefreshPatience(0);
        }

        private void RefreshPatience(int value)
        {
            bool visible = phase2Director != null && phase2Director.IsActive;
            if (patienceRoot != null)
            {
                patienceRoot.SetActive(visible);
            }

            if (!visible)
                return;

            int maxPatience = Mathf.Max(1, phase2Director.MaxPatience);
            SetText(patienceText, phase2Director.DifficultyBonus > 0
                ? $"院长耐心：{value}/{maxPatience}  难度+{phase2Director.DifficultyBonus}"
                : $"院长耐心：{value}/{maxPatience}");

            if (patienceFillImage != null)
            {
                patienceFillImage.fillAmount = Mathf.Clamp01(value / (float)maxPatience);
            }
        }

        private static void SetText(TMP_Text text, string value)
        {
            if (text != null)
            {
                text.text = value;
            }
        }
    }
}
