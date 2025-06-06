using LifeSim.Entities;

namespace LifeSim.States;

public class EatingState : IAnimalState
{
    public AnimalState State => AnimalState.Eating;
    public void Enter(Animal animal)
    {
        if (animal.TargetEntity == null) return;
        animal.Consume(animal.TargetEntity);
    }
    public void Update(Animal animal, float deltaTime)
    {
        animal.StateMachine.TransitionTo(new IdleState());
    }
    public void Exit(Animal animal) { }
}