using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DataAccess.Context;
using DataAccess.Models;
using ApiApp.Services;
using ApiApp.Models.Dto.User;
using Azure.Core;
using ApiApp.Exceptions;
using System.Net;

namespace ApiApp.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(DataContext context,
                                IUserService userService) : ControllerBase
    {
        private readonly IUserService _userService = userService;
        private readonly DataContext _context = context;


        // GET: api/User/5
        [HttpGet("{id}")]
        public async Task<ActionResult<UserModel>> GetUserModel(int id)
        {
            var userModel = await _context.Users.FindAsync(id);

            if (userModel == null)
            {
                return NotFound();
            }

            return userModel;
        }

        // PUT: api/User/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserModel(int id, UserModel userModel)
        {
            if (id != userModel.Id)
            {
                return BadRequest();
            }

            _context.Entry(userModel).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserModelExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        // POST: api/User
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("Register")]
        public async Task<ActionResult> PostUserModel(UserRegisterDto userDto)
        {
            await _userService.Register(userDto);

            return Ok();
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto userDto)
        {
            (string accessToken, string refreshToken) = await _userService.Login(userDto);

            Response.Cookies.Append("X-Refresh-Token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new {accessToken, refreshToken});
        }
        [HttpPost("Refresh")]
        public async Task<IActionResult> RefreshToken()
        {
            string? inputRefreshToken = Request.Cookies["X-Refresh-Token"];

            if(inputRefreshToken is null)
                throw new ClientResponseException("Missing Refresh Token", HttpStatusCode.BadRequest);

            (string accessToken, string refreshToken) = await _userService.RefreshToken(inputRefreshToken);

            Response.Cookies.Append("X-Refresh-Token", refreshToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTime.UtcNow.AddDays(7)
            });

            return Ok(new { accessToken, refreshToken });
        }

        // DELETE: api/User/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUserModel(int id)
        {
            var userModel = await _context.Users.FindAsync(id);
            if (userModel == null)
            {
                return NotFound();
            }

            _context.Users.Remove(userModel);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserModelExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }
    }
}
