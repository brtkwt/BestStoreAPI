using BestStoreAPI.Models;
using BestStoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BestStoreAPI.Controllers
{
    [Authorize(Roles = "admin")] // ten kontroller jest dostepny tylko dla admina
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext context;

        public UsersController(ApplicationDbContext context)
        {
            this.context = context;
        }


        [HttpGet]
        public IActionResult GetUsers(int? page)
        {
            if (page == null || page < 1)
            {
                page = 1;
            }

            int pageSize = 5;
            int totalNumberOfPages = 0;

            decimal iloscUserow = context.Users.Count();
            totalNumberOfPages = (int)Math.Ceiling(iloscUserow / pageSize);

            var users = context.Users
                .OrderByDescending(u  => u.Id)
                .Skip((int)(page - 1) * pageSize)   // ile skipujemy przed każdą stroną
                .Take(pageSize)             // ile wyświetlamy 
                .ToList();

            List<UserProfileDto> userProfiles = new List<UserProfileDto>();

            foreach (var user in users)
            {
                var userProfileDto = new UserProfileDto()
                {
                    Id = user.Id,
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    Phone = user.Phone,
                    Address = user.Address,
                    Role = user.Role,
                    CreatedAt = user.CreatedAt,
                };

                userProfiles.Add(userProfileDto);
            }

            var response = new
            {
                Users = userProfiles,
                TotalNumberOfPages = totalNumberOfPages,
                PageSize = pageSize,
                Page = page
            };

            return Ok(response);
        }

        [HttpGet("{id}")]
        public IActionResult GetUser(int id)
        {
            var user = context.Users.Find(id);

            if (user == null)
            {
                return NotFound();
            }

            var userProfileDto = new UserProfileDto()
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Phone = user.Phone,
                Address = user.Address,
                Role = user.Role,
                CreatedAt = user.CreatedAt,
            };

            return Ok(userProfileDto);
        }
    }
}
