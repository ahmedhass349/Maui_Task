// FILE: Services/AuthService.cs
// STATUS: UPDATED
// CHANGES: Fixed ResetPasswordAsync to validate ResetToken (#1),
//          ForgotPasswordAsync now generates reset token (#18),
//          RegisterAsync now sets FirstName/LastName (#24)

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading.Tasks;
using AutoMapper;
using Maui_Task.Shared.Data.Entities;
using Maui_Task.Shared.DTOs.Auth;
using Maui_Task.Web.Helpers;
using Maui_Task.Web.Repositories.Interfaces;
using Maui_Task.Web.Services.Interfaces;

namespace Maui_Task.Web.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly JwtHelper _jwtHelper;
        private readonly IMapper _mapper;

        public AuthService(IUserRepository userRepository, JwtHelper jwtHelper, IMapper mapper)
        {
            _userRepository = userRepository;
            _jwtHelper = jwtHelper;
            _mapper = mapper;
        }

        public async Task<AuthResponse> LoginAsync(LoginRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new UnauthorizedAccessException("Invalid email or password.");

            bool validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            if (!validPassword)
                throw new UnauthorizedAccessException("Invalid email or password.");

            // Update last login timestamp
            user.LastLoginAt = DateTime.UtcNow;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            var token = _jwtHelper.GenerateToken(user);
            var userDto = _mapper.Map<UserDto>(user);

            // generate refresh token
            var refresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            user.RefreshToken = refresh;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return new AuthResponse
            {
                Token = token,
                User = userDto,
                RefreshToken = refresh,
                RefreshTokenExpiry = user.RefreshTokenExpiry
            };
        }

        public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
        {
            var existingUser = await _userRepository.GetByEmailAsync(request.Email);
            if (existingUser != null)
                throw new InvalidOperationException("A user with this email already exists.");

            // Split FullName into FirstName/LastName for the new fields (#24)
            var nameParts = (request.FullName ?? "").Trim().Split(' ', 2);
            string firstName = nameParts[0];
            string lastName = nameParts.Length > 1 ? nameParts[1] : string.Empty;

            var user = new AppUser
            {
                FullName = request.FullName ?? string.Empty,
                FirstName = firstName,
                LastName = lastName,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Company = request.Company,
                Country = request.Country,
                Phone = request.Phone,
                Timezone = request.Timezone,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepository.AddAsync(user);
            await _userRepository.SaveChangesAsync();

            var token = _jwtHelper.GenerateToken(user);
            var userDto = _mapper.Map<UserDto>(user);

            return new AuthResponse
            {
                Token = token,
                User = userDto
            };
        }

        // Alphanumeric charset — excludes visually ambiguous chars (I, O, 0, 1)
        private static readonly char[] CodeChars =
            "ABCDEFGHJKLMNPQRSTUVWXYZ23456789".ToCharArray();

        private static string GenerateRecoveryCode()
        {
            var bytes = RandomNumberGenerator.GetBytes(8);
            var part1 = new char[4];
            var part2 = new char[4];
            for (int i = 0; i < 4; i++)
                part1[i] = CodeChars[bytes[i] % CodeChars.Length];
            for (int i = 0; i < 4; i++)
                part2[i] = CodeChars[bytes[4 + i] % CodeChars.Length];
            return $"{new string(part1)}-{new string(part2)}";
        }

        public async Task<string> ForgotPasswordAsync(ForgotPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new KeyNotFoundException("No account found with this email address.");

            var code = GenerateRecoveryCode();
            user.ResetToken = code;
            user.ResetTokenExpiry = DateTime.UtcNow.AddMinutes(15);

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            return code;
        }

        public async Task ResetPasswordAsync(ResetPasswordRequest request)
        {
            var user = await _userRepository.GetByEmailAsync(request.Email);
            if (user == null)
                throw new KeyNotFoundException("No account found with this email address.");

            // Validate the reset token (#1)
            if (string.IsNullOrEmpty(user.ResetToken) ||
                !string.Equals(user.ResetToken, request.Token.ToUpperInvariant(), StringComparison.Ordinal) ||
                !user.ResetTokenExpiry.HasValue ||
                user.ResetTokenExpiry.Value < DateTime.UtcNow)
            {
                throw new UnauthorizedAccessException("Invalid or expired reset token.");
            }

            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }

        public async Task<UserDto> GetCurrentUserAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new KeyNotFoundException("User not found.");

            return _mapper.Map<UserDto>(user);
        }

        public async Task<AuthResponse> RefreshAsync(string refreshToken)
        {
            if (string.IsNullOrEmpty(refreshToken))
                throw new UnauthorizedAccessException("Invalid refresh token.");

            var user = await _userRepository.GetByRefreshTokenAsync(refreshToken);
            if (user == null || string.IsNullOrEmpty(user.RefreshToken) || user.RefreshToken != refreshToken)
                throw new UnauthorizedAccessException("Invalid refresh token.");

            if (!user.RefreshTokenExpiry.HasValue || user.RefreshTokenExpiry.Value < DateTime.UtcNow)
                throw new UnauthorizedAccessException("Refresh token expired.");

            // rotate refresh token
            var newRefresh = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
            user.RefreshToken = newRefresh;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);

            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();

            var token = _jwtHelper.GenerateToken(user);
            var userDto = _mapper.Map<UserDto>(user);

            return new AuthResponse
            {
                Token = token,
                User = userDto,
                RefreshToken = newRefresh,
                RefreshTokenExpiry = user.RefreshTokenExpiry
            };
        }

        public async Task LogoutAsync(int userId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) return;
            user.RefreshToken = null;
            user.RefreshTokenExpiry = null;
            _userRepository.Update(user);
            await _userRepository.SaveChangesAsync();
        }
    }
}
