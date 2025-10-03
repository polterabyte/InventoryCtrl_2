using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Inventory.API.Interfaces;
using Inventory.Shared.DTOs;
using System.Security.Claims;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CategoryController(ICategoryService categoryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedApiResponse<CategoryDto>>> GetCategories(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] int? parentId = null,
        [FromQuery] bool? isActive = null)
    {
        var isAdmin = User.IsInRole("Admin");
        var response = await categoryService.GetCategoriesAsync(page, pageSize, search, parentId, isActive, isAdmin);
        if (!response.Success)
        {
            return StatusCode(500, response);
        }
        return Ok(response);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetCategory(int id)
    {
        var response = await categoryService.GetCategoryByIdAsync(id);
        if (!response.Success)
        {
            return NotFound(response);
        }
        return Ok(response);
    }

    [HttpGet("root")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetRootCategories()
    {
        var response = await categoryService.GetRootCategoriesAsync();
        if (!response.Success)
        {
            return StatusCode(500, response);
        }
        return Ok(response);
    }

    [HttpGet("{parentId}/sub")]
    public async Task<ActionResult<ApiResponse<List<CategoryDto>>>> GetSubCategories(int parentId)
    {
        var response = await categoryService.GetSubCategoriesAsync(parentId);
        if (!response.Success)
        {
            return StatusCode(500, response);
        }
        return Ok(response);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> CreateCategory([FromBody] CreateCategoryDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ApiResponse<CategoryDto>.ErrorResult("Invalid model state", errors));
        }

        var response = await categoryService.CreateCategoryAsync(request);
        if (!response.Success)
        {
            return BadRequest(response);
        }
        return CreatedAtAction(nameof(GetCategory), new { id = response.Data!.Id }, response);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> UpdateCategory(int id, [FromBody] UpdateCategoryDto request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
            return BadRequest(ApiResponse<CategoryDto>.ErrorResult("Invalid model state", errors));
        }

        var response = await categoryService.UpdateCategoryAsync(id, request);
        if (!response.Success)
        {
            if (response.ErrorMessage != null && response.ErrorMessage.Contains("not found"))
                return NotFound(response);
            return BadRequest(response);
        }
        return Ok(response);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ApiResponse<object>>> DeleteCategory(int id)
    {
        var response = await categoryService.DeleteCategoryAsync(id);
        if (!response.Success)
        {
            if (response.ErrorMessage != null && response.ErrorMessage.Contains("not found"))
                return NotFound(response);
            return BadRequest(response);
        }
        return Ok(response);
    }
}