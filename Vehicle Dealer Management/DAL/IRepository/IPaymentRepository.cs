using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.DAL.IRepository
{
    public interface IPaymentRepository : IRepository<Payment>
    {
        Task<IEnumerable<Payment>> GetPaymentsBySalesDocumentIdAsync(int salesDocumentId);
        Task<IEnumerable<Payment>> GetPaymentsBySalesDocumentIdsAsync(IEnumerable<int> salesDocumentIds);
        Task<decimal> GetTotalPaidAmountAsync(int salesDocumentId);
    }
}

