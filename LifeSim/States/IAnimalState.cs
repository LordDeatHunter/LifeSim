using LifeSim.Entities;

namespace LifeSim.States;

public interface IAnimalState
{
    AnimalState State { get; }
    void Enter(Animal animal);
    void Update(Animal animal, float deltaTime);
    void Exit(Animal animal);
}