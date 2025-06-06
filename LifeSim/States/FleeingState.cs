using System.Numerics;
using LifeSim.Entities;
using LifeSim.Utils;

namespace LifeSim.States;

public class FleeingState(Animal? threat) : IAnimalState
{
    public AnimalState State => AnimalState.Idle;
    private float _fleeDurationLeft;

    public void Enter(Animal animal)
    {
        animal.TargetEntity = null;
        _fleeDurationLeft = 3f + RandomUtils.RNG.NextSingle() * 2f;
    }

    public void Update(Animal animal, float deltaTime)
    {
        if (threat == null || threat.MarkedForDeletion)
        {
            animal.StateMachine.TransitionTo(new IdleState());
            return;
        }

        _fleeDurationLeft -= deltaTime;

        var awayDir = Vector2.Normalize(animal.Position - threat.Position);
        if (!float.IsNaN(awayDir.X) && !float.IsNaN(awayDir.Y))
        {
            animal.MoveTowards(animal.Position + awayDir * animal.Speed * deltaTime, deltaTime);
        }

        var safeDistance = animal.Size * 5f;
        if (Vector2.DistanceSquared(animal.Position, threat.Position) > safeDistance * safeDistance ||
            _fleeDurationLeft <= 0f)
        {
            animal.StateMachine.TransitionTo(new IdleState());
        }
    }

    public void Exit(Animal animal)
    {
    }
}