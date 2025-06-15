using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using LifeSim.Entities;
using LifeSim.Utils;

namespace LifeSim.Data.Models;

[Table("Animals")]
public class AnimalEntity
{
    [Key]
    public int Id { get; set; }

    public float X { get; set; }
    public float Y { get; set; }

    [Required, StringLength(7)]
    public string ColorHex { get; set; } = null!;

    public float Size { get; set; }
    public float PredationInclination { get; set; }
    public float Saturation { get; set; }
    public float ReproductionCooldown { get; set; }
    public float Speed { get; set; }
    public float Age { get; set; }
    public float Lifespan { get; set; }
    
    public static Animal FromDomain(AnimalEntity animalEntity)
    {
        var position = new Vector2(animalEntity.X, animalEntity.Y);
        var color = ColorUtils.FromHex(animalEntity.ColorHex);
        var animal = new Animal(position, animalEntity.Size, color)
        {
            Speed = animalEntity.Speed,
            ReproductionCooldown = animalEntity.ReproductionCooldown,
            PredationInclination = animalEntity.PredationInclination,
            Saturation = animalEntity.Saturation,
            Age = animalEntity.Age,
            Lifespan = animalEntity.Lifespan
        };
        return animal;
    }

    public static AnimalEntity ToDomain(Animal animal) =>
        new()
        {
            Id = animal.Id,
            X = animal.Position.X,
            Y = animal.Position.Y,
            ColorHex = animal.Color.ToHex(),
            Size = animal.Size,
            Speed = animal.Speed,
            ReproductionCooldown = animal.ReproductionCooldown,
            PredationInclination = animal.PredationInclination,
            Saturation = animal.Saturation,
            Age = animal.Age,
            Lifespan = animal.Lifespan
        };
}