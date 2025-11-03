using Vehicle_Dealer_Management.DAL.Models;
using Vehicle_Dealer_Management.DAL.IRepository;
using Vehicle_Dealer_Management.BLL.IService;

namespace Vehicle_Dealer_Management.BLL.Services
{
    public class FeedbackService : IFeedbackService
    {
        private readonly IFeedbackRepository _feedbackRepository;

        public FeedbackService(IFeedbackRepository feedbackRepository)
        {
            _feedbackRepository = feedbackRepository;
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByCustomerIdAsync(int customerId)
        {
            return await _feedbackRepository.GetFeedbacksByCustomerIdAsync(customerId);
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByDealerIdAsync(int dealerId)
        {
            return await _feedbackRepository.GetFeedbacksByDealerIdAsync(dealerId);
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByTypeAsync(string type, int? dealerId = null)
        {
            return await _feedbackRepository.GetFeedbacksByTypeAsync(type, dealerId);
        }

        public async Task<IEnumerable<Feedback>> GetFeedbacksByStatusAsync(string status, int? dealerId = null)
        {
            return await _feedbackRepository.GetFeedbacksByStatusAsync(status, dealerId);
        }

        public async Task<Feedback?> GetFeedbackByIdAsync(int id)
        {
            return await _feedbackRepository.GetByIdAsync(id);
        }

        public async Task<Feedback> CreateFeedbackAsync(Feedback feedback)
        {
            if (feedback == null)
            {
                throw new ArgumentNullException(nameof(feedback));
            }

            // Business logic: Validate feedback
            if (string.IsNullOrWhiteSpace(feedback.Content))
            {
                throw new ArgumentException("Feedback content is required", nameof(feedback));
            }

            if (string.IsNullOrWhiteSpace(feedback.Type))
            {
                feedback.Type = "FEEDBACK";
            }

            if (string.IsNullOrWhiteSpace(feedback.Status))
            {
                feedback.Status = "NEW";
            }

            feedback.CreatedAt = DateTime.UtcNow;

            return await _feedbackRepository.AddAsync(feedback);
        }

        public async Task UpdateFeedbackStatusAsync(int id, string status)
        {
            var feedback = await _feedbackRepository.GetByIdAsync(id);
            if (feedback == null)
            {
                throw new KeyNotFoundException($"Feedback with ID {id} not found");
            }

            feedback.Status = status;
            feedback.UpdatedAt = DateTime.UtcNow;

            if (status == "RESOLVED" && feedback.ResolvedAt == null)
            {
                feedback.ResolvedAt = DateTime.UtcNow;
            }

            await _feedbackRepository.UpdateAsync(feedback);
        }

        public async Task<bool> FeedbackExistsAsync(int id)
        {
            return await _feedbackRepository.ExistsAsync(id);
        }

        // Review methods (Type = REVIEW)
        public async Task<Feedback?> GetReviewByOrderIdAsync(int orderId)
        {
            return await _feedbackRepository.GetReviewByOrderIdAsync(orderId);
        }

        public async Task<Feedback> CreateReviewAsync(Feedback review)
        {
            if (review == null)
                throw new ArgumentNullException(nameof(review));

            // Validate review
            if (review.Type != "REVIEW")
                throw new ArgumentException("Review must have Type = REVIEW", nameof(review));

            if (!review.Rating.HasValue || review.Rating < 1 || review.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5", nameof(review));

            if (!review.OrderId.HasValue)
                throw new ArgumentException("OrderId is required for review", nameof(review));

            // Check if review already exists for this order
            var existingReview = await _feedbackRepository.GetReviewByOrderIdAsync(review.OrderId.Value);
            if (existingReview != null)
                throw new InvalidOperationException("Đơn hàng này đã có đánh giá. Vui lòng cập nhật đánh giá hiện có.");

            review.Status = "RESOLVED"; // Reviews don't use workflow status
            review.CreatedAt = DateTime.UtcNow;

            return await _feedbackRepository.AddAsync(review);
        }

        public async Task<Feedback> UpdateReviewAsync(int id, int rating, string? content)
        {
            var review = await _feedbackRepository.GetByIdAsync(id);
            if (review == null)
                throw new KeyNotFoundException($"Review with ID {id} not found");

            if (review.Type != "REVIEW")
                throw new InvalidOperationException("This is not a review");

            if (rating < 1 || rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

            review.Rating = rating;
            review.Content = content ?? review.Content;
            review.UpdatedAt = DateTime.UtcNow;
            
            await _feedbackRepository.UpdateAsync(review);
            return review;
        }

        public async Task<bool> DeleteReviewAsync(int id)
        {
            var review = await _feedbackRepository.GetByIdAsync(id);
            if (review == null || review.Type != "REVIEW")
                return false;

            await _feedbackRepository.DeleteAsync(review);
            return true;
        }

        public async Task<bool> HasReviewedAsync(int orderId)
        {
            var review = await _feedbackRepository.GetReviewByOrderIdAsync(orderId);
            return review != null;
        }

        public async Task<IEnumerable<Feedback>> GetReviewsByDealerIdAsync(int dealerId)
        {
            return await _feedbackRepository.GetReviewsByDealerIdAsync(dealerId);
        }

        public async Task<double> GetAverageRatingByDealerIdAsync(int dealerId)
        {
            return await _feedbackRepository.GetAverageRatingByDealerIdAsync(dealerId);
        }
    }
}

