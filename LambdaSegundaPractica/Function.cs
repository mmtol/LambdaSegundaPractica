using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace LambdaSegundaPractica;

public class Function
{
    private const string SecretName = "ai-foundry-secrets";

    public async Task<string> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation("BODY: " + request.Body);

            if (string.IsNullOrWhiteSpace(request.Body))
                return "ERROR: body vacío";

            var bodyJson = JsonSerializer.Deserialize<JsonElement>(request.Body);

            if (!bodyJson.TryGetProperty("question", out var q))
                return "ERROR: no viene question";

            string question = q.GetString();

            var (endpoint, apiKey) = await GetSecretsAsync();

            ChatClient client = new(
                credential: new ApiKeyCredential(apiKey),
                model: "gpt-4.1",
                options: new OpenAIClientOptions()
                {
                    Endpoint = new Uri(endpoint)
                });

            ChatCompletion completion = await client.CompleteChatAsync(
            [
                new SystemChatMessage("Eres un ayudante para un examen de programación"),
                new UserChatMessage(question),
            ]);

            return string.Concat(completion.Content.Select(c => c.Text));
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex.ToString());
            return "ERROR INTERNO: " + ex.Message;
        }
    }

    private async Task<(string endpoint, string apiKey)> GetSecretsAsync()
    {
        var client = new AmazonSecretsManagerClient();

        var response = await client.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = SecretName
        });

        var json = JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);

        return (json["endpoint"], json["apiKey"]);
    }
}