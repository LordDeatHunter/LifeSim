using LifeSim.Entities;

namespace LifeSim.Components;

public interface IComponent
{
    void Update(Entity entity, float deltaTime);
}