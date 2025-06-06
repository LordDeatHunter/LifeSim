using LifeSim.Entities;

namespace LifeSim.States;

public class IdleState : IAnimalState
{
    public AnimalState State => AnimalState.Idle;

    public void Enter(Animal animal)
    {
        animal.TargetEntity = null;
    }

    public void Update(Animal animal, float deltaTime)
    {
        var predator = animal.FindNearestPredator();
        if (predator != null)
        {
            animal.StateMachine.TransitionTo(new FleeingState(predator));
            return;
        }
        
        if (animal.CanReproduce())
        {
            animal.StateMachine.TransitionTo(new SeekingMateState());
        }
        else
        {
            animal.StateMachine.TransitionTo(new SeekingFoodState());
        }
    }
 
    public void Exit(Animal animal)
    {
    }
}