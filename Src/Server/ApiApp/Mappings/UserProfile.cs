using ApiApp.Models.Dto.User;
using AutoMapper;
using DataAccess.Models;

namespace ApiApp.Mappings;

public class UserProfile : Profile
{
    public UserProfile()
    {
        CreateMap<UserRegisterDto, UserModel>();
        CreateMap<UserModel, UserRegisterDto>();
    }
}
