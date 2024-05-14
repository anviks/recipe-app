using Base.Domain;

namespace App.DTO.v1_0;

public class RecipeIngredient : BaseEntityId
{
    public string? CustomUnit { get; set; }
    public float Quantity { get; set; }
    public string? IngredientModifier { get; set; } = default!;
    public Guid UnitId { get; set; }
    public Guid RecipeId { get; set; }
    public Guid IngredientId { get; set; }
}