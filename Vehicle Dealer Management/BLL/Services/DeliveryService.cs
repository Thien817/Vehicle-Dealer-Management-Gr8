using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Vehicle_Dealer_Management.BLL.Services
{
    public class DeliveryService : IDeliveryService
    {
        private readonly IDeliveryRepository _deliveryRepository;
        private readonly ISalesDocumentRepository _salesDocumentRepository;
        private readonly ApplicationDbContext _context;
        private readonly IPaymentService _paymentService;
        private readonly INotificationService? _notificationService;

        public DeliveryService(
            IDeliveryRepository deliveryRepository,
            ISalesDocumentRepository salesDocumentRepository,
            ApplicationDbContext context,
            IPaymentService paymentService,
            INotificationService? notificationService = null)
        {
            _deliveryRepository = deliveryRepository;
            _salesDocumentRepository = salesDocumentRepository;
            _context = context;
            _paymentService = paymentService;
            _notificationService = notificationService;
        }

        public async Task<Delivery?> GetDeliveryBySalesDocumentIdAsync(int salesDocumentId)
        {
            return await _deliveryRepository.GetDeliveryBySalesDocumentIdAsync(salesDocumentId);
        }

        public async Task<IEnumerable<Delivery>> GetDeliveriesByStatusAsync(string status)
        {
            return await _deliveryRepository.GetDeliveriesByStatusAsync(status);
        }

        public async Task<IEnumerable<Delivery>> GetDeliveriesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _deliveryRepository.GetDeliveriesByDateRangeAsync(startDate, endDate);
        }

        public async Task<Delivery> CreateOrUpdateDeliveryAsync(int salesDocumentId, DateTime scheduledDate, string? handoverNote = null)
        {
            // Validate sales document exists
            var salesDocument = await _salesDocumentRepository.GetByIdAsync(salesDocumentId);
            if (salesDocument == null)
            {
                throw new KeyNotFoundException($"SalesDocument with ID {salesDocumentId} not found");
            }

            // Check if delivery already exists
            var existingDelivery = await _deliveryRepository.GetDeliveryBySalesDocumentIdAsync(salesDocumentId);

            if (existingDelivery != null)
            {
                // Update existing delivery
                existingDelivery.ScheduledDate = scheduledDate;
                existingDelivery.HandoverNote = handoverNote;
                existingDelivery.Status = "SCHEDULED";
                await _deliveryRepository.UpdateAsync(existingDelivery);
                return existingDelivery;
            }
            else
            {
                // Create new delivery
                var delivery = new Delivery
                {
                    SalesDocumentId = salesDocumentId,
                    ScheduledDate = scheduledDate,
                    Status = "SCHEDULED",
                    HandoverNote = handoverNote,
                    CreatedDate = DateTime.UtcNow
                };
                var createdDelivery = await _deliveryRepository.AddAsync(delivery);

                // Auto-update sales document status
                if (salesDocument.Type == "ORDER" && salesDocument.Status != "DELIVERED")
                {
                    salesDocument.Status = "DELIVERY_SCHEDULED";
                    salesDocument.UpdatedAt = DateTime.UtcNow;
                    await _salesDocumentRepository.UpdateAsync(salesDocument);
                }

                return createdDelivery;
            }
        }

        public async Task<Delivery> MarkDeliveryAsDeliveredAsync(int deliveryId, DateTime deliveredDate, string? handoverNote = null)
        {
            var delivery = await _deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery == null)
            {
                throw new KeyNotFoundException($"Delivery with ID {deliveryId} not found");
            }

            delivery.DeliveredDate = deliveredDate;
            delivery.Status = "DELIVERED";
            delivery.HandoverNote = handoverNote ?? delivery.HandoverNote;

            await _deliveryRepository.UpdateAsync(delivery);

            // Auto-update sales document status
            var salesDocument = await _salesDocumentRepository.GetByIdAsync(delivery.SalesDocumentId);
            if (salesDocument != null && salesDocument.Type == "ORDER")
            {
                salesDocument.Status = "DELIVERED";
                salesDocument.UpdatedAt = DateTime.UtcNow;
                await _salesDocumentRepository.UpdateAsync(salesDocument);
            }

            return delivery;
        }

        public async Task<Delivery> UpdateDeliveryStatusAsync(int deliveryId, string status)
        {
            var delivery = await _deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery == null)
            {
                throw new KeyNotFoundException($"Delivery with ID {deliveryId} not found");
            }

            // Validate status
            var validStatuses = new[] { "SCHEDULED", "IN_TRANSIT", "DELIVERED", "CANCELLED" };
            if (!validStatuses.Contains(status))
            {
                throw new ArgumentException($"Invalid delivery status: {status}", nameof(status));
            }

            delivery.Status = status;
            await _deliveryRepository.UpdateAsync(delivery);
            return delivery;
        }

        public async Task<Delivery> StartDeliveryAsync(int deliveryId)
        {
            var delivery = await _deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery == null)
            {
                throw new KeyNotFoundException($"Delivery with ID {deliveryId} not found");
            }

            if (delivery.Status != "SCHEDULED")
            {
                throw new InvalidOperationException($"Cannot start delivery. Current status: {delivery.Status}. Expected: SCHEDULED");
            }

            delivery.Status = "IN_TRANSIT";
            await _deliveryRepository.UpdateAsync(delivery);

            // Auto-update sales document status
            var salesDocument = await _salesDocumentRepository.GetByIdAsync(delivery.SalesDocumentId);
            if (salesDocument != null && salesDocument.Type == "ORDER")
            {
                salesDocument.Status = "IN_DELIVERY";
                salesDocument.UpdatedAt = DateTime.UtcNow;
                await _salesDocumentRepository.UpdateAsync(salesDocument);
            }

            return delivery;
        }

        public async Task<Delivery> CustomerConfirmReceiptAsync(int deliveryId)
        {
            var delivery = await _deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery == null)
            {
                throw new KeyNotFoundException($"Delivery with ID {deliveryId} not found");
            }

            if (delivery.Status != "IN_TRANSIT")
            {
                throw new InvalidOperationException($"Customer cannot confirm receipt. Current status: {delivery.Status}. Expected: IN_TRANSIT");
            }

            // Customer xác nhận nhận xe - tự động chuyển delivery status thành DELIVERED
            delivery.CustomerConfirmed = true;
            delivery.CustomerConfirmedDate = DateTime.UtcNow;
            delivery.DeliveredDate = DateTime.UtcNow;
            delivery.Status = "DELIVERED";
            await _deliveryRepository.UpdateAsync(delivery);

            // Auto-update sales document status - chỉ đổi status nếu đã thanh toán đủ 100%
            // Nếu chưa thanh toán đủ, giữ nguyên status để không đóng đơn
            var salesDocument = await _salesDocumentRepository.GetByIdAsync(delivery.SalesDocumentId);
            if (salesDocument != null && salesDocument.Type == "ORDER")
            {
                // Kiểm tra thanh toán - chỉ đóng đơn nếu đã thanh toán đủ 100%
                var totalAmount = salesDocument.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
                var totalPaid = await _paymentService.GetTotalPaidAmountAsync(salesDocument.Id);
                
                // Nếu đã thanh toán đủ 100%, đổi status thành DELIVERED
                // Nếu chưa đủ, giữ status hiện tại (có thể là IN_DELIVERY) để tiếp tục thanh toán
                if (totalAmount > 0 && totalPaid >= totalAmount)
                {
                    salesDocument.Status = "DELIVERED";
                }
                // Nếu chưa thanh toán đủ, không đổi status - đơn vẫn mở để tiếp tục thanh toán
                
                salesDocument.UpdatedAt = DateTime.UtcNow;
                await _salesDocumentRepository.UpdateAsync(salesDocument);
            }

            return delivery;
        }

        public async Task<Delivery> CompleteDeliveryAsync(int deliveryId, DateTime deliveredDate, string? handoverNote = null)
        {
            var delivery = await _deliveryRepository.GetByIdAsync(deliveryId);
            if (delivery == null)
            {
                throw new KeyNotFoundException($"Delivery with ID {deliveryId} not found");
            }

            if (delivery.Status != "IN_TRANSIT")
            {
                throw new InvalidOperationException($"Cannot complete delivery. Current status: {delivery.Status}. Expected: IN_TRANSIT");
            }

            if (!delivery.CustomerConfirmed)
            {
                throw new InvalidOperationException("Cannot complete delivery. Customer has not confirmed receipt yet.");
            }

            delivery.DeliveredDate = deliveredDate;
            delivery.Status = "DELIVERED";
            delivery.HandoverNote = handoverNote ?? delivery.HandoverNote;

            await _deliveryRepository.UpdateAsync(delivery);

            // Auto-update sales document status - chỉ đổi status nếu đã thanh toán đủ 100%
            // Nếu chưa thanh toán đủ, giữ nguyên status để không đóng đơn
            var salesDocument = await _salesDocumentRepository.GetSalesDocumentWithDetailsAsync(delivery.SalesDocumentId);
            if (salesDocument != null && salesDocument.Type == "ORDER")
            {
                // Kiểm tra thanh toán - chỉ đóng đơn nếu đã thanh toán đủ 100%
                var totalAmount = salesDocument.Lines?.Sum(l => l.UnitPrice * l.Qty - l.DiscountValue) ?? 0;
                var totalPaid = await _paymentService.GetTotalPaidAmountAsync(salesDocument.Id);
                
                // Nếu đã thanh toán đủ 100%, đổi status thành DELIVERED
                // Nếu chưa đủ, giữ status hiện tại (có thể là IN_DELIVERY hoặc DELIVERY_SCHEDULED)
                // Delivery status vẫn là DELIVERED nhưng order status không đổi để tiếp tục thanh toán
                var wasNotDelivered = salesDocument.Status != "DELIVERED";
                if (totalAmount > 0 && totalPaid >= totalAmount)
                {
                    salesDocument.Status = "DELIVERED";
                    
                    // Tạo notification cho customer để đánh giá đơn hàng
                    if (wasNotDelivered && salesDocument.Customer?.UserId != null && _notificationService != null)
                    {
                        try
                        {
                            await _notificationService.CreateNotificationAsync(
                                userId: salesDocument.Customer.UserId.Value,
                                title: "Đơn hàng đã hoàn thành - Hãy đánh giá!",
                                content: $"Đơn hàng #{salesDocument.Id} đã được giao và thanh toán đủ. Hãy đánh giá trải nghiệm của bạn!",
                                type: "ORDER",
                                linkUrl: $"/Customer/OrderDetail?id={salesDocument.Id}",
                                relatedEntityId: salesDocument.Id,
                                relatedEntityType: "Order");
                        }
                        catch
                        {
                            // Ignore notification errors - không làm gián đoạn quá trình giao xe
                        }
                    }
                }
                // Nếu chưa thanh toán đủ, không đổi status - đơn vẫn mở để tiếp tục thanh toán
                
                salesDocument.UpdatedAt = DateTime.UtcNow;
                await _salesDocumentRepository.UpdateAsync(salesDocument);
            }

            return delivery;
        }

        public async Task<bool> DeliveryExistsAsync(int id)
        {
            return await _deliveryRepository.ExistsAsync(id);
        }
    }
}

