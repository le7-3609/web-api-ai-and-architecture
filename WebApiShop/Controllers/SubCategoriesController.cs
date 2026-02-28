using DTO;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Services;
using Zxcvbn;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SubCategoriesController : ControllerBase
    {
        private readonly ISubCategoryService _subCategoryService;
        public SubCategoriesController(ISubCategoryService subCategoryService)
        {
            _subCategoryService = subCategoryService;
        }

        private string? MapImageUrl(string? fileName)
        {
            if (string.IsNullOrEmpty(fileName)) return null;
            return $"{Request.Scheme}://{Request.Host}/images/{fileName}";
        }

        // GET: api/<CategoryController>
        [HttpGet]
        async public Task<ActionResult<PaginatedResponse<SubCategoryDTO>>> GetSubCategoryAsync([FromQuery] int position, [FromQuery] int skip, [FromQuery] string? desc, [FromQuery] int?[] mainCategoryIds)
        {
            var (subCategories, totalCount) = await _subCategoryService.GetSubCategoryAsync(position, skip, desc, mainCategoryIds);

            if (subCategories == null)
                return NoContent();

            var updatedSubCategories = subCategories
                .Select(sub => sub with { ImageUrl = MapImageUrl(sub.ImageUrl) })
                .ToList();
            return Ok(new PaginatedResponse<SubCategoryDTO>(updatedSubCategories, totalCount));
        }

        // GET api/<CategoryController>/5
        [HttpGet("{id}")]
        async public Task<ActionResult<SubCategoryDTO>> GetSubCategoryByIdAsync(int id)
        {
            SubCategoryDTO category = await _subCategoryService.GetSubCategoryByIdAsync(id);

            if (category == null)
            {
                return NoContent();
            }
            var updatedCategory = category with { ImageUrl = MapImageUrl(category.ImageUrl) };

            return Ok(updatedCategory);
        }

        // POST api/<CategoryController>
        [HttpPost]
        async public Task<ActionResult<SubCategoryDTO>> AddSubCategoryAsync([FromBody] AddSubCategoryDTO dto)
        {
            SubCategoryDTO subCategory = await _subCategoryService.AddSubCategoryAsync(dto);
            return CreatedAtAction(nameof(GetSubCategoryByIdAsync), new { id = subCategory.SubCategoryId }, subCategory);
        }

        // PUT api/<CategoryController>/5
        [HttpPut("{id}")]
        async public Task UpdateSubCategoryAsync(int id, [FromBody] SubCategoryDTO category)
        {
            await _subCategoryService.UpdateSubCategoryAsync(id, category);
        }

        // DELETE api/<CategoryController>/5
        [HttpDelete("{id}")]
        async public Task<ActionResult> DeleteSubCategoryAsync(int id)
        {
            try
            {
                bool flag = await _subCategoryService.DeleteSubCategoryAsync(id);
                if (flag)
                {
                    return NoContent();
                }
                return NotFound();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
