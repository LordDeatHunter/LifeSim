using LifeSim.Entities;

namespace LifeSim.States;

public class MatingState : IAnimalState
{
    public AnimalState State => AnimalState.Mating;

    public void Enter(Animal animal)
    {
        if (animal.Target is not Animal mate) return;
        animal.HandleReproductionTarget(mate);
    }

    public void Update(Animal animal, float deltaTime)
    {
        animal.StateMachine.TransitionTo(new IdleState());
    }

    public void Exit(Animal animal)
    {
    }
}