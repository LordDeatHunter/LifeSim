using LifeSim.Entities;

namespace LifeSim.States;

public class SeekingFoodState : IAnimalState
{
    public AnimalState State => AnimalState.SeekingFood;

    public void Enter(Animal animal)
    {
        animal.TargetEntity = animal.FindNearestTarget();
    }

    public void Update(Animal animal, float deltaTime)
    {
        var predator = animal.FindNearestPredator();
        if (predator != null)
        {
            animal.StateMachine.TransitionTo(new FleeingState(predator));
            return;
        }
        
        if (animal.TargetEntity == null)
        {
            animal.StateMachine.TransitionTo(new IdleState());
            return;
        }
        
        var targetPosition = animal.TargetEntity.Position;

        var to = targetPosition - animal.Position;
        if (to.LengthSquared() < animal.EatRangeSquared)
        {
            animal.StateMachine.TransitionTo(new EatingState());
        }
        else
        {
            animal.MoveTowards(targetPosition, deltaTime);
        }
    }

    public void Exit(Animal animal)
    {
    }
}