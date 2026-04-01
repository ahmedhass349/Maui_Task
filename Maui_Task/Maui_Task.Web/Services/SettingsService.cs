// FILE: Services/SettingsService.cs
// STATUS: UPDATED
// CHANGES: Updated UpdateProfileAsync to use FirstName/LastName (#24)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Settings;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;
using Maui_Task.Web.Helpers;

namespace Maui_Task.Web.Services
{
    public class SettingsService : ISettingsService
    {
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        private readonly JwtHelper _jwtHelper;

        public SettingsService(IUserRepository userRepository, IMapper mapper, JwtHelper jwtHelper)
        {
            _userRepository = userRepository;
            _mapper = mapper;
            _jwtHelper = jwtHelper;
        }

        public async Task<ProfileDto> GetProfileAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            return _mapper.Map<ProfileDto>(user);
        }

        public async Task<AuthResponse> UpdateProfileAsync(int userId, UpdateProfileRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            // Update FirstName/LastName and keep FullName in sync (#24)
            user.FirstName = request.FirstName;
            user.LastName = request.LastName;
            user.FullName = $"{request.FirstName} {request.LastName}".Trim();

            if (!string.IsNullOrEmpty(request.Email))
            {
                var existingUser = await _userRepository.GetByEmailAsync(request.Email);
                if (existingUser != null && existingUser.Id != userId)
                    throw new InvalidOperationException("Email is already in use by another account.");

                user.Email = request.Email;
            }

            user.AvatarUrl = request.AvatarUrl;
            user.Company = request.Company;
            user.Country = request.Country;
            user.Phone = request.Phone;
            user.Timezone = request.Timezone;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            // Generate new token with updated claims
            var newToken = _jwtHelper.GenerateToken(user);
            var userDto = _mapper.Map<UserDto>(user);

            return new AuthResponse
            {
                Token = newToken,
                User = userDto
            };
        }

        public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            bool validPassword = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash);
            if (!validPassword)
                throw new UnauthorizedAccessException("Current password is incorrect.");

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task DeleteAccountAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            _userRepository.Remove(user);
            await _userRepository.SaveChangesAsync();
        }
    }
}
