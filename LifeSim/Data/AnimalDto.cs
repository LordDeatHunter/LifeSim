﻿namespace LifeSim.Data;

public record AnimalDto(string id, float x, float y, string color, float size, float predationInclanation, bool alive) : IEntityDto;