using System;
using System.Collections.Generic;
using System.Linq;
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

        public AccountController(DataContext context, ITokenService tokenService)
        {
            _context = context;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        public async Task<ActionResult<PetDto>> Register(RegisterDto registerDto)
        {
            if (await PetExists(registerDto.Petname)) return BadRequest("PetName is taken");

            using var hmac = new HMACSHA512();

            var pet = new AppPet()
            {
                PetName = registerDto.Petname.ToLower(),
                PasswordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(registerDto.Password)),
                PasswordSalt = hmac.Key
            };

            _context.Pets.Add(pet);
            await _context.SaveChangesAsync();

            return new PetDto 
            {
                Petname = pet.PetName,
                Token = _tokenService.CreateToken(pet)
            };
        }

        [HttpPost("login")]
        public async Task<ActionResult<PetDto>> Login(LoginDto loginDto)
        {
            var pet = await _context.Pets
            .SingleOrDefaultAsync(x => x.PetName == loginDto.PetName);

            if (pet == null) return Unauthorized("invalid petname");

            using var hmac = new HMACSHA512(pet.PasswordSalt);

            var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(loginDto.Password));

            for (int i = 0; i < computedHash.Length; i++)
            {
                if (computedHash[i] != pet.PasswordHash[i]) return Unauthorized("invalid password");
            }

             return new PetDto 
            {
                Petname = pet.PetName,
                Token = _tokenService.CreateToken(pet)
            };

        }
        private async Task<bool> PetExists(string petname)
        {
            return await _context.Pets.AnyAsync(x => x.PetName == petname.ToLower());
        }
    }
}