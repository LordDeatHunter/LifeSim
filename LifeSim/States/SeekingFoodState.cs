using LifeSim.Entities;

namespace LifeSim.States;

public class SeekingFoodState : IAnimalState
{
    public AnimalState State => AnimalState.SeekingFood;

    public void Enter(Animal animal)
    {
        animal.Target = animal.FindNearestTarget();
    }

    public void Update(Animal animal, float deltaTime)
    {
        if (animal.Target == null)
        {
            animal.StateMachine.TransitionTo(new IdleState());
            return;
        }

        var to = animal.Target.Position - animal.Position;
        if (to.LengthSquared() < animal.EatRangeSquared)
        {
            animal.StateMachine.TransitionTo(new EatingState());
        }
        else
        {
            animal.MoveTowards(animal.Target.Position, deltaTime);
        }
    }

    public void Exit(Animal animal)
    {
    }
}