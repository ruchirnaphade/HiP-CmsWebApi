﻿using Api.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;

namespace Api.Controllers
{
    [Produces("application/json")]
    [Route("Download")]
    public class DownloadController : Controller
    {
        /// <summary>
        /// Provides the file associated with the hashValue
        /// </summary>
        /// <param name="downloadHash"></param>
        /// <returns>The requested Resource</returns>
        /// <response code="200">Resource</response>
        /// <response code="403">User is not allows to download the file</response>
        /// <response code="404">Resource Not Found</response>
        /// <response code="410">Download has expired</response>
        [HttpGet("{downloadHash}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(VirtualFileResult), 200)]
        [ProducesResponseType(typeof(void), 403)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(void), 410)]
        public IActionResult Get([FromQuery]string downloadHash)
        {
            var userIp = HttpContext.Connection.RemoteIpAddress;
            var resource = DownloadManager.GetResource(downloadHash);

            if (resource == null) return NotFound();

            if (resource.IsExpired())
                return ApiController.Gone();

            if (!resource.IsSameUser(userIp))
                return ApiController.Forbidden();

            var contentType = MimeKit.MimeTypes.GetMimeType(resource.FileName);
            return File(resource.FileName, contentType, Path.GetFileName(resource.FileName));
        }
    }
}
