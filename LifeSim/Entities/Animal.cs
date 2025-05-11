using System.Drawing;

namespace LifeSim.Entities;

public class Animal(float x, float y) : Entity(x, y, Color.CornflowerBlue)
{
    public override void Update(float deltaTime)
    {
        X += (float)(new Random().NextDouble() - 0.5) * 5;
        Y += (float)(new Random().NextDouble() - 0.5) * 5;

        X = Math.Clamp(X, 0, 1000);
        Y = Math.Clamp(Y, 0, 1000);
    }
}