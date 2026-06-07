using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    public class Scene1CharacterButtonNavigator : MonoBehaviour
    {
        public RectTransform character;
        public RectTransform[] buttonTargets;
        public int startIndex;
        public float jumpDuration = 0.22f;
        public float jumpHeight = 55f;
        public GameObject audioSettingsPanel;
        public int audioSettingsTargetIndex = 2;
        public bool hideAudioSettingsOnStart = true;

        private int currentIndex;
        private Coroutine jumpRoutine;

        private void Start()
        {
            Canvas.ForceUpdateCanvases();
            if (audioSettingsPanel != null && hideAudioSettingsOnStart)
            {
                audioSettingsPanel.SetActive(false);
            }

            currentIndex = Mathf.Clamp(startIndex, 0, GetLastTargetIndex());
            MoveCharacterToCurrentTarget(false);
            SelectCurrentButton();
        }

        private void Update()
        {
            if (buttonTargets == null || buttonTargets.Length == 0 || character == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A))
            {
                JumpToDirection(Vector2.left);
            }
            else if (Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D))
            {
                JumpToDirection(Vector2.right);
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W))
            {
                JumpToDirection(Vector2.up);
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S))
            {
                JumpToDirection(Vector2.down);
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
            {
                PressCurrentButton();
            }
        }

        private void JumpToDirection(Vector2 direction)
        {
            int nextIndex = FindGridTargetInDirection(direction);
            if (nextIndex < 0)
            {
                nextIndex = FindNearestTargetInDirection(direction);
            }

            if (nextIndex < 0 || nextIndex == currentIndex)
            {
                return;
            }

            currentIndex = nextIndex;
            MoveCharacterToCurrentTarget(true);
            SelectCurrentButton();
        }

        private int FindGridTargetInDirection(Vector2 direction)
        {
            if (buttonTargets == null || buttonTargets.Length < 3)
            {
                return -1;
            }

            if (currentIndex == 0)
            {
                if (direction == Vector2.right)
                {
                    return 1;
                }

                if (direction == Vector2.down)
                {
                    return 2;
                }
            }

            if (currentIndex == 1 && direction == Vector2.left)
            {
                return 0;
            }

            if (currentIndex == 2 && direction == Vector2.up)
            {
                return 0;
            }

            return -1;
        }

        private int FindNearestTargetInDirection(Vector2 direction)
        {
            Vector2 currentPosition = GetTargetLocalPosition(buttonTargets[currentIndex]);
            int bestIndex = -1;
            float bestScore = float.MaxValue;

            for (int i = 0; i < buttonTargets.Length; i++)
            {
                RectTransform target = buttonTargets[i];
                if (i == currentIndex || target == null || !target.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector2 offset = GetTargetLocalPosition(target) - currentPosition;
                float directionalDistance = Vector2.Dot(offset, direction);
                if (directionalDistance <= 1f)
                {
                    continue;
                }

                float sidewaysDistance = Mathf.Abs(Vector2.Dot(offset, new Vector2(-direction.y, direction.x)));
                float score = directionalDistance + sidewaysDistance * 1.5f;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private void MoveCharacterToCurrentTarget(bool animated)
        {
            Vector2 targetPosition = GetTargetLocalPosition(buttonTargets[currentIndex]);
            if (jumpRoutine != null)
            {
                StopCoroutine(jumpRoutine);
                jumpRoutine = null;
            }

            if (!animated || jumpDuration <= 0f)
            {
                character.anchoredPosition = targetPosition;
                return;
            }

            jumpRoutine = StartCoroutine(JumpTo(targetPosition));
        }

        private IEnumerator JumpTo(Vector2 targetPosition)
        {
            Vector2 startPosition = character.anchoredPosition;
            float elapsed = 0f;

            while (elapsed < jumpDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / jumpDuration);
                Vector2 position = Vector2.Lerp(startPosition, targetPosition, t);
                position.y += Mathf.Sin(t * Mathf.PI) * jumpHeight;
                character.anchoredPosition = position;
                yield return null;
            }

            character.anchoredPosition = targetPosition;
            jumpRoutine = null;
        }

        private Vector2 GetTargetLocalPosition(RectTransform target)
        {
            if (target == null || character == null || character.parent == null)
            {
                return Vector2.zero;
            }

            Vector3 worldPosition = target.TransformPoint(target.rect.center);
            return ((RectTransform)character.parent).InverseTransformPoint(worldPosition);
        }

        private void SelectCurrentButton()
        {
            Button button = GetCurrentButton();
            if (button != null && EventSystem.current != null)
            {
                EventSystem.current.SetSelectedGameObject(button.gameObject);
            }
        }

        private void PressCurrentButton()
        {
            if (currentIndex == audioSettingsTargetIndex && audioSettingsPanel != null)
            {
                audioSettingsPanel.SetActive(true);
                SelectAudioSettingsControl();
                InvokeCurrentButtonForFeedback();
                return;
            }

            SceneLoadButton sceneLoadButton = GetCurrentSceneLoadButton();
            if (sceneLoadButton != null)
            {
                sceneLoadButton.LoadScene();
                return;
            }

            Button button = GetCurrentButton();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }

        private void InvokeCurrentButtonForFeedback()
        {
            Button button = GetCurrentButton();
            if (button != null && button.interactable)
            {
                button.onClick.Invoke();
            }
        }

        private void SelectAudioSettingsControl()
        {
            if (audioSettingsPanel == null || EventSystem.current == null)
            {
                return;
            }

            Selectable selectable = audioSettingsPanel.GetComponentInChildren<Selectable>(true);
            if (selectable != null)
            {
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
            }
        }

        private Button GetCurrentButton()
        {
            if (buttonTargets == null || currentIndex < 0 || currentIndex >= buttonTargets.Length || buttonTargets[currentIndex] == null)
            {
                return null;
            }

            return buttonTargets[currentIndex].GetComponent<Button>();
        }

        private SceneLoadButton GetCurrentSceneLoadButton()
        {
            if (buttonTargets == null || currentIndex < 0 || currentIndex >= buttonTargets.Length || buttonTargets[currentIndex] == null)
            {
                return null;
            }

            return buttonTargets[currentIndex].GetComponent<SceneLoadButton>();
        }

        private int GetLastTargetIndex()
        {
            return buttonTargets == null || buttonTargets.Length == 0 ? 0 : buttonTargets.Length - 1;
        }
    }
}
