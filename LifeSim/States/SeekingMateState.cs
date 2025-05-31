using LifeSim.Entities;

namespace LifeSim.States;

public class SeekingMateState : IAnimalState
{
    public AnimalState State => AnimalState.SeekingMate;

    public void Enter(Animal animal)
    {
        animal.Target = animal.FindNearestMate();
        if (animal.Target is not Animal mate) return;
        if (mate.StateMachine.Current == AnimalState.SeekingMate && mate.Target == animal) return;
        mate.StateMachine.SetMatingTarget(animal);
    }

    public void Update(Animal animal, float deltaTime)
    {
        if (animal.Target is not Animal mate)
        {
            animal.StateMachine.TransitionTo(new IdleState());
            return;
        }

        if (animal.Saturation <= animal.HungerThresholdValue)
        {
            animal.StateMachine.TransitionTo(new SeekingFoodState());
            return;
        }

        if (animal.CanMateWith(mate))
        {
            animal.StateMachine.TransitionTo(new MatingState());
        }
        else
        {
            animal.MoveTowards(mate.Position, deltaTime);
        }
    }

    public void Exit(Animal animal)
    {
    }
}