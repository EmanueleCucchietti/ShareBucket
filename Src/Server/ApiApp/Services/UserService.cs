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
    Task<(string accessToken, string refreshToken)> Login(UserLoginDto userDto);
    Task<(string accessToken, string refreshToken)> RefreshToken(string refreshToken);
    Task Register(UserRegisterDto userDto);
}

public class UserService : IUserService
{

    private readonly DataContext _dataContext;
    private readonly IMapper _mapper;
    private readonly IAuthenticationHelper _authenticationHelper;
    public UserService(DataContext dataContext,
                         IMapper mapper,
                         IAuthenticationHelper authenticationHelper)
    {
        _dataContext = dataContext;
        _mapper = mapper;
        _authenticationHelper = authenticationHelper;
    }

    public async Task<(string accessToken, string refreshToken)> Login(UserLoginDto userDto)
    {
        var user = await _dataContext.Users.SingleOrDefaultAsync(user => user.Email == userDto.Email);

        var watch = Stopwatch.StartNew();

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

        if (_dataContext.Users.Any(user => user.Email != userDto.Email))
            throw new ClientResponseException("User Already existent", HttpStatusCode.BadRequest);

        _dataContext.Users.Add(user);

        await _dataContext.SaveChangesAsync();
    }

    public async Task<(string accessToken, string refreshToken)> RefreshToken(string inputRefreshToken)
    {
        var userId = _authenticationHelper.ValidateRefreshTokenAndGetUserId(inputRefreshToken);

        var user = await _dataContext.Users.SingleOrDefaultAsync(user => user.Id == userId);

        if (user is null)
            throw new Exception("Refresh token is invalid");

        var accessToken = _authenticationHelper.GenerateAccessToken(user);

        var refreshToken = _authenticationHelper.GenerateRefreshToken(user);

        return (accessToken, refreshToken);
    }
}
