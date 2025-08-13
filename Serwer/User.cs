using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace CzatujiemyServer
{
    public class User
    {
        public string Id { get; set; }
        public string Nick { get; set; }
        public string PasswordHash { get; set; }
        public DateTime RegisteredAt { get; set; }
        public DateTime LastLoginAt { get; set; }
        public List<string> OwnedChannels { get; set; } = new List<string>();
        public bool IsOnline { get; set; } = false;
        public string CurrentClientId { get; set; }

        public User()
        {
            Id = Guid.NewGuid().ToString();
            RegisteredAt = DateTime.Now;
        }

        public static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(hashedBytes);
            }
        }

        public bool VerifyPassword(string password)
        {
            return PasswordHash == HashPassword(password);
        }

        public void UpdateLastLogin()
        {
            LastLoginAt = DateTime.Now;
        }
    }

    public class LoginRequest
    {
        public string Nick { get; set; }
        public string Password { get; set; }
    }

    public class RegisterRequest
    {
        public string Nick { get; set; }
        public string Password { get; set; }
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string UserId { get; set; }
        public List<string> OwnedChannels { get; set; } = new List<string>();
    }
}