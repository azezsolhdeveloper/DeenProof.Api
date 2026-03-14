// DeenProof.Api/DTOs/AddCommentDto.cs
using System.ComponentModel.DataAnnotations;

namespace DeenProof.Api.DTOs;

public class AddCommentDto
{
    public string Content { get; set; }
    public string? Section { get; set; }
}
