namespace LifeSim.Data;

public record AnimalDto(string id, float x, float y, string color, float size, string foodType) : IEntityDto;