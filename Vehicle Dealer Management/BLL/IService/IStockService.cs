using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.BLL.IService
{
    public interface IStockService
    {
        Task<IEnumerable<Stock>> GetStocksByOwnerAsync(string ownerType, int ownerId);
        Task<IEnumerable<Stock>> GetStocksByVehicleIdAsync(int vehicleId);
        Task<Stock?> GetStockByOwnerAndVehicleAsync(string ownerType, int ownerId, int vehicleId, string colorCode);
        Task<IEnumerable<Stock>> GetAvailableStocksByVehicleIdAsync(int vehicleId, string ownerType);
        Task<decimal> GetTotalStockQtyAsync(int vehicleId, string ownerType);
        Task<Stock> CreateOrUpdateStockAsync(string ownerType, int ownerId, int vehicleId, string colorCode, decimal qty);
        Task<Stock> UpdateStockQtyAsync(int stockId, decimal newQty);
        Task<bool> StockExistsAsync(int id);
        Task<bool> DistributeStockToDealerAsync(int evmStockId, int dealerId, int quantity);
    }
}

