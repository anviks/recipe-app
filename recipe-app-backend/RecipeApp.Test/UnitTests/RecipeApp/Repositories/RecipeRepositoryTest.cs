using System.Globalization;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using RecipeApp.Base;
using RecipeApp.Base.Helpers;
using RecipeApp.Infrastructure;
using RecipeApp.Infrastructure.Data.EntityFramework;
using RecipeApp.Infrastructure.Data.EntityFramework.Entities;
using RecipeApp.Infrastructure.Data.EntityFramework.Repositories;

namespace RecipeApp.Test.UnitTests.RecipeApp.Repositories;

[Collection("NonParallel")]
public class RecipeRepositoryTest : IClassFixture<TestDatabaseFixture>
{
    private readonly IMapper _mapper;
    private readonly EntityMapper<Recipe, global::RecipeApp.Infrastructure.Data.DTO.Recipe> _entityMapper;
    private readonly TestDatabaseFixture _fixture;
    private readonly AppDbContext _context;
    private readonly RecipeRepository _repository;
    private int _createdRecipes;

    public RecipeRepositoryTest(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile<AutoMapperProfile>();
        });
        _mapper = config.CreateMapper();
        _entityMapper = new EntityMapper<Recipe, global::RecipeApp.Infrastructure.Data.DTO.Recipe>(_mapper);
        (_context, _repository) = SetupDependencies();
    }

    [Fact]
    public async Task Update_ShouldUpdateTranslations()
    {
        // Arrange
        Recipe recipe = CreateRecipe();
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("et-EE");

        // Act
        global::RecipeApp.Infrastructure.Data.DTO.Recipe dalRecipe = _entityMapper.Map(recipe)!;
        dalRecipe.Title = "Test Retsept 1";
        _repository.Update(dalRecipe);
        await _context.SaveChangesAsync();

        // Assert
        Recipe updatedRecipe = await _context.Recipes.FirstAsync(r => r.Id == recipe.Id);
        updatedRecipe.Title.Translate("en-GB").Should().Be("Test Recipe 1");
        updatedRecipe.Title.Translate("en").Should().Be("Test Recipe 1");
        updatedRecipe.Title.Translate("et-EE").Should().Be("Test Retsept 1");
        updatedRecipe.Title.Translate("et").Should().Be("Test Retsept 1");
    }
    
    [Fact]
    public async Task UpdateRange_ShouldUpdateTranslations()
    {
        // Arrange
        Recipe recipe1 = CreateRecipe();
        Recipe recipe2 = CreateRecipe();
        _context.Recipes.AddRange(recipe1, recipe2);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("et-EE");

        // Act
        global::RecipeApp.Infrastructure.Data.DTO.Recipe dalRecipe1 = _entityMapper.Map(recipe1)!;
        dalRecipe1.Title = "Test Retsept 1";
        global::RecipeApp.Infrastructure.Data.DTO.Recipe dalRecipe2 = _entityMapper.Map(recipe2)!;
        dalRecipe2.Title = "Test Retsept 2";
        _repository.UpdateRange(new List<global::RecipeApp.Infrastructure.Data.DTO.Recipe> { dalRecipe1, dalRecipe2 });
        await _context.SaveChangesAsync();

        // Assert
        Recipe updatedRecipe1 = await _context.Recipes.FirstAsync(r => r.Id == recipe1.Id);
        updatedRecipe1.Title.Translate("en-GB").Should().Be("Test Recipe 1");
        updatedRecipe1.Title.Translate("en").Should().Be("Test Recipe 1");
        updatedRecipe1.Title.Translate("et-EE").Should().Be("Test Retsept 1");
        updatedRecipe1.Title.Translate("et").Should().Be("Test Retsept 1");
        
        Recipe updatedRecipe2 = await _context.Recipes.FirstAsync(r => r.Id == recipe2.Id);
        updatedRecipe2.Title.Translate("en-GB").Should().Be("Test Recipe 2");
        updatedRecipe2.Title.Translate("en").Should().Be("Test Recipe 2");
        updatedRecipe2.Title.Translate("et-EE").Should().Be("Test Retsept 2");
        updatedRecipe2.Title.Translate("et").Should().Be("Test Retsept 2");
    }

    [Fact]
    public async Task FindAsync_ShouldReturnRecipe_WithNavigationProperties()
    {
        // Arrange
        Recipe recipe = CreateRecipe();
        _context.Recipes.Add(recipe);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        global::RecipeApp.Infrastructure.Data.DTO.Recipe? dalRecipe = await _repository.FindAsync(recipe.Id);

        // Assert
        dalRecipe.Should().NotBeNull();
        dalRecipe!.Title.Should().Be("Test Recipe 1");
        dalRecipe.AuthorUser.Should().NotBeNull();
        dalRecipe.AuthorUser.Id.Should().Be(TestDatabaseFixture.UserId);
        dalRecipe.RecipeCategories.Should().NotBeNull();
        dalRecipe.RecipeIngredients.Should().NotBeNull();
    }
    
    private (AppDbContext, RecipeRepository) SetupDependencies()
    {
        AppDbContext context = _fixture.CreateContext();
        var repository = new RecipeRepository(context, _fixture.Mapper);
        context.Database.BeginTransaction();

        return (context, repository);
    }

    private Recipe CreateRecipe()
    {
        _createdRecipes++;
        return new Recipe
        {
            Id = Guid.NewGuid(),
            Title = new LangStr($"Test Recipe {_createdRecipes}", "en-GB"),
            Description = $"Test Description {_createdRecipes}",
            ImageFileUrl = "non-existing.jpg",
            Instructions = [$"Test Instruction {_createdRecipes * 2 - 1}", $"Test Instruction {_createdRecipes * 2}"],
            AuthorUserId = TestDatabaseFixture.UserId,
            CreatedAt = DateTime.Now.ToUniversalTime()
        };
    }
}