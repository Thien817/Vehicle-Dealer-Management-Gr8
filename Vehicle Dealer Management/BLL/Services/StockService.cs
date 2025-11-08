using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.BLL.Services
{
    public class StockService : IStockService
    {
        private readonly IStockRepository _stockRepository;
        private readonly IVehicleRepository _vehicleRepository;

        public StockService(IStockRepository stockRepository, IVehicleRepository vehicleRepository)
        {
            _stockRepository = stockRepository;
            _vehicleRepository = vehicleRepository;
        }

        public async Task<IEnumerable<Stock>> GetStocksByOwnerAsync(string ownerType, int ownerId)
        {
            return await _stockRepository.GetStocksByOwnerAsync(ownerType, ownerId);
        }

        public async Task<IEnumerable<Stock>> GetStocksByVehicleIdAsync(int vehicleId)
        {
            return await _stockRepository.GetStocksByVehicleIdAsync(vehicleId);
        }

        public async Task<Stock?> GetStockByOwnerAndVehicleAsync(string ownerType, int ownerId, int vehicleId, string colorCode)
        {
            return await _stockRepository.GetStockByOwnerAndVehicleAsync(ownerType, ownerId, vehicleId, colorCode);
        }

        public async Task<IEnumerable<Stock>> GetAvailableStocksByVehicleIdAsync(int vehicleId, string ownerType)
        {
            return await _stockRepository.GetAvailableStocksByVehicleIdAsync(vehicleId, ownerType);
        }

        public async Task<decimal> GetTotalStockQtyAsync(int vehicleId, string ownerType)
        {
            return await _stockRepository.GetTotalStockQtyAsync(vehicleId, ownerType);
        }

        public async Task<Stock> CreateOrUpdateStockAsync(string ownerType, int ownerId, int vehicleId, string colorCode, decimal qty)
        {
            if (qty < 0)
            {
                throw new ArgumentException("Stock quantity cannot be negative", nameof(qty));
            }

            // Validate vehicle exists
            var vehicle = await _vehicleRepository.GetByIdAsync(vehicleId);
            if (vehicle == null)
            {
                throw new KeyNotFoundException($"Vehicle with ID {vehicleId} not found");
            }

            // Validate ownerType
            if (ownerType != "EVM" && ownerType != "DEALER")
            {
                throw new ArgumentException("OwnerType must be 'EVM' or 'DEALER'", nameof(ownerType));
            }

            // Check if stock exists
            var existingStock = await _stockRepository.GetStockByOwnerAndVehicleAsync(ownerType, ownerId, vehicleId, colorCode);

            // Set Name from Vehicle for easy viewing and comparison
            var vehicleName = $"{vehicle.ModelName} {vehicle.VariantName}";

            if (existingStock != null)
            {
                // Update existing stock
                existingStock.Qty = qty;
                existingStock.Name = vehicleName;
                existingStock.UpdatedDate = DateTime.UtcNow;
                await _stockRepository.UpdateAsync(existingStock);
                return existingStock;
            }
            else
            {
                // Create new stock
                var stock = new Stock
                {
                    OwnerType = ownerType,
                    OwnerId = ownerId,
                    VehicleId = vehicleId,
                    ColorCode = colorCode,
                    Name = vehicleName,
                    Qty = qty,
                    CreatedDate = DateTime.UtcNow
                };
                return await _stockRepository.AddAsync(stock);
            }
        }

        public async Task<Stock> UpdateStockQtyAsync(int stockId, decimal newQty)
        {
            if (newQty < 0)
            {
                throw new ArgumentException("Stock quantity cannot be negative", nameof(newQty));
            }

            var stock = await _stockRepository.GetByIdAsync(stockId);
            if (stock == null)
            {
                throw new KeyNotFoundException($"Stock with ID {stockId} not found");
            }

            // Update Name from Vehicle if Vehicle exists
            if (stock.VehicleId > 0)
            {
                var vehicle = await _vehicleRepository.GetByIdAsync(stock.VehicleId);
                if (vehicle != null)
                {
                    stock.Name = $"{vehicle.ModelName} {vehicle.VariantName}";
                }
            }

            stock.Qty = newQty;
            stock.UpdatedDate = DateTime.UtcNow;
            await _stockRepository.UpdateAsync(stock);
            return stock;
        }

        public async Task<bool> StockExistsAsync(int id)
        {
            return await _stockRepository.ExistsAsync(id);
        }

        public async Task<bool> DistributeStockToDealerAsync(int evmStockId, int dealerId, int quantity)
        {
            if (quantity <= 0)
            {
                throw new ArgumentException("Số lượng phân phối phải lớn hơn 0", nameof(quantity));
            }

            // Get EVM stock
            var evmStock = await _stockRepository.GetByIdAsync(evmStockId);
            if (evmStock == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy tồn kho EVM với ID {evmStockId}");
            }

            // Validate owner type
            if (evmStock.OwnerType != "EVM")
            {
                throw new InvalidOperationException("Chỉ có thể phân phối từ kho EVM");
            }

            // Check sufficient quantity
            if (evmStock.Qty < quantity)
            {
                throw new InvalidOperationException($"Không đủ hàng trong kho. Có sẵn: {evmStock.Qty}, yêu cầu: {quantity}");
            }

            // Get vehicle info for dealer stock name
            var vehicle = await _vehicleRepository.GetByIdAsync(evmStock.VehicleId);
            if (vehicle == null)
            {
                throw new KeyNotFoundException($"Không tìm thấy xe với ID {evmStock.VehicleId}");
            }

            try
            {
                // 1. Giảm số lượng tồn kho EVM
                evmStock.Qty -= quantity;
                evmStock.UpdatedDate = DateTime.UtcNow;
                await _stockRepository.UpdateAsync(evmStock);

                // 2. Kiểm tra xem dealer đã có stock này chưa
                var dealerStock = await _stockRepository.GetStockByOwnerAndVehicleAsync(
                    "DEALER",
                    dealerId,
                    evmStock.VehicleId,
                    evmStock.ColorCode
                );

                if (dealerStock != null)
                {
                    // Cộng thêm vào stock hiện có
                    dealerStock.Qty += quantity;
                    dealerStock.UpdatedDate = DateTime.UtcNow;
                    dealerStock.Name = $"{vehicle.ModelName} {vehicle.VariantName}";
                    await _stockRepository.UpdateAsync(dealerStock);
                }
                else
                {
                    // Tạo stock mới cho dealer
                    var newDealerStock = new Stock
                    {
                        OwnerType = "DEALER",
                        OwnerId = dealerId,
                        VehicleId = evmStock.VehicleId,
                        ColorCode = evmStock.ColorCode,
                        Name = $"{vehicle.ModelName} {vehicle.VariantName}",
                        Qty = quantity,
                        CreatedDate = DateTime.UtcNow
                    };
                    await _stockRepository.AddAsync(newDealerStock);
                }

                return true;
            }
            catch (Exception)
            {
                // Nếu có lỗi, repository layer sẽ rollback transaction
                throw;
            }
        }
    }
}

