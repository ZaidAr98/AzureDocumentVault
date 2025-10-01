using Microsoft.AspNetCore.Mvc;
using DocumentVault.Services.DocumentService;
using DocumentVault.Services.CosmosDbService;
using DocumentVault.Models;
using System.ComponentModel.DataAnnotations;

namespace DocumentVault.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DocumentsController : ControllerBase
    {
        private readonly IDocumentService _documentService;
        private readonly ICosmosDbService _cosmosDbService;
        private readonly ILogger<DocumentsController> _logger;

        public DocumentsController(
            IDocumentService documentService,
            ICosmosDbService cosmosDbService,
            ILogger<DocumentsController> logger)
        {
            _documentService = documentService;
            _cosmosDbService = cosmosDbService;
            _logger = logger;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocument(
            [Required] IFormFile file,
            [FromForm] string? tags = null,
            [FromForm] bool enableCdn = false)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest(new { error = "No file provided or file is empty" });
                }

                var tagList = string.IsNullOrEmpty(tags)
                      ? new List<string>()
                      : tags.Split(',').Select(t => t.Trim()).ToList();

                var document = await _documentService.UploadDocumentAsync(file, tagList, enableCdn);

                _logger.LogInformation("Document uploaded successfully: {DocumentId}", document.id);

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, new { error = "An error occurred while uploading the document" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDocuments()
        {
            try
            {
                var documents = await _documentService.GetDocumentsAsync();
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(500, new { error = "An error occurred while retrieving documents" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocument(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { error = "Document ID is required" });
                }

                var document = await _documentService.GetDocumentByIdAsync(id);

                if (document == null)
                {
                    return NotFound(new { error = "Document not found" });
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document {DocumentId}", id);
                return StatusCode(500, new { error = "An error occurred while retrieving the document" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocument(string id)
        {
            try
            {
                if (string.IsNullOrEmpty(id))
                {
                    return BadRequest(new { error = "Document ID is required" });
                }

                var result = await _documentService.DeleteDocumentAsync(id);

                if (!result)
                {
                    return NotFound(new { error = "Document not found" });
                }

                _logger.LogInformation("Document deleted successfully: {DocumentId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", id);
                return StatusCode(500, new { error = "An error occurred while deleting the document" });
            }
        }

        [HttpGet("search/tags")]
        public async Task<IActionResult> SearchByTags([FromQuery, Required] string tags)
        {
            try
            {
                if (string.IsNullOrEmpty(tags))
                {
                    return BadRequest(new { error = "Tags parameter is required" });
                }

                var tagList = tags.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                  .Select(t => t.Trim())
                                  .Where(t => !string.IsNullOrEmpty(t))
                                  .ToList();

                if (!tagList.Any())
                {
                    return BadRequest(new { error = "At least one valid tag is required" });
                }

                // Use the existing CosmosDB service method for tag search
                var filteredDocuments = await _cosmosDbService.GetDocumentsByTagsAsync(tagList);

                return Ok(filteredDocuments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching documents by tags: {Tags}", tags);
                return StatusCode(500, new { error = "An error occurred while searching documents" });
            }
        }
    }
}