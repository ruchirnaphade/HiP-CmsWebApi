﻿using Microsoft.AspNetCore.Mvc;
using PaderbornUniversity.SILab.Hip.CmsApi.Managers;
using PaderbornUniversity.SILab.Hip.CmsApi.Models.AnnotationAnalytics;
using PaderbornUniversity.SILab.Hip.CmsApi.Utility;
using System;
using System.Threading.Tasks;

namespace PaderbornUniversity.SILab.Hip.CmsApi.Controllers
{
    public partial class TopicsController
    {
        private readonly ContentAnalyticsManager _analyticsManager;

        /// <summary>
        /// gets the AnnotationTag Frequency Analytics of {topicId}
        /// </summary>
        /// <param name="topicId">the Id of the Topic {topicId}</param>
        /// <response code="200">The Analytics of</response>
        /// <response code="404">Resource not found</response>
        /// <response code="403">User not allowed to get Analytics</response>
        /// <response code="401">User is denied</response>
        [HttpGet("{topicId}/Analytics/TagFrequency")]
        [ProducesResponseType(typeof(TagFrequencyAnalyticsResult), 200)]
        [ProducesResponseType(typeof(void), 404)]
        [ProducesResponseType(typeof(void), 403)]
        public async Task<IActionResult> GetTagFrequencyAnalyticsAsync([FromRoute]int topicId)
        {
            if (!(await _topicPermissions.IsAssociatedToAsync(User.Identity.GetUserIdentity(), topicId)))
                return Forbid();

            try
            {
                return Ok(_analyticsManager.GetFrequencyAnalytic(topicId));
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }
    }
}