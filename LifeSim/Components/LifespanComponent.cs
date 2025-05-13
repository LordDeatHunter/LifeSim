using LifeSim.Entities;

namespace LifeSim.Components;

public class LifespanComponent(float lifespan) : IComponent
{
    public float Lifespan { get; set; } = lifespan;
    public float Age { get; set; } = 0;

    public void Update(Entity entity, float deltaTime)
    {
        Age += deltaTime;
        if (Age >= Lifespan)
        {
            entity.MarkForDeletion();
        }
    }
}