using Microsoft.EntityFrameworkCore;
using ServicePlatform.Models;
using ServicePlatform.Repositories.Interfaces;
using ServicePlatform.Services.Interfaces;

namespace ServicePlatform.Services;

public class VehicleService : IVehicleService
{
    private readonly IGenericRepository<VehicleModel> _repository;

    public VehicleService(IGenericRepository<VehicleModel> repository)
    {
        _repository = repository;
    }

    public async Task<VehicleModel> CreateModelAsync(VehicleModel model)
    {
        await _repository.AddAsync(model);
        await _repository.SaveChangesAsync();
        return model;
    }

    public async Task<bool> UpdateModelAsync(int id, VehicleModel model, string providerId)
    {
        var existing = await _repository.FirstOrDefaultAsync(v => v.Id == id && v.ServiceProviderId == providerId);
        if (existing == null) return false;

        existing.Name = model.Name;
        existing.Type = model.Type;
        existing.ModelNumber = model.ModelNumber;
        existing.ReleaseYear = model.ReleaseYear;
        existing.Description = model.Description;
        existing.ImageUrl = model.ImageUrl;
        existing.IsActive = model.IsActive;

        _repository.Update(existing);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteModelAsync(int id, string providerId)
    {
        var existing = await _repository.FirstOrDefaultAsync(v => v.Id == id && v.ServiceProviderId == providerId);
        if (existing == null) return false;

        existing.IsDeleted = true;
        _repository.Update(existing);
        await _repository.SaveChangesAsync();
        return true;
    }

    public async Task<VehicleModel?> GetByIdAsync(int id) =>
        await _repository.Query().Include(v => v.ServiceProvider).FirstOrDefaultAsync(v => v.Id == id);

    public async Task<IEnumerable<VehicleModel>> GetProviderModelsAsync(string providerId) =>
        await _repository.Query()
            .Where(v => v.ServiceProviderId == providerId)
            .OrderByDescending(v => v.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<VehicleModel>> GetAllActiveModelsAsync() =>
        await _repository.Query()
            .Include(v => v.ServiceProvider)
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .ToListAsync();
}
