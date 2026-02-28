using System.ComponentModel.DataAnnotations;

namespace DTO
{
    public record ChatMessageDTO(
        [Required] string Role,
        [Required] string Text
    );

    public record ChatRequestDTO(
        List<ChatMessageDTO>? History,
        [Required] string NewMessage
    );
}
