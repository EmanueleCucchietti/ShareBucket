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
using Microsoft.AspNetCore.Authorization;

namespace ApiApp.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class UserController(DataContext context,
                                IUserService userService) : ControllerBase
    {
        private readonly IUserService _userService = userService;
        private readonly DataContext _context = context;


        // GET: api/User/
        [HttpGet]
        public async Task<ActionResult<UserModel>> GetUserModel()
        {
            var userId = (int)HttpContext.Items["UserId"]!;

            var user = await _context.Users.FindAsync(userId);

            if (user is null)
                throw new ClientResponseException("User not found with this userId", HttpStatusCode.BadRequest);

            return user;
        }

        // PUT: api/User/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutUserModel(int id, UserUpdateDto userDto)
        {
            var userModel = await _context.Users.FindAsync(id);

            if (userModel is null)
                throw new ClientResponseException("User not found with this userId", HttpStatusCode.BadRequest);

            if(_context.Users.Any(user => user.Email == userDto.Email))
                throw new ClientResponseException("Email already in use", HttpStatusCode.BadRequest);

            userModel.FirstName = userDto.FirstName;
            userModel.LastName = userDto.LastName;
            userModel.Email = userDto.Email;

            _context.Entry(userDto).State = EntityState.Modified;

            await _context.SaveChangesAsync();

            return Ok();
        }

        // POST: api/User
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("Register")]
        public async Task<ActionResult> PostUserModel(UserRegisterDto userDto)
        {
            await _userService.Register(userDto);

            return Ok();
        }

        [AllowAnonymous]
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

        [AllowAnonymous]
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
        public async Task<IActionResult> DeleteUserModel()
        {
            var userId = (int)HttpContext.Items["UserId"]!;

            await _userService.DeleteUser(userId);

            return Ok();
        }
    }
}
