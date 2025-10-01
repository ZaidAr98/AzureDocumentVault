using DocumentVault.dtos;
using DocumentVault.Models;
using DocumentVault.Services.LinkService;
using Microsoft.AspNetCore.Mvc;

namespace DocumentVault.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LinkController : ControllerBase
    {
        private readonly ILinkService _linkService;

        public LinkController(ILinkService linkService)
        {
            _linkService = linkService;
        }

        [HttpPost("generate")]
        public async Task<IActionResult> GenerateLink([FromBody] LinkRequestDto request)
        {
            try
            {
                var downloadLink = await _linkService.GenerateLinkAsync(request.DocumentId, request.ExpiryHours);
                return Ok(downloadLink);
            }
            catch (ArgumentException ex)
            {
                return NotFound(ex.Message);
            }
        }

        [HttpGet("download/{linkId}")]
        public async Task<IActionResult> DownloadFile(string linkId)
        {
            var result = await _linkService.DownloadFileAsync(linkId);

            if (!result.HasValue)
                return NotFound("Link is invalid or expired");

            var (fileStream, fileName, contentType) = result.Value;
            return File(fileStream, contentType, fileName);
        }

        [HttpGet("validate/{linkId}")]
        public async Task<IActionResult> ValidateLink(string linkId)
        {
            var isValid = await _linkService.ValidateLinkAsync(linkId);
            return Ok(new { isValid });
        }


        [HttpGet("all")]
        public async Task<IActionResult> GetAllLinks()
        {
            try
            {
                var links = await _linkService.GetAllLinksAsync();
                return Ok(links);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Failed to retrieve links", message = ex.Message });
            }
        }
    }
}