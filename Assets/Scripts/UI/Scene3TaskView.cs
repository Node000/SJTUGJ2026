using Gameplay;
using TMPro;
using UnityEngine;

namespace UI
{
    public class Scene3TaskView : MonoBehaviour
    {
        public PlayerInventory playerInventory;
        public InGameManager inGameManager;
        public Phase2Director phase2Director;
        public ItemData patientLetterItem;
        public TMP_Text taskText;

        public string startTaskText = "探寻医院的真相";
        public string patientLetterTaskText = "去目标手术室揭开真相";
        public string phase2TaskText = "前往院长室，直面院长";

        private void OnEnable()
        {
            ResolveReferences();
            Subscribe();
            Refresh();
        }

        private void Start()
        {
            ResolveReferences();
            Refresh();
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        private void ResolveReferences()
        {
            if (inGameManager == null)
            {
                inGameManager = InGameManager.Instance != null ? InGameManager.Instance : FindObjectOfType<InGameManager>();
            }

            if (playerInventory == null)
            {
                playerInventory = inGameManager != null && inGameManager.playerInventory != null
                    ? inGameManager.playerInventory
                    : FindObjectOfType<PlayerInventory>();
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
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged += Refresh;
            }

            if (inGameManager != null)
            {
                inGameManager.OnPhaseChanged += RefreshPhase;
            }

            if (phase2Director != null)
            {
                phase2Director.OnPhase2Started += RefreshPhase2;
                phase2Director.OnPhase2Completed += RefreshPhase2Completed;
            }
        }

        private void Unsubscribe()
        {
            if (playerInventory != null)
            {
                playerInventory.OnInventoryChanged -= Refresh;
            }

            if (inGameManager != null)
            {
                inGameManager.OnPhaseChanged -= RefreshPhase;
            }

            if (phase2Director != null)
            {
                phase2Director.OnPhase2Started -= RefreshPhase2;
                phase2Director.OnPhase2Completed -= RefreshPhase2Completed;
            }
        }

        private void RefreshPhase(InGamePhase phase)
        {
            Refresh();
        }

        private void RefreshPhase2(Phase2Route route)
        {
            Refresh();
        }

        private void RefreshPhase2Completed(bool isWin)
        {
            Refresh();
        }

        private void Refresh()
        {
            if (taskText == null)
                return;

            if ((inGameManager != null && inGameManager.Phase == InGamePhase.Phase2)
                || (phase2Director != null && phase2Director.IsActive))
            {
                taskText.text = phase2TaskText;
                return;
            }

            if (playerInventory != null && patientLetterItem != null && playerInventory.Contains(patientLetterItem))
            {
                taskText.text = patientLetterTaskText;
                return;
            }

            taskText.text = startTaskText;
        }
    }
}
