using RecipeApp.Application.DTO;
using RecipeApp.Base.Contracts.Infrastructure.Data;

namespace RecipeApp.Application.Contracts.Services;

public interface IRecipeIngredientService : IEntityRepository<RecipeIngredient>
{
    
}