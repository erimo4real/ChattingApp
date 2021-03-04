using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly DataContext _context;
        private readonly ITokenService _tokenService;

        public AccountController(DataContext context , ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {

            if (await UserExist(registerDto.UserName)) { return BadRequest("UserName Taken "); }

            using var hmac = new HMACSHA512();

            var user = new AppUser{
                username = registerDto.UserName.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return new UserDto{
                Username = user.username,
                Token = _tokenService.CreateToken(user)
            };
        }
        

         [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
              var user = await _context.Users
                .SingleOrDefaultAsync(x => x.username == loginDto.Username.ToLower());

                if (user == null) return Unauthorized("Invalid username");

             using var hmac = new HMACSHA512(user.PasswordSalt);
             var ComputeHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

             for (int i = 0; i < ComputeHash.Length; i++)
             {
                 if ( ComputeHash[i] != user.PasswordHash[i]) return Unauthorized("Invalid Password");
             }

            return new UserDto{
                Username = user.username,
                Token = _tokenService.CreateToken(user)
            };
        }

        public async Task<bool> UserExist(string username)
        {
            return await _context.Users.AnyAsync(x => x.username == username.ToLower());
        }

    }
}