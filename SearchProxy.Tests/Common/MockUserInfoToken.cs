using Microsoft.IdentityModel.Tokens;
using OrderCloud.Catalyst;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace SearchProxy.Common.Tests
{
    public static class MockUserInfoToken
    {
        /// <summary>
        /// Create a fake token for unit testing. (Grants no access to the API).
        /// If 'signingCredentials' is supplied, the token is signed with those credentials (e.g., RS256).
        /// Otherwise, falls back to HMAC SHA256 with a static symmetric key.
        /// </summary>
        public static string BuildUserInfoToken(
            string? currency = null,
            string? companyID = "SELLER_A",
            IEnumerable<string>? groups = null,
            IEnumerable<string>? roles = null,
            string? marketplaceID = null,
            string? authUrl = "https://auth.example.com",
            string? apiUrl = "https://api.example.com",
            string? keyID = TestRsaKeyProvider.AllowedKid,
            string? username = "test-user",
            DateTime? expiresUTC = null,
            DateTime? notValidBeforeUTC = null,
            SigningCredentials? signingCredentials = null
        )
        {
            var creds = signingCredentials ?? TestRsaKeyProvider.AllowedSigningCredentials;

            var header = new JwtHeader(creds);
            if (keyID != null)
            {
                header["kid"] = keyID;
            }

            var claims = new List<Claim>();

            foreach (var role in roles ?? new List<string>())
            {
                claims.Add(new Claim("availableroles", role));
            }

            foreach (var group in groups ?? new List<string>())
            {
                claims.Add(new Claim("groups", group));
            }

            AddClaimIfNotNull(claims, "currency", currency);
            AddClaimIfNotNull(claims, "sub", username);
            AddClaimIfNotNull(claims, "marketplaceID", marketplaceID);
            AddClaimIfNotNull(claims, "companyID", companyID);

            var payload = new JwtPayload(
                issuer: authUrl ?? "mockdomain.com",
                audience: apiUrl ?? "mockdomain.com",
                claims: claims,
                expires: expiresUTC ?? DateTime.UtcNow.AddMinutes(30),
                notBefore: notValidBeforeUTC ?? DateTime.UtcNow
            );

            var token = new JwtSecurityToken(header, payload);
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public static DecodedUserInfoToken BuildContext(
            string? currency = null,
            string? companyID = "SELLER_A",
            IEnumerable<string>? groups = null,
            IEnumerable<string>? roles = null,
            string? marketplaceID = null,
            string? authUrl = "https://auth.example.com",
            string? apiUrl = "https://api.example.com",
            string? keyID = TestRsaKeyProvider.AllowedKid,
            string? username = "test-user",
            DateTime? expiresUTC = null,
            DateTime? notValidBeforeUTC = null,
            SigningCredentials? signingCredentials = null
            )
        {
            var token = BuildUserInfoToken(currency, companyID, groups, roles, marketplaceID, authUrl, apiUrl, keyID, username, expiresUTC, notValidBeforeUTC, signingCredentials);
            return new DecodedUserInfoToken(token);
        }

        private static void AddClaimIfNotNull(List<Claim> claims, string type, string? value)
        {
            if (value != null) { claims.Add(new Claim(type, value)); }
        }
    }
}
