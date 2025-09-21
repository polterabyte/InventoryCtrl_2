using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Serilog;
using System.Security.Claims;
using System.Text;

namespace Inventory.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController(UserManager<User> userManager, ILogger<UserController> logger) : ControllerBase
{
    [HttpGet("info")]
    public IActionResult GetUserInfo()
    {
        try
        {
            var username = User.FindFirstValue(ClaimTypes.Name) ?? User.Identity?.Name;
            var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToArray();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            logger.LogInformation("User info requested for: {Username}", username);
            
            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new
                {
                    Username = username,
                    UserId = userId,
                    Roles = roles
                }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user info");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve user info"
            });
        }
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        try
        {
            var query = userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.UserName!.Contains(search) || u.Email!.Contains(search));
            }

            // Apply role filter
            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id);
                query = query.Where(u => userIds.Contains(u.Id));
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var users = await query
                .OrderBy(u => u.UserName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    Email = u.Email!,
                    Role = u.Role,
                    EmailConfirmed = u.EmailConfirmed,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            // Get roles for each user
            foreach (var user in users)
            {
                var userEntity = await userManager.FindByIdAsync(user.Id);
                if (userEntity != null)
                {
                    user.Roles = (await userManager.GetRolesAsync(userEntity)).ToList();
                }
            }

            var pagedResponse = new PagedResponse<UserDto>
            {
                Items = users,
                TotalCount = totalCount,
                PageNumber = page,
                PageSize = pageSize
            };

            return Ok(new PagedApiResponse<UserDto>
            {
                Success = true,
                Data = pagedResponse
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving users");
            return StatusCode(500, new PagedApiResponse<UserDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve users"
            });
        }
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetUser(string id)
    {
        try
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    ErrorMessage = "User not found"
                });
            }

            var roles = await userManager.GetRolesAsync(user);
            
            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Role = user.Role,
                Roles = roles.ToList(),
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Data = userDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving user {UserId}", id);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                ErrorMessage = "Failed to retrieve user"
            });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<UserDto>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse<UserDto>
                {
                    Success = false,
                    ErrorMessage = "User not found"
                });
            }

            // Update user properties
            user.UserName = request.UserName;
            user.Email = request.Email;
            user.Role = request.Role;
            user.EmailConfirmed = request.EmailConfirmed;
            user.UpdatedAt = DateTime.UtcNow;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new ApiResponse<UserDto>
                {
                    Success = false,
                    ErrorMessage = errors
                });
            }

            // Update roles
            var currentRoles = await userManager.GetRolesAsync(user);
            await userManager.RemoveFromRolesAsync(user, currentRoles);
            await userManager.AddToRoleAsync(user, request.Role);

            logger.LogInformation("User updated: {Username} with ID {UserId}", user.UserName, user.Id);

            var userDto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName!,
                Email = user.Email!,
                Role = user.Role,
                Roles = new List<string> { request.Role },
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };

            return Ok(new ApiResponse<UserDto>
            {
                Success = true,
                Data = userDto
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating user {UserId}", id);
            return StatusCode(500, new ApiResponse<UserDto>
            {
                Success = false,
                ErrorMessage = "Failed to update user"
            });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        try
        {
            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "User not found"
                });
            }

            // Prevent deletion of the current user
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (id == currentUserId)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Cannot delete your own account"
                });
            }

            var result = await userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = errors
                });
            }

            logger.LogInformation("User deleted: {Username} with ID {UserId}", user.UserName, user.Id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "User deleted successfully" }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting user {UserId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to delete user"
            });
        }
    }

    [HttpGet("export")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ExportUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null)
    {
        try
        {
            var query = userManager.Users.AsQueryable();

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.UserName!.Contains(search) || u.Email!.Contains(search));
            }

            // Apply role filter
            if (!string.IsNullOrEmpty(role))
            {
                var usersInRole = await userManager.GetUsersInRoleAsync(role);
                var userIds = usersInRole.Select(u => u.Id);
                query = query.Where(u => userIds.Contains(u.Id));
            }

            var users = await query
                .OrderBy(u => u.UserName)
                .Select(u => new UserDto
                {
                    Id = u.Id,
                    UserName = u.UserName!,
                    Email = u.Email!,
                    Role = u.Role,
                    EmailConfirmed = u.EmailConfirmed,
                    CreatedAt = u.CreatedAt,
                    UpdatedAt = u.UpdatedAt
                })
                .ToListAsync();

            // Get roles for each user
            foreach (var user in users)
            {
                var userEntity = await userManager.FindByIdAsync(user.Id);
                if (userEntity != null)
                {
                    user.Roles = (await userManager.GetRolesAsync(userEntity)).ToList();
                }
            }

            // Generate CSV content
            var csv = new StringBuilder();
            csv.AppendLine("Username,Email,Role,EmailConfirmed,CreatedAt,UpdatedAt");
            
            foreach (var user in users)
            {
                csv.AppendLine($"{user.UserName},{user.Email},{user.Role},{user.EmailConfirmed},{user.CreatedAt:yyyy-MM-dd HH:mm:ss},{user.UpdatedAt?.ToString("yyyy-MM-dd HH:mm:ss") ?? ""}");
            }

            var bytes = Encoding.UTF8.GetBytes(csv.ToString());
            return File(bytes, "text/csv", $"users-export-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}.csv");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error exporting users");
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to export users"
            });
        }
    }

    [HttpPost("{id}/change-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordDto request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "Invalid model state",
                    Errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList()
                });
            }

            var user = await userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = "User not found"
                });
            }

            // Remove current password and set new one
            await userManager.RemovePasswordAsync(user);
            var result = await userManager.AddPasswordAsync(user, request.NewPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                return BadRequest(new ApiResponse<object>
                {
                    Success = false,
                    ErrorMessage = errors
                });
            }

            logger.LogInformation("Password changed for user: {Username} with ID {UserId}", user.UserName, user.Id);

            return Ok(new ApiResponse<object>
            {
                Success = true,
                Data = new { message = "Password changed successfully" }
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error changing password for user {UserId}", id);
            return StatusCode(500, new ApiResponse<object>
            {
                Success = false,
                ErrorMessage = "Failed to change password"
            });
        }
    }
}
