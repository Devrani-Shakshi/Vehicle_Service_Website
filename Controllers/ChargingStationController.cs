using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicePlatform.Data;
using ServicePlatform.Models;
using ServicePlatform.Repositories.Interfaces;

namespace ServicePlatform.Controllers;

public class ChargingStationController : Controller
{
    private readonly IGenericRepository<ChargingStation> _repository;
    private readonly ILogger<ChargingStationController> _logger;

    public ChargingStationController(IGenericRepository<ChargingStation> repository, ILogger<ChargingStationController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search = null, string? city = null)
    {
        var query = _repository.Query().Where(s => s.IsActive);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(s => s.Name.Contains(search) || s.Address.Contains(search));

        if (!string.IsNullOrEmpty(city))
            query = query.Where(s => s.City.Equals(city, StringComparison.OrdinalIgnoreCase));

        var stations = await query.ToListAsync();
        ViewBag.Cities = await _repository.Query().Select(s => s.City).Distinct().ToListAsync();
        return View(stations);
    }

    [Authorize(Roles = "Admin,ServiceProvider")]
    [HttpGet]
    public IActionResult Create() => View();

    [Authorize(Roles = "Admin,ServiceProvider")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ChargingStation station)
    {
        if (!ModelState.IsValid) return View(station);

        await _repository.AddAsync(station);
        await _repository.SaveChangesAsync();
        TempData["Success"] = "Charging station added successfully!";
        return RedirectToAction("Index");
    }
}
