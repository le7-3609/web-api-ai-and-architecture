using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DTO
{
    public record GeminiPromptDTO(
        long PromptId,
        string Prompt,
        long? SubCategoryId
    );

    public record AddGeminiPromptDTO(
        [Required]
        string Prompt,
        long? SubCategoryId
    );

    public record GeminiInputDTO(
        long? SubCategoryId,
        [Required] string UserRequest
    );

    public record GeminiUserRequestDTO(
        [Required] string UserRequest
    );
}
