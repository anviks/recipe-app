using System.Net;
using App.Contracts.BLL;
using App.Domain.Identity;
using Asp.Versioning;
using AutoMapper;
using Helpers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BLL_DTO = App.BLL.DTO;
using v1_0 = App.DTO.v1_0;

namespace RecipeApp.ApiControllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[ApiController]
public class ReviewsController(
    IAppBusinessLogic businessLogic,
    IMapper mapper,
    UserManager<AppUser> userManager) : ControllerBase
{
    private readonly EntityMapper<v1_0.ReviewResponse, BLL_DTO.ReviewResponse> _responseLayerMapper = new(mapper);
    private readonly EntityMapper<v1_0.ReviewRequest, BLL_DTO.ReviewRequest> _requestLayerMapper = new(mapper);
    
    // GET: api/v1/Reviews
    [HttpGet]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType<IEnumerable<v1_0.ReviewResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<v1_0.ReviewResponse>>> GetReviews()
    {
        var reviews = await businessLogic.Reviews.FindAllAsync();
        return Ok(reviews.Select(_responseLayerMapper.Map));
    }

    // GET: api/v1/Reviews/5
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [Produces("application/json")]
    [ProducesResponseType<v1_0.ReviewResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<v1_0.RestApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<v1_0.ReviewResponse>> GetReview(Guid id)
    {
        BLL_DTO.ReviewResponse? review = await businessLogic.Reviews.FindAsync(id);

        if (review == null)
        {
            return NotFound(
                new v1_0.RestApiErrorResponse
                {
                    Status = HttpStatusCode.NotFound,
                    Error = $"Review with id {id} not found."
                });
        }

        return Ok(_responseLayerMapper.Map(review));
    }

    // PUT: api/v1/Reviews/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id:guid}")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<v1_0.RestApiErrorResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<v1_0.RestApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PutReview(Guid id, v1_0.ReviewRequest reviewRequest)
    {
        if (id != reviewRequest.Id)
        {
            return BadRequest(
                new v1_0.RestApiErrorResponse
                {
                    Status = HttpStatusCode.BadRequest,
                    Error = "Id in the request body does not match the id in the URL."
                });
        }
        
        try
        {
            await businessLogic.Reviews.UpdateAsync(_requestLayerMapper.Map(reviewRequest)!);
            await businessLogic.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await businessLogic.Reviews.ExistsAsync(id))
            {
                return NotFound(
                    new v1_0.RestApiErrorResponse
                    {
                        Status = HttpStatusCode.NotFound,
                        Error = $"Review with id {id} not found."
                    });
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/v1/Reviews
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ProducesResponseType<v1_0.ReviewResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<v1_0.ReviewResponse>> PostReview(v1_0.ReviewRequest reviewRequest)
    {
        businessLogic.Reviews.Add(_requestLayerMapper.Map(reviewRequest)!, Guid.Parse(userManager.GetUserId(User)!));
        await businessLogic.SaveChangesAsync();

        return CreatedAtAction("GetReview", new
        {
            version = HttpContext.GetRequestedApiVersion()?.ToString(),
            id = reviewRequest.Id
        }, reviewRequest);
    }

    // DELETE: api/v1/Reviews/5
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<v1_0.RestApiErrorResponse>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteReview(Guid id)
    {
        BLL_DTO.ReviewResponse? review = await businessLogic.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound(
                new v1_0.RestApiErrorResponse
                {
                    Status = HttpStatusCode.NotFound,
                    Error = $"Review with id {id} not found."
                });
        }

        await businessLogic.Reviews.RemoveAsync(review);
        await businessLogic.SaveChangesAsync();

        return NoContent();
    }
}