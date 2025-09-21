using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Inventory.API.Services;
using Inventory.Shared.DTOs;
using Inventory.Shared.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Inventory.API.Controllers
{
    /// <summary>
    /// Controller for managing SSL certificates
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")]
    public class SSLCertificateController : ControllerBase
    {
        private readonly ISSLCertificateService _sslService;
        private readonly ILogger<SSLCertificateController> _logger;

        public SSLCertificateController(
            ISSLCertificateService sslService,
            ILogger<SSLCertificateController> logger)
        {
            _sslService = sslService;
            _logger = logger;
        }

        /// <summary>
        /// Get all SSL certificates
        /// </summary>
        /// <returns>List of SSL certificates</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<SSLCertificateDto>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetCertificates()
        {
            try
            {
                var certificates = await _sslService.GetAllCertificatesAsync();
                return Ok(certificates);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SSL certificates");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get SSL certificate by domain
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>SSL certificate details</returns>
        [HttpGet("{domain}")]
        [ProducesResponseType(typeof(SSLCertificateDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetCertificate(string domain)
        {
            try
            {
                var certificate = await _sslService.GetCertificateByDomainAsync(domain);
                if (certificate == null)
                {
                    return NotFound($"Certificate for domain '{domain}' not found");
                }
                return Ok(certificate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SSL certificate for domain {Domain}", domain);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Generate new SSL certificate
        /// </summary>
        /// <param name="request">Certificate generation request</param>
        /// <returns>Generated certificate details</returns>
        [HttpPost("generate")]
        [ProducesResponseType(typeof(SSLCertificateDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GenerateCertificate([FromBody] GenerateCertificateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var certificate = await _sslService.GenerateCertificateAsync(request);
                return Ok(certificate);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid certificate generation request");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating SSL certificate");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Renew SSL certificate
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>Renewed certificate details</returns>
        [HttpPost("{domain}/renew")]
        [ProducesResponseType(typeof(SSLCertificateDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> RenewCertificate(string domain)
        {
            try
            {
                var certificate = await _sslService.RenewCertificateAsync(domain);
                if (certificate == null)
                {
                    return NotFound($"Certificate for domain '{domain}' not found");
                }
                return Ok(certificate);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid certificate renewal request for domain {Domain}", domain);
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error renewing SSL certificate for domain {Domain}", domain);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete SSL certificate
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>Success status</returns>
        [HttpDelete("{domain}")]
        [ProducesResponseType(204)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> DeleteCertificate(string domain)
        {
            try
            {
                var result = await _sslService.DeleteCertificateAsync(domain);
                if (!result)
                {
                    return NotFound($"Certificate for domain '{domain}' not found");
                }
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting SSL certificate for domain {Domain}", domain);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get certificate status and health
        /// </summary>
        /// <returns>Certificate health status</returns>
        [HttpGet("health")]
        [ProducesResponseType(typeof(SSLCertificateHealthDto), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetCertificateHealth()
        {
            try
            {
                var health = await _sslService.GetCertificateHealthAsync();
                return Ok(health);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving SSL certificate health");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Validate certificate for domain
        /// </summary>
        /// <param name="domain">Domain name</param>
        /// <returns>Validation result</returns>
        [HttpPost("{domain}/validate")]
        [ProducesResponseType(typeof(SSLCertificateValidationDto), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> ValidateCertificate(string domain)
        {
            try
            {
                var validation = await _sslService.ValidateCertificateAsync(domain);
                if (validation == null)
                {
                    return NotFound($"Certificate for domain '{domain}' not found");
                }
                return Ok(validation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating SSL certificate for domain {Domain}", domain);
                return StatusCode(500, "Internal server error");
            }
        }
    }
}
