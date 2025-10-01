using DocumentVault.Functions.Models;
using DocumentVault.Functions.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace DocumentVault.Functions.Functions
{
    public class DeleteLinks
    {
        private readonly IDeleteDownloadLink _deleteDownloadLink;
        private readonly ILogger<DeleteLinks> _logger;

        public DeleteLinks(IDeleteDownloadLink deleteDownloadLink, ILogger<DeleteLinks> logger)
        {
            _deleteDownloadLink = deleteDownloadLink;
            _logger = logger;
        }

        [Function("DeleteLinks")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            _logger.LogInformation("DeleteLinks function started processing request at {DateTime}", DateTime.UtcNow);

            try
            {
                var deletedCount = await _deleteDownloadLink.DeleteAllExpiredLinksAsync();

                _logger.LogInformation("Successfully deleted {DeletedCount} expired links", deletedCount);

                return new OkObjectResult(new
                {
                    message = $"Successfully deleted {deletedCount} expired links",
                    deletedCount = deletedCount,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while deleting expired links");

                return new ObjectResult(new
                {
                    error = "An error occurred while deleting expired links",
                    timestamp = DateTime.UtcNow
                })
                {
                    StatusCode = 500
                };
            }
        }

        [Function("CleanupExpiredLinks")]
        public async Task CleanupExpiredLinks([TimerTrigger("0 */5 * * * *")] Models.TimerInfo timerInfo)
        {
            try
            {
                _logger.LogInformation("Starting cleanup job");
                var result = await DeleteExpiredLinksAsync();

                if (result.Success)
                {
                    _logger.LogInformation("Cleanup successful: {message}", result.Message);
                }
                else
                {
                    _logger.LogError("Cleanup failed: {error} - {message}", result.Error, result.Message);
                }

                _logger.LogInformation("Cleanup job completed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fatal cleanup error: {type} - {message} - Stack: {stack}",
                    ex.GetType().Name, ex.Message, ex.StackTrace);
                throw;
            }
        }

        private async Task<DeleteResult> DeleteExpiredLinksAsync()
        {
            try
            {
                var expiredLinks = await _deleteDownloadLink.GetExpiredLinksAsync();
                var expiredCount = expiredLinks.Count();

                if (expiredCount == 0)
                {
                    _logger.LogInformation("No expired links found");
                    return new DeleteResult
                    {
                        Success = true,
                        Message = "No expired links found",
                        DeletedCount = 0
                    };
                }

                _logger.LogInformation("Found {expiredCount} expired links", expiredCount);

                var deletedCount = await _deleteDownloadLink.DeleteAllExpiredLinksAsync();

                _logger.LogInformation("Deleted {deletedCount} expired links", deletedCount);

                return new DeleteResult
                {
                    Success = true,
                    Message = $"Deleted {deletedCount} expired links",
                    DeletedCount = deletedCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting expired links");
                return new DeleteResult
                {
                    Success = false,
                    Error = "Error deleting expired links",
                    Message = ex.Message,
                    DeletedCount = 0
                };
            }
        }
    }
}