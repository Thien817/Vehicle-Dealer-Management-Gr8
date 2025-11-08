using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.BLL.IService;
using Vehicle_Dealer_Management.DAL.Data;
using Microsoft.EntityFrameworkCore;

namespace Vehicle_Dealer_Management.BLL.Services
{
    public class SalesDocumentService : ISalesDocumentService
    {
        private readonly ISalesDocumentRepository _salesDocumentRepository;
        private readonly IDealerRepository _dealerRepository;
        private readonly ICustomerProfileRepository _customerProfileRepository; // ✅ THAY ĐỔI
        private readonly ApplicationDbContext _context;

        public SalesDocumentService(
            ISalesDocumentRepository salesDocumentRepository,
            IDealerRepository dealerRepository,
            ICustomerProfileRepository customerProfileRepository, // ✅ THAY ĐỔI
            ApplicationDbContext context)
        {
            _salesDocumentRepository = salesDocumentRepository;
            _dealerRepository = dealerRepository;
            _customerProfileRepository = customerProfileRepository; // ✅ THAY ĐỔI
            _context = context;
        }

        public async Task<SalesDocument?> GetSalesDocumentWithDetailsAsync(int id)
        {
            return await _salesDocumentRepository.GetSalesDocumentWithDetailsAsync(id);
        }

        public async Task<IEnumerable<SalesDocument>> GetSalesDocumentsByDealerIdAsync(int dealerId, string? type = null, string? status = null)
        {
            return await _salesDocumentRepository.GetSalesDocumentsByDealerIdAsync(dealerId, type, status);
        }

        public async Task<IEnumerable<SalesDocument>> GetSalesDocumentsByCustomerIdAsync(int customerId, string? type = null)
        {
            return await _salesDocumentRepository.GetSalesDocumentsByCustomerIdAsync(customerId, type);
        }

        public async Task<IEnumerable<SalesDocument>> GetSalesDocumentsByDateRangeAsync(DateTime startDate, DateTime endDate, string? type = null)
        {
            return await _salesDocumentRepository.GetSalesDocumentsByDateRangeAsync(startDate, endDate, type);
        }

        public async Task<SalesDocument> CreateQuoteAsync(int dealerId, int customerId, int createdBy, int? promotionId = null)
        {
            // Validate dealer exists
            var dealer = await _dealerRepository.GetByIdAsync(dealerId);
            if (dealer == null)
            {
                throw new KeyNotFoundException($"Dealer with ID {dealerId} not found");
            }

            // ✅ SỬA: Validate customer profile exists (thay vì Customer)
            var customerProfile = await _customerProfileRepository.GetByIdAsync(customerId);
            if (customerProfile == null)
            {
                throw new KeyNotFoundException($"Customer with ID {customerId} not found");
            }

            var quote = new SalesDocument
            {
                Type = "QUOTE",
                DealerId = dealerId,
                CustomerId = customerId, // Lưu CustomerProfile.Id
                Status = "DRAFT",
                PromotionId = promotionId,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = createdBy
            };

            return await _salesDocumentRepository.AddAsync(quote);
        }

        public async Task<SalesDocument> ConvertQuoteToOrderAsync(int quoteId)
        {
            var quote = await _salesDocumentRepository.GetSalesDocumentWithDetailsAsync(quoteId);
            if (quote == null)
            {
                throw new KeyNotFoundException($"Quote with ID {quoteId} not found");
            }

            if (quote.Type != "QUOTE")
            {
                throw new InvalidOperationException("Document is not a QUOTE");
            }

            // Convert to ORDER
            quote.Type = "ORDER";
            quote.Status = "CONFIRMED";
            quote.SignedAt = DateTime.UtcNow;
            quote.UpdatedAt = DateTime.UtcNow;

            await _salesDocumentRepository.UpdateAsync(quote);
            return quote;
        }

        public async Task<SalesDocument> UpdateSalesDocumentStatusAsync(int id, string status)
        {
            var document = await _salesDocumentRepository.GetByIdAsync(id);
            if (document == null)
            {
                throw new KeyNotFoundException($"SalesDocument with ID {id} not found");
            }

            document.Status = status;
            document.UpdatedAt = DateTime.UtcNow;
            await _salesDocumentRepository.UpdateAsync(document);
            return document;
        }

        public async Task<bool> SalesDocumentExistsAsync(int id)
        {
            return await _salesDocumentRepository.ExistsAsync(id);
        }

        public async Task<bool> HasSalesDocumentLinesAsync(int vehicleId)
        {
            return await _salesDocumentRepository.HasSalesDocumentLinesAsync(vehicleId);
        }
    }
}