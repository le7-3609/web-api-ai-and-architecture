using DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewsController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        private string? MapReviewImageUrl(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            return $"{Request.Scheme}://{Request.Host}/images/reviews/{fileName}";
        }

        // GET api/reviews
        [HttpGet]
        [Authorize(Roles = "Admin")] // Admin only – full review list.
        public async Task<ActionResult<IEnumerable<ReviewSummaryDTO>>> GetAllReviewsAsync()
        {
            var reviews = await _reviewService.GetAllReviewsAsync();
            if (reviews == null || !reviews.Any())
            {
                return NoContent();
            }
            var updatedReviews = reviews
                .Select(r => r with { ReviewImageUrl = MapReviewImageUrl(r.ReviewImageUrl) })
                .ToList();
            return Ok(updatedReviews);
        }

        // GET api/reviews/{orderId}
        [HttpGet("{orderId}")]
        [Authorize] 
        public async Task<ActionResult<ReviewDTO>> GetReviewByOrderIdAsync(int orderId)
        {
            var review = await _reviewService.GetReviewByOrderIdAsync(orderId);
            if (review == null)
            {
                return NotFound($"Review for Order ID {orderId} not found");
            }
            return Ok(review);
        }

        // POST api/reviews/{orderId}
        [HttpPost("{orderId}")]
        [Authorize] 
        public async Task<ActionResult<ReviewDTO>> AddReviewAsync(int orderId, [FromForm] AddReviewDTO dto)
        {
            var newReview = await _reviewService.AddReviewAsync(orderId, dto);
            if (newReview != null)
            {
                return Ok(newReview);
            }
            return BadRequest("Can't create new review or review already exists");
        }

        // PUT api/reviews
        [HttpPut]
        [Authorize] 
        public async Task<ActionResult> UpdateReviewAsync([FromBody] ReviewDTO dto)
        {
            var existingReview = await _reviewService.GetReviewByOrderIdAsync((int)dto.OrderId);
            if (existingReview == null)
            {
                return NotFound($"Review for Order ID {dto.OrderId} not found");
            }
            await _reviewService.UpdateReviewAsync(dto);
            return Ok();
        }
    }
}
