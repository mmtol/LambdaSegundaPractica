using Amazon.Lambda.Core;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using OpenAI;
using OpenAI.Chat;
using System.ClientModel;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

public class Function
{
    private const string deploymentName = "conciertos-bot";

    public async Task<string> FunctionHandler(dynamic input, ILambdaContext context)
    {
        string question = input?.question;

        if (string.IsNullOrEmpty(question))
            return "No question provided";

        // 🔐 1. Obtener secretos
        var secrets = await GetSecrets();

        string endpoint = secrets["endpoint"];
        string apiKey = secrets["apiKey"];

        // 🤖 2. Cliente IA
        var client = new ChatClient(
            credential: new ApiKeyCredential(apiKey),
            model: deploymentName,
            options: new OpenAIClientOptions
            {
                Endpoint = new Uri(endpoint)
            });

        // 💬 3. Pregunta a la IA
        var completion = client.CompleteChat([
            new SystemChatMessage("Eres un asistente experto en conciertos y eventos musicales."),
            new UserChatMessage(question)
        ]);

        return completion.Value.Content[0].Text;
    }

    private async Task<Dictionary<string, string>> GetSecrets()
    {
        var client = new AmazonSecretsManagerClient();

        var response = await client.GetSecretValueAsync(new GetSecretValueRequest
        {
            SecretId = "ai-foundry-secrets"
        });

        return JsonSerializer.Deserialize<Dictionary<string, string>>(response.SecretString);
    }
}