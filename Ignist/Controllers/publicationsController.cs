﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Ignist.Models;
using Ignist.Data;
using Microsoft.Azure.Cosmos;
using Microsoft.AspNetCore.Authorization;
namespace Ignist.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PublicationsController : ControllerBase // Endret til stor 'P' i navnet for konvensjonens skyld
    {
        private readonly IPublicationsRepository _publicationsRepository;

        public PublicationsController(IPublicationsRepository publicationsRepository)
        {
            _publicationsRepository = publicationsRepository;
        }

        // Get all Publications
        [HttpGet]
        public async Task<ActionResult<List<Publication>>> GetAllPublications()
        {
            try
            {
                var publications = await _publicationsRepository.GetAllPublicationsAsync();
                return Ok(publications);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving publications. please try again later.");
            }
        }

        // Finding a publication with the specific Id
        [HttpGet("{id}")]
        public async Task<ActionResult<Publication>> GetPublication(string id, [FromQuery] string UserId) // Endret til string fordi Cosmos DB bruker string IDs
        {
            var publication = await _publicationsRepository.GetPublicationByIdAsync(id, UserId);
            if (publication is null)
            {
                return NotFound("Publication not found.");
            }

            return Ok(publication);
        }

        [HttpPost]
        [Authorize (Roles ="Admin")]
        public async Task<ActionResult<Publication>> AddPublication(Publication publication)
        {
            await _publicationsRepository.AddPublicationAsync(publication);
            return CreatedAtAction(nameof(GetPublication), new { id = publication.Id }, publication);
        }


        private async Task AddPublicationRecursive(Publication publication, string parentId = null)
        {
            // If a parentId is provided, link the current publication as a child
            if (parentId != null)
            {
                publication.ParentId = parentId; 
            }

            await _publicationsRepository.AddPublicationAsync(publication);

            // Recursively add each child publication
            foreach (var childPublication in publication.ChildPublications)
            {
                await AddPublicationRecursive(childPublication, publication.Id);
            }
        }


        // Updating a Publication
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<Publication>> UpdatePublication(string id, Publication updatedPublication)
        {
            if (string.IsNullOrEmpty(updatedPublication.Id) || updatedPublication.Id != id)
            {
                return BadRequest("The ID of the publication does not match the request.");
            }

            // Optionally, add more checks or logic here, e.g., validate parentId if necessary

            try
            {
                await _publicationsRepository.UpdatePublicationAsync(updatedPublication);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Publication not found.");
            }
        }


        // Deleting a publication
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePublication(string id, [FromQuery] string UserId)
        {
            try
            {
                await _publicationsRepository.DeletePublicationAsync(id, UserId);
                return NoContent();
            }
            catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Publication not found.");
            }
        }
    }
}
