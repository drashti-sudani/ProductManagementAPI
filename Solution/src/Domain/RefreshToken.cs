using System;

namespace Domain
{
    public class RefreshToken
    {
        public int Id { get; set; }

        public string Token { get; set; } = string.Empty;

        public string Username { get; set; } = string.Empty;

        public DateTime ExpiresAt { get; set; }

        public bool Revoked { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
