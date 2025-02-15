using TheSleepyKoala.Map;
using UnityEngine;

namespace TheSleepyKoala.Entities
{
    /// <summary>
    /// A generic class to represent players, enemies, items, etc.
    /// </summary>
    public class Entity : MonoBehaviour
    {
        [field: SerializeField] public bool BlocksMovement { get; set; }
        [field: SerializeField] public SpriteRenderer SpriteRenderer { get; private set; }

        private Vector2 newPosition = Vector2.zero;

        public virtual void AddToGameManager()
        {
            if (GameManager.instance.Entities.Contains(this))
            {
                return;
            }

            if (GetComponent<Player>())
            {
                GameManager.instance.AddOrInsertEntity(this, 0);
            }
            else
            {
                GameManager.instance.AddOrInsertEntity(this);
            }
        }

        public void Move(Vector2 direction)
        {
            if (!MapManager.instance.IsValidPosition(transform.position + (Vector3)direction))
            {
                return;
            }

            if (GameManager.instance.GetActorAtLocation(transform.position + (Vector3)direction))
            {
                return;
            }

            transform.position += (Vector3)direction;
        }

        private void FixedUpdate()
        {
            if (GetComponent<Player>())
                Camera.main.transform.position = Vector3.Lerp(Camera.main.transform.position, new Vector3(transform.position.x, transform.position.y, -10), GetComponent<Player>().playerCameraSyncSpeed);
        }

        public virtual EntityState SaveState() => new EntityState();
    }

    [System.Serializable]
    public class EntityState
    {
        public enum EntityType
        {
            Actor,
            Item,
            Other
        }
        [field: SerializeField] public EntityType Type { get; set; }
        [field: SerializeField] public string Name { get; set; }
        [field: SerializeField] public bool BlocksMovement { get; set; }
        [field: SerializeField] public bool IsVisible { get; set; }
        [field: SerializeField] public Vector3 Position { get; set; }

        public EntityState(EntityType type = EntityType.Other, string name = "", bool blocksMovement = false, bool isVisible = false, Vector3 position = new Vector3())
        {
            this.Type = type;
            this.Name = name;
            this.BlocksMovement = blocksMovement;
            this.IsVisible = isVisible;
            this.Position = position;
        }
    }
}