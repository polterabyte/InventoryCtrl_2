using Microsoft.EntityFrameworkCore;
using Inventory.API.Models;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using Serilog;
using System.Linq.Expressions;

namespace Inventory.API.Services;

public class UnitOfMeasureService : BaseReferenceDataService<UnitOfMeasure, UnitOfMeasureDto, CreateUnitOfMeasureDto, UpdateUnitOfMeasureDto>
{
    public UnitOfMeasureService(
        AppDbContext context,
        ILogger<UnitOfMeasureService> logger,
        IHttpContextAccessor httpContextAccessor)
        : base(context, logger, httpContextAccessor)
    {
    }

    protected override DbSet<UnitOfMeasure> DbSet => _context.UnitOfMeasures;

    protected override IQueryable<UnitOfMeasure> BaseQuery => _context.UnitOfMeasures.AsQueryable();

    protected override UnitOfMeasureDto MapToDto(UnitOfMeasure entity)
    {
        return new UnitOfMeasureDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Symbol = entity.Symbol,
            Description = entity.Description,
            IsActive = entity.IsActive,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    protected override UnitOfMeasure MapToEntity(CreateUnitOfMeasureDto createDto)
    {
        return new UnitOfMeasure
        {
            Name = createDto.Name,
            Symbol = createDto.Symbol,
            Description = createDto.Description,
            IsActive = true
        };
    }

    protected override void UpdateEntity(UnitOfMeasure entity, UpdateUnitOfMeasureDto updateDto)
    {
        entity.Name = updateDto.Name;
        entity.Symbol = updateDto.Symbol;
        entity.Description = updateDto.Description;
        entity.IsActive = updateDto.IsActive;
    }

    protected override string GetNameProperty(UnitOfMeasure entity) => entity.Name;

    protected override string GetIdentifierProperty(UnitOfMeasure entity) => entity.Symbol;

    protected override bool HasDependencies(UnitOfMeasure entity)
    {
        return _context.Products.Any(p => p.UnitOfMeasureId == entity.Id && p.IsActive);
    }

    protected override IQueryable<UnitOfMeasure> ApplySearchFilter(IQueryable<UnitOfMeasure> query, string search)
    {
        return query.Where(u => u.Name.Contains(search) || 
                               u.Symbol.Contains(search) ||
                               (u.Description != null && u.Description.Contains(search)));
    }

    protected override IQueryable<UnitOfMeasure> ApplyActiveFilter(IQueryable<UnitOfMeasure> query, bool isActive)
    {
        return query.Where(u => u.IsActive == isActive);
    }

    protected override Expression<Func<UnitOfMeasure, bool>> GetIdentifierFilterExpression(string identifier)
    {
        return u => u.Symbol == identifier;
    }

    protected override string GetIdentifierFromCreateDto(CreateUnitOfMeasureDto createDto)
    {
        return createDto.Symbol;
    }

    protected override string GetIdentifierFromUpdateDto(UpdateUnitOfMeasureDto updateDto)
    {
        return updateDto.Symbol;
    }
}
