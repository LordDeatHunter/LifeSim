namespace LifeSim.Data;

public record FoodDto(string id, float x, float y, string color, float size, bool infected) : IEntityDto;
