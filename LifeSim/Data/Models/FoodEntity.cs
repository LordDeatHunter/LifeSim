using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using LifeSim.Entities;
using LifeSim.Utils;

namespace LifeSim.Data.Models;

[Table("Foods")]
public class FoodEntity
{
    [Key]
    public int Id { get; set; }

    public float X { get; set; }
    public float Y { get; set; }

    [Required, StringLength(7)]
    public string ColorHex { get; set; } = null!;

    public float Size { get; set; }

    public float Age { get; set; }
    public float Lifespan { get; set; }
    
    public static FoodEntity ToDomain(Food food) =>
        new()
        {
            Id = food.Id,
            X = food.Position.X,
            Y = food.Position.Y,
            ColorHex = food.Color.ToHex(),
            Size = food.Size,
            Age = food.Age,
            Lifespan = food.Lifespan
        };

    public static Food FromDomain(FoodEntity foodEntity)
    {
        var position = new Vector2(foodEntity.X, foodEntity.Y);
        var color = ColorUtils.FromHex(foodEntity.ColorHex);
        var food = new Food(position)
        {
            Color = color,
            Size = foodEntity.Size,
            Age = foodEntity.Age,
            Lifespan = foodEntity.Lifespan
        };
        return food;
    }
}