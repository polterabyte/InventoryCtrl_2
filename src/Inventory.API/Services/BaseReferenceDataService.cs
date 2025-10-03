using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Serilog;
using System.Security.Claims;

namespace Inventory.API.Services;

/// <summary>
/// Base service for reference data management
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TDto">DTO type</typeparam>
/// <typeparam name="TCreateDto">Create DTO type</typeparam>
/// <typeparam name="TUpdateDto">Update DTO type</typeparam>
public abstract class BaseReferenceDataService<TEntity, TDto, TCreateDto, TUpdateDto> : IReferenceDataService<TDto, TCreateDto, TUpdateDto>
    where TEntity : class
    where TDto : class
    where TCreateDto : class
    where TUpdateDto : class
{
    protected readonly AppDbContext _context;
    protected readonly Microsoft.Extensions.Logging.ILogger _logger;
    protected readonly IHttpContextAccessor _httpContextAccessor;

    protected BaseReferenceDataService(
        AppDbContext context,
        Microsoft.Extensions.Logging.ILogger logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }

    protected abstract DbSet<TEntity> DbSet { get; }
    protected abstract IQueryable<TEntity> BaseQuery { get; }
    protected abstract TDto MapToDto(TEntity entity);
    protected abstract TEntity MapToEntity(TCreateDto createDto);
    protected abstract void UpdateEntity(TEntity entity, TUpdateDto updateDto);
    protected abstract string GetNameProperty(TEntity entity);
    protected abstract string GetIdentifierProperty(TEntity entity);
    protected abstract bool HasDependencies(TEntity entity);

    public virtual async Task<PagedApiResponse<TDto>> GetAllAsync(int page = 1, int pageSize = 10, string? search = null, bool? isActive = null)
    {
        try
        {
            var query = BaseQuery;

            // Apply search filter
            if (!string.IsNullOrEmpty(search))
            {
                query = ApplySearchFilter(query, search);
            }

            // Apply active filter
            if (isActive.HasValue)
            {
                query = ApplyActiveFilter(query, isActive.Value);
            }
            else
            {
                // By default, show only active items for non-admin users
                var userRole = GetCurrentUserRole();
                if (userRole != "Admin")
                {
                    query = ApplyActiveFilter(query, true);
                }
            }

            // Get total count
            var totalCount = await query.CountAsync();

            // Apply pagination
            var entities = await query
                .OrderBy(GetOrderByExpression())
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = entities.Select(MapToDto).ToList();

            var pagedResponse = new PagedResponse<TDto>
            {
                Items = dtos,
                total = totalCount,
                page = page,
                PageSize = pageSize
            };

            return PagedApiResponse<TDto>.CreateSuccess(pagedResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {TypeName} data", typeof(TEntity).Name);
            return PagedApiResponse<TDto>.CreateFailure($"Failed to retrieve {typeof(TEntity).Name.ToLower()} data");
        }
    }

    public virtual async Task<ApiResponse<List<TDto>>> GetAllSimpleAsync()
    {
        try
        {
            var userRole = GetCurrentUserRole();
            var query = BaseQuery;

            // Show only active items for non-admin users
            if (userRole != "Admin")
            {
                query = ApplyActiveFilter(query, true);
            }

            var entities = await query
                .OrderBy(GetOrderByExpression())
                .ToListAsync();

            var dtos = entities.Select(MapToDto).ToList();

            return ApiResponse<List<TDto>>.SuccessResult(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all {TypeName} data", typeof(TEntity).Name);
            return ApiResponse<List<TDto>>.ErrorResult($"Failed to retrieve {typeof(TEntity).Name.ToLower()} data");
        }
    }

    public virtual async Task<ApiResponse<TDto>> GetByIdAsync(int id)
    {
        try
        {
            var entity = await BaseQuery.FirstOrDefaultAsync(GetIdFilterExpression(id));

            if (entity == null)
            {
                return ApiResponse<TDto>.ErrorResult($"{typeof(TEntity).Name} not found");
            }

            var dto = MapToDto(entity);

            return ApiResponse<TDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving {TypeName} with ID {Id}", typeof(TEntity).Name, id);
            return ApiResponse<TDto>.ErrorResult($"Failed to retrieve {typeof(TEntity).Name.ToLower()}");
        }
    }

    public virtual async Task<ApiResponse<TDto>> CreateAsync(TCreateDto createDto)
    {
        try
        {
            // Check if item with same identifier already exists
            var identifier = GetIdentifierFromCreateDto(createDto);
            if (!string.IsNullOrEmpty(identifier) && await ExistsAsync(identifier))
            {
                return ApiResponse<TDto>.ErrorResult($"{typeof(TEntity).Name} with this identifier already exists");
            }

            var entity = MapToEntity(createDto);
            SetCreatedProperties(entity);

            DbSet.Add(entity);
            await _context.SaveChangesAsync();

            _logger.LogInformation("{TypeName} created: {Name} with ID {Id}", 
                typeof(TEntity).Name, GetNameProperty(entity), GetIdFromEntity(entity));

            var dto = MapToDto(entity);

            return ApiResponse<TDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating {TypeName}", typeof(TEntity).Name);
            return ApiResponse<TDto>.ErrorResult($"Failed to create {typeof(TEntity).Name.ToLower()}");
        }
    }

    public virtual async Task<ApiResponse<TDto>> UpdateAsync(int id, TUpdateDto updateDto)
    {
        try
        {
            var entity = await DbSet.FindAsync(id);
            if (entity == null)
            {
                return ApiResponse<TDto>.ErrorResult($"{typeof(TEntity).Name} not found");
            }

            // Check if another item with the same identifier exists
            var identifier = GetIdentifierFromUpdateDto(updateDto);
            if (!string.IsNullOrEmpty(identifier))
            {
                var existingEntity = await BaseQuery
                    .FirstOrDefaultAsync(GetIdentifierFilterExpression(identifier));
                
                if (existingEntity != null && GetIdFromEntity(existingEntity) != id)
                {
                    return ApiResponse<TDto>.ErrorResult($"{typeof(TEntity).Name} with this identifier already exists");
                }
            }

            UpdateEntity(entity, updateDto);
            SetUpdatedProperties(entity);

            await _context.SaveChangesAsync();

            _logger.LogInformation("{TypeName} updated: {Name} with ID {Id}", 
                typeof(TEntity).Name, GetNameProperty(entity), GetIdFromEntity(entity));

            var dto = MapToDto(entity);

            return ApiResponse<TDto>.SuccessResult(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating {TypeName} with ID {Id}", typeof(TEntity).Name, id);
            return ApiResponse<TDto>.ErrorResult($"Failed to update {typeof(TEntity).Name.ToLower()}");
        }
    }

    public virtual async Task<ApiResponse<object>> DeleteAsync(int id)
    {
        try
        {
            var entity = await DbSet.FindAsync(id);
            if (entity == null)
            {
                return ApiResponse<object>.ErrorResult($"{typeof(TEntity).Name} not found");
            }

            // Check if entity has dependencies
            if (HasDependencies(entity))
            {
                return ApiResponse<object>.ErrorResult($"Cannot delete {typeof(TEntity).Name.ToLower()} with dependencies");
            }

            // Soft delete - set IsActive to false
            SetDeletedProperties(entity);

            await _context.SaveChangesAsync();

            _logger.LogInformation("{TypeName} deleted (soft): {Name} with ID {Id}", 
                typeof(TEntity).Name, GetNameProperty(entity), GetIdFromEntity(entity));

            return ApiResponse<object>.SuccessResult(new { message = $"{typeof(TEntity).Name} deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting {TypeName} with ID {Id}", typeof(TEntity).Name, id);
            return ApiResponse<object>.ErrorResult($"Failed to delete {typeof(TEntity).Name.ToLower()}");
        }
    }

    public virtual async Task<bool> ExistsAsync(string identifier)
    {
        try
        {
            return await BaseQuery.AnyAsync(GetIdentifierFilterExpression(identifier));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking {TypeName} existence", typeof(TEntity).Name);
            return false;
        }
    }

    public virtual async Task<int> GetCountAsync(bool? isActive = null)
    {
        try
        {
            var query = BaseQuery;

            if (isActive.HasValue)
            {
                query = ApplyActiveFilter(query, isActive.Value);
            }

            return await query.CountAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting {TypeName} count", typeof(TEntity).Name);
            return 0;
        }
    }

    protected virtual IQueryable<TEntity> ApplySearchFilter(IQueryable<TEntity> query, string search)
    {
        // Override in derived classes to implement specific search logic
        return query;
    }

    protected virtual IQueryable<TEntity> ApplyActiveFilter(IQueryable<TEntity> query, bool isActive)
    {
        // Override in derived classes to implement specific active filter logic
        return query;
    }

    protected virtual System.Linq.Expressions.Expression<System.Func<TEntity, object>> GetOrderByExpression()
    {
        // Override in derived classes to implement specific ordering
        var nameProperty = typeof(TEntity).GetProperty("Name");
        if (nameProperty != null)
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "x");
            var property = System.Linq.Expressions.Expression.Property(parameter, nameProperty);
            var lambda = System.Linq.Expressions.Expression.Lambda<System.Func<TEntity, object>>(
                System.Linq.Expressions.Expression.Convert(property, typeof(object)), parameter);
            return lambda;
        }
        return x => x.GetHashCode();
    }

    protected virtual System.Linq.Expressions.Expression<System.Func<TEntity, bool>> GetIdFilterExpression(int id)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty != null)
        {
            var parameter = System.Linq.Expressions.Expression.Parameter(typeof(TEntity), "x");
            var property = System.Linq.Expressions.Expression.Property(parameter, idProperty);
            var constant = System.Linq.Expressions.Expression.Constant(id);
            var equals = System.Linq.Expressions.Expression.Equal(property, constant);
            return System.Linq.Expressions.Expression.Lambda<System.Func<TEntity, bool>>(equals, parameter);
        }
        return x => false;
    }

    protected virtual System.Linq.Expressions.Expression<System.Func<TEntity, bool>> GetIdentifierFilterExpression(string identifier)
    {
        // Override in derived classes to implement specific identifier filtering
        return x => false;
    }

    protected virtual string GetIdentifierFromCreateDto(TCreateDto createDto)
    {
        // Override in derived classes to implement specific identifier extraction
        return string.Empty;
    }

    protected virtual string GetIdentifierFromUpdateDto(TUpdateDto updateDto)
    {
        // Override in derived classes to implement specific identifier extraction
        return string.Empty;
    }

    protected virtual void SetCreatedProperties(TEntity entity)
    {
        var createdAtProperty = typeof(TEntity).GetProperty("CreatedAt");
        if (createdAtProperty != null && createdAtProperty.CanWrite)
        {
            createdAtProperty.SetValue(entity, DateTime.UtcNow);
        }
    }

    protected virtual void SetUpdatedProperties(TEntity entity)
    {
        var updatedAtProperty = typeof(TEntity).GetProperty("UpdatedAt");
        if (updatedAtProperty != null && updatedAtProperty.CanWrite)
        {
            updatedAtProperty.SetValue(entity, DateTime.UtcNow);
        }
    }

    protected virtual void SetDeletedProperties(TEntity entity)
    {
        var isActiveProperty = typeof(TEntity).GetProperty("IsActive");
        if (isActiveProperty != null && isActiveProperty.CanWrite)
        {
            isActiveProperty.SetValue(entity, false);
        }

        SetUpdatedProperties(entity);
    }

    protected virtual int GetIdFromEntity(TEntity entity)
    {
        var idProperty = typeof(TEntity).GetProperty("Id");
        if (idProperty != null)
        {
            var value = idProperty.GetValue(entity);
            if (value is int intValue)
            {
                return intValue;
            }
        }
        return 0;
    }

    protected virtual string GetCurrentUserRole()
    {
        var user = _httpContextAccessor.HttpContext?.User;
        return user?.FindFirst(ClaimTypes.Role)?.Value ?? "User";
    }
}
