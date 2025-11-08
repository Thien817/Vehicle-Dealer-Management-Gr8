using Vehicle_Dealer_Management.DAL.Models;

namespace Vehicle_Dealer_Management.BLL.IService
{
    public interface ICustomerProfileService
    {
        Task<IEnumerable<CustomerProfile>> GetAllCustomerProfilesAsync();
        Task<CustomerProfile?> GetCustomerProfileByIdAsync(int id);
        Task<CustomerProfile?> GetCustomerProfileByUserIdAsync(int userId);
        Task<IEnumerable<CustomerProfile>> SearchCustomerProfilesAsync(string searchTerm);
        Task<CustomerProfile> CreateCustomerProfileAsync(CustomerProfile customerProfile);
        Task UpdateCustomerProfileAsync(CustomerProfile customerProfile);
        Task DeleteCustomerProfileAsync(int id);
        Task<bool> CustomerProfileExistsAsync(int id);
        Task<bool> CustomerProfileExistsByUserIdAsync(int userId);
    }
}