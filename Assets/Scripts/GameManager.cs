using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using TheSleepyKoala.Entities;
using TheSleepyKoala.Map;
using TMPro;
using static UnityEngine.GraphicsBuffer;

namespace TheSleepyKoala
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager instance;

        [Header("Time")]
        [SerializeField] private float baseTime = 0.075f;
        [field: SerializeField] public bool IsPlayerTurn { get; set; } = true;

        [field: Header("Entities & Actors")]
        [field: SerializeField] public List<Entity> Entities { get; private set; }
        [field: SerializeField] public List<Actor> Actors { get; private set; }
        private Queue<Actor> actorQueue = new Queue<Actor>();

        [field: Header("Death")]
        [field: SerializeField] public Color DeadColor { get; private set; }
        [field: SerializeField] public Sprite DeadSprite { get; private set; }

        [field: Header("LevelUp")]
        [field: SerializeField] public GameObject LevelUpScreen;
        [field: SerializeField] public TMP_Text ConText;
        [field: SerializeField] public TMP_Text StrText;
        [field: SerializeField] public TMP_Text DefText;

        private void Awake()
        {
            Application.targetFrameRate = 60;
            if (instance == null)
            {
                instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void Update()
        {
            if (LevelUpScreen.activeSelf)
            {
                ConText.text = $" - Constitution ({Actors[0].Fighter.MaxHp} + {Mathf.CeilToInt(Actors[0].Fighter.MaxHp * 0.2f)})";
                StrText.text = $" - Strength ({Actors[0].Fighter.Power()} + 1)";
                DefText.text = $" - Defense ({Actors[0].Fighter.Defense()} + 1)";
            }
            

            timeSinceLastPlayerTurn += Time.deltaTime;
        }

        public void IncreaseCon()
        {
            Actors[0].Fighter.MaxHp += Mathf.CeilToInt(Actors[0].Fighter.MaxHp * 0.2f);
        }

        public void IncreaseStr()
        {
            Actors[0].Fighter.State.BasePower += 1;
        }

        public void IncreaseDef()
        {
            Actors[0].Fighter.State.BaseDefense += 1;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneState sceneState = SaveManager.instance.Save.Scenes.Find(x => x.FloorNumber == SaveManager.instance.CurrentFloor);

            if (sceneState is not null)
            {
                LoadState(sceneState.GameState, true);
            }
            else
            {
                Entities = new List<Entity>();
                Actors = new List<Actor>();
            }

            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        float timeSinceLastPlayerTurn = 0;
        private IEnumerator StartTurn()
        {

            Actor actor = actorQueue.Peek();
            if (actor.GetComponent<Player>())
            {
                if (timeSinceLastPlayerTurn < baseTime)
                    yield return new WaitForSeconds(baseTime - timeSinceLastPlayerTurn);
                IsPlayerTurn = true;
            }
            else
            {
                yield return null;

                if (actor.AI != null)
                {
                    actor.AI.RunAI();
                }
                else
                {
                    Action.WaitAction();
                }
            }
        }

        public void EndTurn()
        {
            Actor actor = actorQueue.Dequeue();

            if (actor.GetComponent<Player>())
            {
                IsPlayerTurn = false;
                timeSinceLastPlayerTurn = 0;

                actorQueue.Enqueue(actor);
            }
            else
            {
                actorQueue.Enqueue(actor); // End this actor's turn before finding next actor

                actor = actorQueue.Peek(); // Look at next item

                if (Vector2.Distance(Actors[0].transform.position, actor.transform.position) > 6) // too far away
                {
                    while (!actor.GetComponent<Player>()) // If next item isn't player
                    {
                        if (Vector2.Distance(Actors[0].transform.position, actor.transform.position) > 6) // and if too far away
                        {
                            actor = actorQueue.Dequeue();
                            actorQueue.Enqueue(actor);
                        }
                        else
                            break;
                        actor = actorQueue.Peek(); // Look at next item
                    }
                }
            }

            StartCoroutine(StartTurn());
        }


        public void AddOrInsertEntity(Entity entity, int index = -1)
        {
            if (!entity.gameObject.activeSelf)
            {
                entity.gameObject.SetActive(true);
            }

            if (index < 0)
            {
                Entities.Add(entity);
            }
            else
            {
                Entities.Insert(index, entity);
            }
        }

        public void RemoveEntity(Entity entity)
        {
            entity.gameObject.SetActive(false);
            Entities.Remove(entity);
        }

        public void AddOrInsertActor(Actor actor, int index = -1)
        {
            if (index < 0)
            {
                Actors.Add(actor);
            }
            else
            {
                Actors.Insert(index, actor);
            }

            actorQueue.Enqueue(actor);
        }

        public void RemoveActor(Actor actor)
        {
            if (actor.GetComponent<Player>())
            {
                return;
            }
            Actors.Remove(actor);
            actorQueue = new Queue<Actor>(actorQueue.Where(x => x != actor));
        }

        public void RefreshPlayer()
        {
            Actors[0].UpdateFieldOfView();
        }

        public Actor GetActorAtLocation(Vector3 location)
        {
            foreach (Actor actor in Actors)
            {
                if (actor.BlocksMovement && actor.transform.position == location)
                {
                    return actor;
                }
            }
            return null;
        }

        public GameState SaveState()
        {
            foreach (Item item in Actors[0].Inventory.Items)
            {
                AddOrInsertEntity(item);
            }

            GameState gameState = new GameState(entities: Entities.ConvertAll(x => x.SaveState()));

            foreach (Item item in Actors[0].Inventory.Items)
            {
                RemoveEntity(item);
            }

            return gameState;
        }

        public void LoadState(GameState state, bool canRemovePlayer)
        {
            IsPlayerTurn = false; //Prevents player from moving during load

            Reset(canRemovePlayer);
            StartCoroutine(LoadEntityStates(state.entityStates, canRemovePlayer));
        }

        private IEnumerator LoadEntityStates(List<EntityState> entityStates, bool canPlacePlayer)
        {
            int entityState = 0;
            while (entityState < entityStates.Count)
            {
                yield return new WaitForEndOfFrame();

                if (entityStates[entityState].Type == EntityState.EntityType.Actor)
                {
                    ActorState actorState = entityStates[entityState] as ActorState;

                    string entityName = entityStates[entityState].Name.Contains("Remains of") ?
                      entityStates[entityState].Name.Substring(entityStates[entityState].Name.LastIndexOf(' ') + 1) : entityStates[entityState].Name;

                    if (entityName == "Player" && !canPlacePlayer)
                    {
                        Actors[0].transform.position = entityStates[entityState].Position;
                        entityState++;
                        continue;
                    }

                    Actor actor = MapManager.instance.CreateEntity(entityName, actorState.Position).GetComponent<Actor>();

                    actor.LoadState(actorState);
                }
                else if (entityStates[entityState].Type == EntityState.EntityType.Item)
                {
                    ItemState itemState = entityStates[entityState] as ItemState;

                    string entityName = entityStates[entityState].Name.Contains("(E)") ?
                      entityStates[entityState].Name.Replace(" (E)", "") : entityStates[entityState].Name;

                    if (itemState.Parent == "Player" && !canPlacePlayer)
                    {
                        entityState++;
                        continue;
                    }

                    Item item = MapManager.instance.CreateEntity(entityName, itemState.Position).GetComponent<Item>();

                    item.LoadState(itemState);
                }

                entityState++;
            }

            RefreshPlayer();
            IsPlayerTurn = true; //Allows player to move after load
        }

        public void Reset(bool canRemovePlayer)
        {
            if (Entities.Count > 0)
            {
                for (int i = 0; i < Entities.Count; i++)
                {
                    if (!canRemovePlayer && Entities[i].GetComponent<Player>())
                    {
                        continue;
                    }

                    Destroy(Entities[i].gameObject);
                }

                if (canRemovePlayer)
                {
                    Entities.Clear();
                    Actors.Clear();
                    actorQueue.Clear();
                }
                else
                {
                    Entities.RemoveAll(x => x.GetComponent<Player>() == null);
                    Actors.RemoveAll(x => x.GetComponent<Player>() == null);
                    actorQueue = new Queue<Actor>(actorQueue.Where(x => x.GetComponent<Player>()));
                }
            }
        }
    }

    [System.Serializable]
    public class GameState
    {
        [field: SerializeField] public List<EntityState> entityStates { get; set; }

        public GameState(List<EntityState> entities)
        {
            this.entityStates = entities;
        }
    }
}
