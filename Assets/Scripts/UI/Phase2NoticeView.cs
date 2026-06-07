using System.Collections;
using Gameplay;
using TMPro;
using UnityEngine;

namespace UI
{
    public class Phase2NoticeView : MonoBehaviour
    {
        public Phase2Director phase2Director;
        public GameObject panelRoot;
        public TMP_Text titleText;
        public TMP_Text bodyText;
        public float visibleSeconds = 4f;
        public string title = "二阶段开启";
        public string bloodyKnifeMessage = "所有出入口已关闭。院长室已经出现，院长正在等待与你对峙。";
        public string patientLetterMessage = "所有出入口已关闭。院长室已经出现，也许患者留下的文字能让他停下。";

        private Coroutine hideRoutine;

        private void Awake()
        {
            if (panelRoot == null)
            {
                panelRoot = gameObject;
            }

            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            ResolveReferences();
            if (phase2Director != null)
            {
                phase2Director.OnPhase2Started += Show;
                if (phase2Director.IsActive)
                {
                    Show(phase2Director.CurrentRoute);
                }
            }
        }

        private void OnDisable()
        {
            if (phase2Director != null)
            {
                phase2Director.OnPhase2Started -= Show;
            }
        }

        private void ResolveReferences()
        {
            if (phase2Director == null)
            {
                phase2Director = FindObjectOfType<Phase2Director>();
            }
        }

        private void Show(Phase2Route route)
        {
            if (titleText != null)
            {
                titleText.text = title;
            }

            if (bodyText != null)
            {
                bodyText.text = route == Phase2Route.PatientLetter ? patientLetterMessage : bloodyKnifeMessage;
            }

            if (hideRoutine != null)
            {
                StopCoroutine(hideRoutine);
            }

            panelRoot.SetActive(true);
            hideRoutine = StartCoroutine(HideAfterDelay());
        }

        private IEnumerator HideAfterDelay()
        {
            yield return new WaitForSecondsRealtime(visibleSeconds);
            panelRoot.SetActive(false);
            hideRoutine = null;
        }
    }
}
