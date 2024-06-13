using BestStoreAPI.Models;
using BestStoreAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace BestStoreAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController : ControllerBase
    {
        private readonly IConfiguration configuration;
        private readonly ApplicationDbContext context;
        private readonly EmailSender emailSender;

        public AccountController(IConfiguration configuration, ApplicationDbContext context, EmailSender emailSender)
        {
            this.configuration = configuration;
            this.context = context;
            this.emailSender = emailSender;
        }

        [HttpPost("Register")]
        public IActionResult Register(UserDto userDto)
        {
            // check if the email is unique or not
            var email = context.Users.Count( u => u.Email == userDto.Email);

            if(email > 0)
            {
                ModelState.AddModelError("Email", "This email address is already used");
                return BadRequest(ModelState);
            }

            // encrypt password
            var passwordHasher = new PasswordHasher<User>();
            var encryptedPassword = passwordHasher.HashPassword(new User(), userDto.Password);

            // create new account

            User user = new User()
            {
                FirstName = userDto.FirstName,
                LastName = userDto.LastName,
                Email = userDto.Email,
                Phone = userDto.Phone ?? "",
                Address = userDto.Address,
                Password = encryptedPassword,
                Role = "client",
                CreatedAt = DateTime.Now
            };

            context.Users.Add(user);
            context.SaveChanges();

            // crreating jwt that will be transmited to the user

            var jwt = CreateJWToken(user);

            // creating UserProfileDto that will be sent to the user

            UserProfileDto userProfileDto = new UserProfileDto()
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

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };

            return Ok(response);
        }

        [HttpPost("Login")]
        public IActionResult Login(string email, string password)
        {
            var user = context.Users.FirstOrDefault(x => x.Email == email);

            if (user == null)
            {
                ModelState.AddModelError("Error", "Email or Password not valid");
                return BadRequest(ModelState);
            }

            // verify the password
            var passwordHasher = new PasswordHasher<User>();
            var result = passwordHasher.VerifyHashedPassword(new User(), user.Password, password);

            if(result == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError("Password", "Wrong Password");
                return BadRequest(ModelState);
            }

            var jwt = CreateJWToken(user);

            UserProfileDto userProfileDto = new UserProfileDto()
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

            var response = new
            {
                Token = jwt,
                User = userProfileDto
            };

            return Ok(response);
        }

        [HttpPost("ForgotPassword")]
        public IActionResult ForgotPassword(string email)
        {
            var user = context.Users.FirstOrDefault(x => x.Email == email);

            if(user == null)
            {
                return NotFound();
            }

            // delete any old password reset request
            var oldPasswordReset = context.PasswordResets.FirstOrDefault(r => r.Email == email);

            if (oldPasswordReset != null)
            {
                // delete old password reset request
                context.Remove(oldPasswordReset);
            }

            //create password reset token
            string token = Guid.NewGuid().ToString() + "-" + Guid.NewGuid().ToString();

            PasswordReset passwordReset = new PasswordReset()
            {
                Email = email,
                Token = token,
                CreatedAt = DateTime.Now,
            };

            context.PasswordResets.Add(passwordReset);
            context.SaveChanges();

            // SENDING TOKEN BY THE MAIL TO THE USER
            string emailSubject = "Password Reset";
            string userName = user.FirstName + " " + user.LastName;
            string emailMessage = "Dear " + userName + "\n" +
                "We recived your password reset request.\n" +
                "Please copy the following token and paste it in the Password Reset Form:\n" +
                token + "\n\n" +
                "Best Regards\n";

            emailSender.SendEmail(emailSubject, email, userName, emailMessage).Wait();

            return Ok();
        }

        [HttpPost("ResetPassword")]
        public IActionResult ResetPassword(string token, string password)
        {
            var passwordReset = context.PasswordResets.FirstOrDefault(r => r.Token == token);

            if (passwordReset == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token");
                return BadRequest(ModelState);
            }

            var user = context.Users.FirstOrDefault(u => u.Email == passwordReset.Email);

            if (user == null)
            {
                ModelState.AddModelError("Token", "Wrong or Expired Token");
                return BadRequest(ModelState);
            }

            // encrypt password
            var passwordHasher = new PasswordHasher<User>();
            string encryptedPassword = passwordHasher.HashPassword(new User(), password);

            // save the new encrypted password
            user.Password = encryptedPassword;

            // delete the token because it can be used again to change the password
            context.PasswordResets.Remove(passwordReset);

            context.SaveChanges();

            return Ok();
        }

        [Authorize]
        [HttpGet("Profile")]
        public IActionResult GetProfile()
        {
            int id = JwtReader.GetUserId(User);

            var user = context.Users.Find(id);
            if (user == null)
            {
                return Unauthorized();
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

        [Authorize]
        [HttpPut("UpdateProfile")]
        public IActionResult UpdateProfile(UserProfileUpdateDto userProfileUpdateDto)
        {
            int id = JwtReader.GetUserId(User);

            var user = context.Users.Find(id);

            if (user == null)
            {
                return Unauthorized();
            }

            user.FirstName = userProfileUpdateDto.FirstName;
            user.LastName = userProfileUpdateDto.LastName;
            user.Email = userProfileUpdateDto.Email;
            user.Phone = userProfileUpdateDto.Phone ?? "";
            user.Address = userProfileUpdateDto.Address;

            context.SaveChanges();

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

        [Authorize]
        [HttpPut("UpdatePassword")]
        public IActionResult UpdatePassword([Required, MinLength(8), MaxLength(100)] string password)
        {
            int id = JwtReader.GetUserId(User);

            var user = context.Users.Find(id);
            if (user == null)
            {
                return Unauthorized();
            }

            // encrypting password 

            var passwordHasher = new PasswordHasher<User>();
            var encryptedPassword = passwordHasher.HashPassword(new User(), password);

            // update password
            user.Password = encryptedPassword;
            context.SaveChanges();

            return Ok();
        }

        /*
        [Authorize]
        [HttpGet("GetTokenClaims")]
        public IActionResult GetTokenClaims()
        {
            var identity = User.Identity as ClaimsIdentity;

            if (identity != null)
            {
                // dictionary - list of pairs
                Dictionary<string, string> claims = new Dictionary<string, string>();

                foreach (Claim claim in identity.Claims)
                {
                    claims.Add(claim.Type, claim.Value);
                }

                return Ok(claims);
            }
            return Ok();
        }
        */

        /*
        [Authorize]
        [HttpGet("AuthorizeAuthenticatedUsers")]
        public IActionResult AuthorizeAuthenticatedUsers()
        {
            return Ok("You are Authorized");
        }

        [Authorize(Roles = "admin")]
        [HttpGet("AuthorizeAdmin")]
        public IActionResult AuthorizeAdmin()
        {
            return Ok("You are Authorized (admin)");
        }

        [Authorize(Roles = "admin, seller")]
        [HttpGet("AuthorizeAdminAndSeller")]
        public IActionResult AuthorizeAdminAndSeller()
        {
            return Ok("You are Authorized (admin and seller)");
        }
        */

        /*
        [HttpGet("TestToken")]
        public IActionResult TestToken()
        {
            User user = new User() { Id = 2, Role = "admin" };
            string jwt = CreateJWToken(user);

            var response = new { JWToken = jwt };

            return Ok(response);
        }
        */

        private string CreateJWToken(User user)
        {
            List<Claim> claims = new List<Claim>()
            {
                new Claim("id", "" + user.Id),
                new Claim("role", user.Role)
            };

            string strKey = configuration["JwtSettings:Key"]!;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(strKey));

            var credensials = new SigningCredentials(key, SecurityAlgorithms.HmacSha512);

            var token = new JwtSecurityToken(

                issuer : configuration["JwtSettings:Issuer"],
                audience: configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.Now.AddDays(1),
                signingCredentials: credensials
                );

            var jwt = new JwtSecurityTokenHandler().WriteToken(token); // konwersja wszystkiego na string

            return jwt;
        }
    }
}
