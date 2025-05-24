using LifeSim.Entities;

namespace LifeSim.States;

public class AnimalStateMachine
{
    private IAnimalState _currentState;
    private readonly Animal _animal;

    public AnimalStateMachine(Animal animal)
    {
        _animal = animal;
        TransitionTo(new IdleState());
    }

    public void Update(float deltaTime)
    {
        _currentState.Update(_animal, deltaTime);
    }

    public void TransitionTo(IAnimalState next)
    {
        _currentState?.Exit(_animal);
        _currentState = next;
        _currentState.Enter(_animal);
    }

    public AnimalState Current => _currentState.State;

    public void SetMatingTarget(Animal target)
    {
        TransitionTo(new SeekingMateState());
        _animal.Target = target;
    }
}