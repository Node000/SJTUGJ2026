using System.Collections.Generic;
using UnityEngine;

namespace Gameplay
{
    public class BoardManager : MonoBehaviour
    {
        public RoomCard roomPrefab;
        public RoomCardData startRoom;
        public List<RoomCardData> deck = new();

        public RoomSelectionHandler roomSelectionHandler;
        [Range(1, 3)] public int roomChoiceCount = 3;

        public float tileSize = 3f;
        public RectTransform uiRoomParent;
        public Vector2 uiOrigin;
        public float uiTileSize = 503f;
        public int mapWidth = 5;
        public int mapHeight = 5;
        public Vector2Int bossRoomPosition = new Vector2Int(4, 4);
        public int omenWeightBonusPerNonOmenRoom = 1;
        public int maxOmenWeightBonus = 8;
        public int minimumRoomsBeforeOmen = 3;

        private readonly Dictionary<Vector2Int, RoomCard> placedRooms = new();
        private readonly Dictionary<Vector2Int, RoomCard> previewRooms = new();
        private readonly List<RoomCardData> initialDeck = new();
        private readonly List<RoomCardData> candidateRooms = new();
        private readonly List<RoomPlacementOption> pendingRoomOptions = new();
        private readonly RoomPlacementOption[] reusableOptions = new RoomPlacementOption[3];
        private int currentOmenWeightBonus;
        private readonly Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.left,
            Vector2Int.down,
            Vector2Int.right
        };

        public event System.Action RoomsChanged;

        public IEnumerable<RoomCard> PlacedRooms => placedRooms.Values;
        public IEnumerable<RoomCard> PreviewRooms => previewRooms.Values;

        private void Awake()
        {
            CacheInitialDeck();

            if (roomPrefab == null)
            {
                Debug.LogError("BoardManager needs a room prefab.", this);
                return;
            }

            if (startRoom == null)
            {
                Debug.LogError("BoardManager needs a start room.", this);
                return;
            }

            PlaceRoom(startRoom, Vector2Int.zero, CreatePlacementLayout(startRoom));
        }

        private void Start()
        {
            RefreshReachablePreviews();
        }

        public RoomCard GetRoom(Vector2Int position)
        {
            placedRooms.TryGetValue(position, out RoomCard room);
            return room;
        }

        public bool TryDrawAndPlaceRoom(Vector2Int origin, Vector2Int direction, out RoomCard placedRoom)
        {
            placedRoom = null;

            if (GetRoomOptions(null, 1, origin, direction) <= 0)
                return false;

            RoomPlacementOption option = reusableOptions[0];
            return TryPlaceSelectedRoom(origin, direction, option, out placedRoom);
        }

        public bool TryPlaceSelectedRoom(Vector2Int origin, Vector2Int direction, RoomPlacementOption option, out RoomCard placedRoom)
        {
            placedRoom = null;

            if (!CanPlaceSelectedRoom(origin, direction, option, out string failureReason))
            {
                Debug.LogWarning(failureReason, this);
                return false;
            }

            RoomCard originRoom = GetRoom(origin);
            Vector2Int nextPosition = origin + direction;

            deck.Remove(option.data);

            placedRoom = PlaceRoom(option.data, nextPosition, option.doorLayout);
            if (placedRoom != null)
            {
                AudioManager.PlaySfx(SfxEnum.PlaceRoom);
                UpdateOmenWeightAfterPlacement(option.data);
                ClearPendingRoomOptions();
                placedRoom.remainingDoors = Mathf.Max(0, placedRoom.remainingDoors - 1);
                originRoom.remainingDoors = Mathf.Max(0, originRoom.remainingDoors - 1);
                RefreshReachablePreviews();
            }

            return placedRoom != null;
        }

        public bool CanPlaceSelectedRoom(Vector2Int origin, Vector2Int direction, RoomPlacementOption option, out string failureReason)
        {
            if (!CanPlaceFromOrigin(origin, direction, out failureReason))
                return false;

            if (option == null || option.data == null)
            {
                failureReason = "选择的房间为空。";
                return false;
            }

            if (!ContainsPendingOption(option.data))
            {
                failureReason = "选择的房间已经不在当前候选组中。";
                return false;
            }

            if (!option.HasDoor(GetOppositeDirection(direction)))
            {
                failureReason = "选择的房间没有与当前门对齐。";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public int GetRoomChoices(List<RoomCardData> results, int count, Vector2Int origin, Vector2Int direction)
        {
            if (results == null)
                return 0;

            results.Clear();
            int optionCount = GetRoomOptions(null, count, origin, direction);
            for (int i = 0; i < optionCount; i++)
            {
                results.Add(reusableOptions[i].data);
            }

            return results.Count;
        }

        public int GetRoomOptions(List<RoomPlacementOption> results, int count, Vector2Int origin, Vector2Int direction)
        {
            if (results != null)
            {
                results.Clear();
            }

            if (pendingRoomOptions.Count == 0)
            {
                GeneratePendingRoomOptions(count, origin, direction);
            }

            int optionCount = Mathf.Min(pendingRoomOptions.Count, count, reusableOptions.Length);
            for (int i = 0; i < optionCount; i++)
            {
                reusableOptions[i] = pendingRoomOptions[i];
                if (results != null)
                {
                    results.Add(reusableOptions[i]);
                }
            }

            return optionCount;
        }

        private void GeneratePendingRoomOptions(int count, Vector2Int origin, Vector2Int direction)
        {
            pendingRoomOptions.Clear();

            if (deck.Count < Mathf.Max(1, roomChoiceCount))
            {
                ResetDeck();
            }

            if (deck.Count == 0)
            {
                ResetDeck();
            }

            if (deck.Count == 0 || count <= 0 || !CanPlaceFromOrigin(origin, direction, out _))
                return;

            FillCandidates();
            if (candidateRooms.Count == 0)
            {
                ResetDeck();
                FillCandidates();
            }

            if (candidateRooms.Count == 0)
                return;

            count = Mathf.Clamp(count, 1, reusableOptions.Length);
            int attempts = candidateRooms.Count * 3;

            while (pendingRoomOptions.Count < count && attempts > 0)
            {
                attempts--;
                RoomCardData card = candidateRooms[Random.Range(0, candidateRooms.Count)];
                if (card == null || ContainsPendingOption(card))
                    continue;

                pendingRoomOptions.Add(CreatePlacementOption(card, direction));
            }
        }

        public bool TryPlaceFixedRoom(RoomCardData card, Vector2Int gridPosition, out RoomCard placedRoom)
        {
            return TryPlaceFixedRoom(card, gridPosition, Vector2Int.zero, out placedRoom);
        }

        public bool TryPlaceFixedRoom(RoomCardData card, Vector2Int gridPosition, Vector2Int entranceDirection, out RoomCard placedRoom)
        {
            placedRoom = null;

            if (card == null)
            {
                Debug.LogWarning("Cannot place fixed room: room data is null.", this);
                return false;
            }

            if (roomPrefab == null)
            {
                Debug.LogWarning("Cannot place fixed room: room prefab is null.", this);
                return false;
            }

            if (!IsInsideMap(gridPosition))
            {
                Debug.LogWarning($"Cannot place fixed room at {gridPosition}: position is outside map bounds.", this);
                return false;
            }

            if (gridPosition != bossRoomPosition && IsReservedForBoss(gridPosition))
            {
                Debug.LogWarning($"Cannot place fixed room at {gridPosition}: position is reserved for the boss room.", this);
                return false;
            }

            if (placedRooms.ContainsKey(gridPosition))
            {
                Debug.LogWarning($"Cannot place fixed room at {gridPosition}: position is already occupied.", this);
                return false;
            }

            RoomDoorLayout layout = entranceDirection == Vector2Int.zero
                ? CreatePlacementLayout(card)
                : CreateDoorLayout(card, entranceDirection);
            placedRoom = PlaceRoom(card, gridPosition, layout);
            if (placedRoom != null)
            {
                AudioManager.PlaySfx(SfxEnum.PlaceRoom);
            }

            RefreshReachablePreviews();
            return placedRoom != null;
        }

        public bool HasRoom(Vector2Int position)
        {
            return placedRooms.ContainsKey(position);
        }

        public bool HasReachablePreview(Vector2Int position)
        {
            return previewRooms.ContainsKey(position);
        }

        public bool CanPlaceRoom(Vector2Int origin, Vector2Int direction, out string failureReason)
        {
            if (!CanPlaceFromOrigin(origin, direction, out failureReason))
                return false;

            FillCandidates();
            if (candidateRooms.Count == 0)
            {
                ResetDeck();
                FillCandidates();
            }

            if (candidateRooms.Count == 0)
            {
                failureReason = "随机池里没有可放置的房间。";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        public void RefreshReachablePreviews()
        {
            ClearPreviewRooms();

            foreach (var pair in placedRooms)
            {
                RoomCard room = pair.Value;
                if (room == null)
                    continue;

                for (int i = 0; i < directions.Length; i++)
                {
                    Vector2Int direction = directions[i];
                    Vector2Int position = pair.Key + direction;
                    if (!room.HasDoor(direction) || !IsInsideMap(position) || IsReservedForBoss(position) || placedRooms.ContainsKey(position) || previewRooms.ContainsKey(position))
                        continue;

                    RoomCard preview = CreateRoomInstance(position);
                    preview.InitPreview(position);
                    previewRooms.Add(position, preview);
                }
            }

            RoomsChanged?.Invoke();
        }

        public Vector3 GridToWorldPosition(Vector2Int gridPosition)
        {
            return new Vector3(
                gridPosition.x * tileSize,
                gridPosition.y * tileSize,
                0f
            );
        }

        public Vector2 GridToUiAnchoredPosition(Vector2Int gridPosition)
        {
            return uiOrigin + new Vector2(
                gridPosition.x * uiTileSize,
                gridPosition.y * uiTileSize
            );
        }

        private RoomCard PlaceRoom(RoomCardData card, Vector2Int gridPosition, RoomDoorLayout layout)
        {
            RemovePreview(gridPosition);
            RoomCard room = CreateRoomInstance(gridPosition);
            room.Init(card, gridPosition, layout);
            placedRooms.Add(gridPosition, room);
            return room;
        }

        private RoomCard CreateRoomInstance(Vector2Int gridPosition)
        {
            RoomCard room;

            if (uiRoomParent != null)
            {
                room = Instantiate(roomPrefab, uiRoomParent);
                RectTransform rectTransform = room.GetComponent<RectTransform>();
                if (rectTransform != null)
                {
                    rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = GridToUiAnchoredPosition(gridPosition);
                    rectTransform.localScale = Vector3.one;
                }
            }
            else
            {
                Vector3 worldPosition = GridToWorldPosition(gridPosition);
                room = Instantiate(roomPrefab, worldPosition, Quaternion.identity);
            }

            return room;
        }

        private void FillCandidates()
        {
            candidateRooms.Clear();

            for (int i = 0; i < deck.Count; i++)
            {
                RoomCardData card = deck[i];
                if (card == null || card.doorCount <= 0 || ShouldExcludeFromCandidates(card))
                    continue;

                int copies = IsOmenRoom(card) ? 1 + currentOmenWeightBonus : 1;
                for (int copyIndex = 0; copyIndex < copies; copyIndex++)
                {
                    candidateRooms.Add(card);
                }
            }
        }

        private bool ShouldExcludeFromCandidates(RoomCardData card)
        {
            return IsOmenRoom(card) && GetGeneratedRoomCount() < Mathf.Max(0, minimumRoomsBeforeOmen);
        }

        private int GetGeneratedRoomCount()
        {
            return Mathf.Max(0, placedRooms.Count - 1);
        }

        private void CacheInitialDeck()
        {
            initialDeck.Clear();
            for (int i = 0; i < deck.Count; i++)
            {
                if (deck[i] != null && !initialDeck.Contains(deck[i]))
                {
                    initialDeck.Add(deck[i]);
                }
            }
        }

        private void ResetDeck()
        {
            for (int i = 0; i < initialDeck.Count; i++)
            {
                if (initialDeck[i] != null && !deck.Contains(initialDeck[i]))
                {
                    deck.Add(initialDeck[i]);
                }
            }
        }

        private void UpdateOmenWeightAfterPlacement(RoomCardData card)
        {
            if (IsOmenRoom(card))
            {
                currentOmenWeightBonus = 0;
                return;
            }

            currentOmenWeightBonus = Mathf.Min(maxOmenWeightBonus, currentOmenWeightBonus + Mathf.Max(0, omenWeightBonusPerNonOmenRoom));
        }

        private static bool IsOmenRoom(RoomCardData card)
        {
            return card != null && card.eventData != null && card.eventData.category == RoomEventCategory.Omen;
        }

        private bool CanPlaceFromOrigin(Vector2Int origin, Vector2Int direction, out string failureReason)
        {
            RoomCard originRoom = GetRoom(origin);
            Vector2Int nextPosition = origin + direction;

            if (originRoom == null)
            {
                failureReason = "当前格子没有房间。";
                return false;
            }

            if (!IsInsideMap(nextPosition))
            {
                failureReason = "目标位置超出地图边界。";
                return false;
            }

            if (IsReservedForBoss(nextPosition))
            {
                failureReason = "右上角是院长室，不能生成普通房间。";
                return false;
            }

            if (originRoom.remainingDoors <= 0)
            {
                failureReason = "当前房间没有剩余可连接的门。";
                return false;
            }

            if (!originRoom.HasDoor(direction))
            {
                failureReason = "当前方向没有门，无法放置房间。";
                return false;
            }

            if (placedRooms.ContainsKey(nextPosition))
            {
                failureReason = "目标位置已经有房间。";
                return false;
            }

            failureReason = string.Empty;
            return true;
        }

        private RoomPlacementOption CreatePlacementOption(RoomCardData card, Vector2Int entranceDirection)
        {
            return new RoomPlacementOption
            {
                data = card,
                doorLayout = CreatePlacementLayout(card)
            };
        }

        private RoomDoorLayout CreateDoorLayout(RoomCardData card, Vector2Int entranceDirection)
        {
            RoomDoorLayout layout = default;
            Vector2Int requiredDoor = GetOppositeDirection(entranceDirection);
            layout.SetDoor(requiredDoor, true);

            int targetDoorCount = Mathf.Clamp(card != null ? card.doorCount : 1, 1, 4);
            int guard = 16;
            while (layout.Count < targetDoorCount && guard > 0)
            {
                guard--;
                Vector2Int direction = directions[Random.Range(0, directions.Length)];
                layout.SetDoor(direction, true);
            }

            return layout;
        }

        private RoomDoorLayout CreatePlacementLayout(RoomCardData card)
        {
            return card != null && card.useFixedDoorLayout
                ? RoomDoorLayout.FromData(card)
                : CreateRandomDoorLayout(card);
        }

        private RoomDoorLayout CreateRandomDoorLayout(RoomCardData card)
        {
            RoomDoorLayout layout = default;
            int targetDoorCount = Mathf.Clamp(card != null ? card.doorCount : 1, 1, 4);
            int guard = 16;
            while (layout.Count < targetDoorCount && guard > 0)
            {
                guard--;
                Vector2Int direction = directions[Random.Range(0, directions.Length)];
                layout.SetDoor(direction, true);
            }

            return layout;
        }

        private bool ContainsPendingOption(RoomCardData card)
        {
            for (int i = 0; i < pendingRoomOptions.Count; i++)
            {
                if (pendingRoomOptions[i] != null && pendingRoomOptions[i].data == card)
                    return true;
            }

            return false;
        }

        private void ClearPendingRoomOptions()
        {
            pendingRoomOptions.Clear();
        }

        private void ClearPreviewRooms()
        {
            foreach (var pair in previewRooms)
            {
                if (pair.Value != null)
                {
                    Destroy(pair.Value.gameObject);
                }
            }

            previewRooms.Clear();
        }

        private void RemovePreview(Vector2Int gridPosition)
        {
            if (!previewRooms.TryGetValue(gridPosition, out RoomCard preview))
                return;

            if (preview != null)
            {
                Destroy(preview.gameObject);
            }

            previewRooms.Remove(gridPosition);
        }

        private bool IsInsideMap(Vector2Int position)
        {
            return position.x >= 0
                && position.y >= 0
                && position.x < Mathf.Max(1, mapWidth)
                && position.y < Mathf.Max(1, mapHeight);
        }

        private bool IsReservedForBoss(Vector2Int position)
        {
            return position == bossRoomPosition;
        }

        private static Vector2Int GetOppositeDirection(Vector2Int direction)
        {
            if (direction == Vector2Int.up)
                return Vector2Int.down;

            if (direction == Vector2Int.down)
                return Vector2Int.up;

            if (direction == Vector2Int.left)
                return Vector2Int.right;

            if (direction == Vector2Int.right)
                return Vector2Int.left;

            return Vector2Int.zero;
        }
    }
}
