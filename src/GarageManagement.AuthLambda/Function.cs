using System.Net;
using System.Text.Json;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace GarageManagement.AuthLambda;

public class Function
{
    private readonly CustomerRepository customerRepository;
    private readonly JwtTokenService tokenService;

    public Function()
    {
        var settings = Environment.GetEnvironmentVariables()
            .Cast<System.Collections.DictionaryEntry>()
            .ToDictionary(entry => (string)entry.Key, entry => entry.Value?.ToString());

        customerRepository = new CustomerRepository(Required(settings, "ConnectionStrings__Default"));
        tokenService = new JwtTokenService(settings);
    }

    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var correlationId = request.Headers?.FirstOrDefault(pair => pair.Key.Equals("X-Correlation-Id", StringComparison.OrdinalIgnoreCase)).Value
            ?? context.AwsRequestId;

        try
        {
            if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(request.Path, "/.well-known/jwks.json", StringComparison.OrdinalIgnoreCase))
            {
                return Json(HttpStatusCode.OK, tokenService.GetPublicJwks(), correlationId);
            }

            var authenticationRequest = ParseRequest(request);
            var cpf = CpfValidator.NormalizeAndValidate(authenticationRequest.Cpf);
            if (cpf is null)
            {
                return Error(HttpStatusCode.BadRequest, "CPF inválido.", correlationId);
            }

            var customer = await customerRepository.FindByCpfAsync(cpf, CancellationToken.None);
            if (customer is null || !customer.IsActive)
            {
                return Error(HttpStatusCode.Unauthorized, "Cliente não autorizado.", correlationId);
            }

            context.Logger.LogInformation($"Customer authentication succeeded. CustomerId={customer.Id}; CorrelationId={correlationId}");
            return Json(HttpStatusCode.OK, tokenService.CreateToken(customer), correlationId);
        }
        catch (JsonException)
        {
            return Error(HttpStatusCode.BadRequest, "Corpo da requisição inválido.", correlationId);
        }
        catch (Exception exception)
        {
            context.Logger.LogError($"Authentication failed. CorrelationId={correlationId}; ErrorType={exception.GetType().Name}");
            return Error(HttpStatusCode.InternalServerError, "Não foi possível processar a autenticação.", correlationId);
        }
    }

    private static AuthenticationRequest ParseRequest(APIGatewayProxyRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.Body))
        {
            return JsonSerializer.Deserialize<AuthenticationRequest>(request.Body)
                ?? throw new JsonException();
        }

        var cpf = request.QueryStringParameters is not null &&
                  request.QueryStringParameters.TryGetValue("cpf", out var queryCpf)
            ? queryCpf
            : null;
        return new AuthenticationRequest(cpf ?? string.Empty);
    }

    private static APIGatewayProxyResponse Json(HttpStatusCode statusCode, object body, string correlationId)
    {
        return new APIGatewayProxyResponse
        {
            StatusCode = (int)statusCode,
            Body = JsonSerializer.Serialize(body),
            Headers = new Dictionary<string, string>
            {
                ["Content-Type"] = "application/json",
                ["X-Correlation-Id"] = correlationId
            }
        };
    }

    private static APIGatewayProxyResponse Error(HttpStatusCode statusCode, string message, string correlationId)
    {
        return Json(statusCode, new { error = message }, correlationId);
    }

    private static string Required(IReadOnlyDictionary<string, string?> settings, string key)
    {
        return settings.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value)
            ? value
            : throw new InvalidOperationException($"Missing required setting: {key}");
    }
}
