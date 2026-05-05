using ServicePlatform.Models;
using ServicePlatform.ViewModels.Service;

namespace ServicePlatform.Services.Interfaces;

public interface IVehicleService
{
    Task<VehicleModel> CreateModelAsync(VehicleModel model);
    Task<bool> UpdateModelAsync(int id, VehicleModel model, string providerId);
    Task<bool> DeleteModelAsync(int id, string providerId);
    Task<VehicleModel?> GetByIdAsync(int id);
    Task<IEnumerable<VehicleModel>> GetProviderModelsAsync(string providerId);
    Task<IEnumerable<VehicleModel>> GetAllActiveModelsAsync();
}
