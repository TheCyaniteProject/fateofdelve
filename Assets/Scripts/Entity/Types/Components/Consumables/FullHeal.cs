using UnityEngine;

namespace TheSleepyKoala.Entities
{
    [RequireComponent(typeof(Item))]
    public class FullHeal : Consumable
    {

        public override bool Activate(Actor consumer)
        {
            int amountRecovered = consumer.Fighter.Heal(consumer.Fighter.MaxHp);

            if (amountRecovered > 0)
            {
                UIManager.instance.AddMessage($"You consume the {name}, and recover {amountRecovered} HP!", "#00FF00");
                Consume(consumer);
                return true;
            }
            else
            {
                UIManager.instance.AddMessage("Your health is already full.", "#808080");
                return false;
            }
        }
    }
}