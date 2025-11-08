using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.BLL.Services
{
    public class CustomerProfileService : ICustomerProfileService
    {
        private readonly ICustomerProfileRepository _customerProfileRepository;

        public CustomerProfileService(ICustomerProfileRepository customerProfileRepository)
        {
            _customerProfileRepository = customerProfileRepository;
        }

        public async Task<IEnumerable<CustomerProfile>> GetAllCustomerProfilesAsync()
        {
            return await _customerProfileRepository.GetAllAsync();
        }

        public async Task<CustomerProfile?> GetCustomerProfileByIdAsync(int id)
        {
            return await _customerProfileRepository.GetByIdAsync(id);
        }

        public async Task<CustomerProfile?> GetCustomerProfileByUserIdAsync(int userId)
        {
            return await _customerProfileRepository.GetByUserIdAsync(userId);
        }

        public async Task<IEnumerable<CustomerProfile>> SearchCustomerProfilesAsync(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetAllCustomerProfilesAsync();
            }
            return await _customerProfileRepository.SearchByNameOrPhoneAsync(searchTerm);
        }

        public async Task<CustomerProfile> CreateCustomerProfileAsync(CustomerProfile customerProfile)
        {
            if (customerProfile == null)
            {
                throw new ArgumentNullException(nameof(customerProfile));
            }

            // Validate required fields
            if (string.IsNullOrWhiteSpace(customerProfile.FullName))
            {
                throw new ArgumentException("Full name is required", nameof(customerProfile));
            }

            if (string.IsNullOrWhiteSpace(customerProfile.Phone))
            {
                throw new ArgumentException("Phone number is required", nameof(customerProfile));
            }

            customerProfile.CreatedDate = DateTime.UtcNow;
            return await _customerProfileRepository.AddAsync(customerProfile);
        }

        public async Task UpdateCustomerProfileAsync(CustomerProfile customerProfile)
        {
            if (customerProfile == null)
            {
                throw new ArgumentNullException(nameof(customerProfile));
            }

            if (!await _customerProfileRepository.ExistsAsync(customerProfile.Id))
            {
                throw new KeyNotFoundException($"Customer profile with ID {customerProfile.Id} not found");
            }

            customerProfile.UpdatedDate = DateTime.UtcNow;
            await _customerProfileRepository.UpdateAsync(customerProfile);
        }

        public async Task DeleteCustomerProfileAsync(int id)
        {
            var customerProfile = await _customerProfileRepository.GetByIdAsync(id);
            if (customerProfile == null)
            {
                throw new KeyNotFoundException($"Customer profile with ID {id} not found");
            }

            await _customerProfileRepository.DeleteAsync(id);
        }

        public async Task<bool> CustomerProfileExistsAsync(int id)
        {
            return await _customerProfileRepository.ExistsAsync(id);
        }

        public async Task<bool> CustomerProfileExistsByUserIdAsync(int userId)
        {
            return await _customerProfileRepository.ExistsByUserIdAsync(userId);
        }
    }
}