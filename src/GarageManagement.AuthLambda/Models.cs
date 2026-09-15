namespace GarageManagement.AuthLambda;

public sealed record AuthenticationRequest(string Cpf);

public sealed record AuthenticationResponse(string AccessToken, string TokenType, int ExpiresIn);

public sealed record CustomerAuthenticationRecord(Guid Id, string Document, bool IsActive);
