using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.DAL.IRepository
{
    public interface ICustomerProfileRepository : IRepository<CustomerProfile>
    {
        Task<CustomerProfile?> GetByUserIdAsync(int userId);
        Task<IEnumerable<CustomerProfile>> SearchByNameOrPhoneAsync(string searchTerm);
        Task<bool> ExistsByUserIdAsync(int userId);
    }
}