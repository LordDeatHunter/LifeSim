using System.Text.Json.Serialization;

namespace LifeSim.Data;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AnimalDto), "animal")]
[JsonDerivedType(typeof(FoodDto),  "food")]
public interface IEntityDto;