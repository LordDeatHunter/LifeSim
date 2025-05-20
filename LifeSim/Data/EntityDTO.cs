namespace LifeSim.Data;

public record FoodDto(string type, string id, float x, float y, string color, float size);
public record AnimalDto(string type, string id, float x, float y, string color, float size, FoodType foodType);
