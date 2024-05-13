using ApiApp.Exceptions;
using ApiApp.Helpers;
using ApiApp.Models.Dto.User;
using AutoMapper;
using DataAccess.Context;
using DataAccess.Models;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Net;

namespace ApiApp.Services;

public interface IUserService
{
    Task DeleteUser(int userId);
    Task<(string accessToken, string refreshToken)> Login(UserLoginDto userDto);
    Task<(string accessToken, string refreshToken)> RefreshToken(string refreshToken);
    Task Register(UserRegisterDto userDto);
}

public class UserService : IUserService
{

    private readonly DataContext _context;
    private readonly IMapper _mapper;
    private readonly IAuthenticationHelper _authenticationHelper;
    public UserService(DataContext dataContext,
                         IMapper mapper,
                         IAuthenticationHelper authenticationHelper)
    {
        _context = dataContext;
        _mapper = mapper;
        _authenticationHelper = authenticationHelper;
    }

    public async Task<(string accessToken, string refreshToken)> Login(UserLoginDto userDto)
    {
        var user = await _context.Users.SingleOrDefaultAsync(user => user.Email == userDto.Email);

        if (user is null ||
            _authenticationHelper.GeneratePasswordHash(userDto.Password, user.PasswordSalt)
                != user.PasswordHash)
            throw new ClientResponseException("User not found or wrong password", HttpStatusCode.BadRequest);

        var accessToken = _authenticationHelper.GenerateAccessToken(user);

        var refreshToken = _authenticationHelper.GenerateRefreshToken(user);

        return (accessToken, refreshToken);
    }


    public async Task Register(UserRegisterDto userDto)
    {
        var user = _mapper.Map<UserModel>(userDto);

        user.PasswordSalt = _authenticationHelper.GenerateSalt();

        user.PasswordHash = _authenticationHelper.GeneratePasswordHash(
            userDto.Password, user.PasswordSalt);

        if (_context.Users.Any(user => user.Email != userDto.Email))
            throw new ClientResponseException("User Already existent", HttpStatusCode.BadRequest);

        _context.Users.Add(user);

        await _context.SaveChangesAsync();
    }

    public async Task<(string accessToken, string refreshToken)> RefreshToken(string inputRefreshToken)
    {
        var userId = _authenticationHelper.ValidateRefreshTokenAndGetUserId(inputRefreshToken);

        var user = await _context.Users.SingleOrDefaultAsync(user => user.Id == userId);

        if (user is null)
            throw new ClientResponseException("Refresh token is invalid", HttpStatusCode.BadRequest);

        var accessToken = _authenticationHelper.GenerateAccessToken(user);

        var refreshToken = _authenticationHelper.GenerateRefreshToken(user);

        return (accessToken, refreshToken);
    }

    public async Task DeleteUser(int userId)
    {
        var userModel = await _context.Users.FindAsync(userId);

        if (userModel is null)
            throw new ClientResponseException("User not found", HttpStatusCode.NotFound);

        var friendships = _context.Friendships.Where(f => f.UserId == userId || f.FriendId == userId);
        _context.Friendships.RemoveRange(friendships);

        // Remove all the memoryArea that the user is owner
        // Select all the memoryAreas that the user is owner
        var memoryAreas = _context.MemoryAreas.Where(m => m.UserOwnerId == userId);


        // Remove all the folder of the memoryAreas
        foreach (var memoryArea in memoryAreas)
        {
            // Remove all the metadatas of the memoryAreas
            var metadatas = _context.Metadatas.Where(m => m.MemoryAreaId == memoryArea.Id);
            _context.Metadatas.RemoveRange(metadatas);

            string localFilePath = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, $"UploadedFiles/{memoryArea.Id}"));

            if (Directory.Exists(localFilePath))
            {
                Directory.Delete(localFilePath, true);
            }
        }

        _context.MemoryAreas.RemoveRange(memoryAreas);

        // Remove the user
        _context.Users.Remove(userModel);
        await _context.SaveChangesAsync();
    }
}
