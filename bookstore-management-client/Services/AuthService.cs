using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private string? _token;

    public AuthService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string? Token => _token;

    public async Task<string?> Register(string email, string password)
    {
        var mutation = @"
        mutation ($email: String!, $password: String!) {
            register(email: $email, password: $password)
        }";

        var variables = new { email, password };
        var response = await _httpClient.PostAsJsonAsync("/graphql", new { query = mutation, variables });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GraphQLResponse<string>>();
        return result?.Data;
    }

    public async Task<string?> Login(string email, string password)
    {
        var mutation = @"
        mutation ($email: String!, $password: String!) {
            login(email: $email, password: $password)
        }";

        var variables = new { email, password };
        var response = await _httpClient.PostAsJsonAsync("/graphql", new { query = mutation, variables });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GraphQLResponse<string>>();
        _token = result?.Data;
        return _token;
    }

    public class GraphQLResponse<T>
    {
        public T? Data { get; set; }
    }
}
