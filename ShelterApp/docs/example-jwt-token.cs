// Generowanie tokena JWT - uproszczony fragment

// 1. Tworzenie claims (danych w tokenie)
var claims = new List<Claim>
{
    new Claim("sub", user.Id.ToString()),        // ID użytkownika
    new Claim("email", user.Email),              // Email
    new Claim("name", user.FullName),            // Imię i nazwisko
    new Claim("role", "User")                    // Rola użytkownika
};

// 2. Klucz do podpisywania tokena
var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

// 3. Tworzenie tokena JWT
var token = new JwtSecurityToken(
    issuer: "ShelterApp",
    audience: "ShelterApp",
    claims: claims,
    expires: DateTime.UtcNow.AddMinutes(60),
    signingCredentials: credentials
);

// 4. Serializacja tokena do stringa
var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
