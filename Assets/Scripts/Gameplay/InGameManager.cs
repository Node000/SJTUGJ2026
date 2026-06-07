using System;
using UnityEngine;

namespace Gameplay
{
    public enum InGamePhase
    {
        Preparation,
        Exploring,
        ResolvingRoomEvent,
        TruthRevealed,
        Phase2,
        GameOver
    }

    public class InGameManager : MonoBehaviour
    {
        public static InGameManager Instance { get; private set; }

        public BoardManager boardManager;
        public RoomEventHandler roomEventHandler;
        public PlayerGridMovement playerMovement;
        public PlayerStateManager playerStateManager;
        public PlayerInventory playerInventory;
        public Phase2Director phase2Director;
        public bool triggerPhase2OnOmenThreshold = true;
        public int omenCountForPhase2 = 3;

        [SerializeField] private InGamePhase phase = InGamePhase.Exploring;
        [SerializeField] private int turnCount;
        [SerializeField] private int omenCount;
        [SerializeField] private bool truthRevealed;

        private RoomCard currentRoom;
        private RoomEventData currentEventData;
        private bool? gameResult;

        public InGamePhase Phase => phase;
        public int TurnCount => turnCount;
        public int OmenCount => omenCount;
        public bool TruthRevealed => truthRevealed;
        public RoomCard CurrentRoom => currentRoom;
        public RoomEventData CurrentEventData => currentEventData;
        public bool? GameResult => gameResult;
        public bool CanPlayerAct => phase == InGamePhase.Exploring || phase == InGamePhase.TruthRevealed || phase == InGamePhase.Phase2;

        public event Action<InGamePhase> OnPhaseChanged;
        public event Action<int> OnTurnCountChanged;
        public event Action<int> OnOmenCountChanged;
        public event Action OnTruthRevealed;
        public event Action<RoomCard> OnRoomEntered;
        public event Action<RoomCard> OnRoomResolved;
        public event Action<bool> OnGameEnded;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            ResolveReferences();
            SubscribePlayerState();
            SubscribePhase2();
        }

        private void OnDestroy()
        {
            UnsubscribePlayerState();
            UnsubscribePhase2();

            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void EnterRoom(Vector2Int gridPosition)
        {
            if (!CanPlayerAct || boardManager == null)
                return;

            RoomCard room = boardManager.GetRoom(gridPosition);

            if (room == null || room.hasResolvedEvent)
                return;

            currentRoom = room;
            currentEventData = room.data != null ? room.data.eventData : null;
            OnRoomEntered?.Invoke(room);
            SetPhase(InGamePhase.ResolvingRoomEvent);

            if (roomEventHandler != null)
            {
                roomEventHandler.HandleRoomEvent(room, currentEventData, ResolveCurrentRoom);
            }
            else
            {
                ResolveCurrentRoom();
            }
        }

        public void StartNextTurn()
        {
            turnCount++;
            OnTurnCountChanged?.Invoke(turnCount);
        }

        public void AddOmen(int amount = 1)
        {
            if (amount <= 0)
                return;

            omenCount += amount;
            OnOmenCountChanged?.Invoke(omenCount);
            TryTriggerPhase2FromOmenCount();
        }

        public void RevealTruth()
        {
            if (truthRevealed)
                return;

            truthRevealed = true;
            SetPhase(InGamePhase.TruthRevealed);
            OnTruthRevealed?.Invoke();
        }

        public void EndGame(bool isWin)
        {
            if (phase == InGamePhase.GameOver)
                return;

            gameResult = isWin;
            SetPhase(InGamePhase.GameOver);
            OnGameEnded?.Invoke(isWin);
        }

        private void ResolveCurrentRoom()
        {
            RoomCard resolvedRoom = currentRoom;

            if (currentRoom != null)
            {
                currentRoom.hasResolvedEvent = true;
            }

            if (currentEventData != null && currentEventData.category == RoomEventCategory.Omen)
            {
                AddOmen();
            }

            currentRoom = null;
            currentEventData = null;
            OnRoomResolved?.Invoke(resolvedRoom);

            if (phase != InGamePhase.GameOver)
            {
                SetPhase(phase2Director != null && phase2Director.IsActive
                    ? InGamePhase.Phase2
                    : truthRevealed ? InGamePhase.TruthRevealed : InGamePhase.Exploring);
            }
        }

        private void ResolveReferences()
        {
            if (boardManager == null)
            {
                boardManager = FindObjectOfType<BoardManager>();
            }

            if (roomEventHandler == null)
            {
                roomEventHandler = FindObjectOfType<RoomEventHandler>();
            }

            if (playerMovement == null)
            {
                playerMovement = FindObjectOfType<PlayerGridMovement>();
            }

            if (playerStateManager == null)
            {
                playerStateManager = PlayerStateManager.Instance != null ? PlayerStateManager.Instance : FindObjectOfType<PlayerStateManager>();
            }

            if (playerInventory == null)
            {
                playerInventory = FindObjectOfType<PlayerInventory>();
            }

            if (phase2Director == null)
            {
                phase2Director = FindObjectOfType<Phase2Director>();
            }
        }

        private void TryTriggerPhase2FromOmenCount()
        {
            if (!triggerPhase2OnOmenThreshold || omenCount < omenCountForPhase2)
                return;

            ResolveReferences();
            if (phase2Director != null && phase2Director.State == Phase2State.NotStarted)
            {
                phase2Director.TriggerTruthRevealFailed();
            }
        }

        private void SubscribePlayerState()
        {
            if (playerStateManager != null)
            {
                playerStateManager.OnDied += HandlePlayerDied;
            }
        }

        private void UnsubscribePlayerState()
        {
            if (playerStateManager != null)
            {
                playerStateManager.OnDied -= HandlePlayerDied;
            }
        }

        private void SubscribePhase2()
        {
            if (phase2Director != null)
            {
                phase2Director.OnPhase2Started += HandlePhase2Started;
            }
        }

        private void UnsubscribePhase2()
        {
            if (phase2Director != null)
            {
                phase2Director.OnPhase2Started -= HandlePhase2Started;
            }
        }

        private void HandlePhase2Started(Phase2Route route)
        {
            AudioManager.PlayBgm(BgmEnum.Phase2);
            SetPhase(InGamePhase.Phase2);
        }

        private void HandlePlayerDied()
        {
            EndGame(false);
        }

        private void SetPhase(InGamePhase nextPhase)
        {
            if (phase == nextPhase)
                return;

            phase = nextPhase;
            OnPhaseChanged?.Invoke(phase);
        }
    }
}
