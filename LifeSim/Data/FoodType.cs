using LifeSim.Entities;

namespace LifeSim.Data;

public enum FoodType
{
    OMNIVORE,
    HERBIVORE,
    CARNIVORE,
}

public static class FoodTypeExtensions
{
    public static FoodType GetRandomForOffspring(Animal parent1, Animal parent2)
    {
        var parentFoodType = GetRandomFromParents(parent1, parent2);
        var mutationChance = (parent1.FoodType == parent2.FoodType ? 0.025F : 0.1F) * (Program.RNG.NextSingle() * 2F - 1F);

        if (Program.RNG.NextSingle() > mutationChance) return parentFoodType;

        return parentFoodType switch
        {
            FoodType.OMNIVORE => Program.RNG.NextSingle() < 0.5F ? FoodType.HERBIVORE : FoodType.CARNIVORE,
            FoodType.HERBIVORE => Program.RNG.NextSingle() < 0.5F ? FoodType.OMNIVORE : FoodType.CARNIVORE,
            FoodType.CARNIVORE => Program.RNG.NextSingle() < 0.5F ? FoodType.OMNIVORE : FoodType.HERBIVORE,
            _ => parentFoodType
        };
    }

    public static FoodType GetRandomFromParents(Animal parent1, Animal parent2)
    {
        var rng = Program.RNG.NextSingle();

        return rng switch
        {
            < 0.40F => parent1.FoodType,
            < 0.80F => parent2.FoodType,
            _ => GetCombinedFoodType(parent1.FoodType, parent2.FoodType)
        };
    }

    public static FoodType GetCombinedFoodType(this FoodType foodType, FoodType otherFoodType)
    {
        if (foodType == otherFoodType) return foodType;

        if (foodType == FoodType.OMNIVORE || otherFoodType == FoodType.OMNIVORE ||
            (foodType == FoodType.HERBIVORE && otherFoodType == FoodType.CARNIVORE) ||
            (foodType == FoodType.CARNIVORE && otherFoodType == FoodType.HERBIVORE)) return FoodType.OMNIVORE;

        return otherFoodType;
    }
}