using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.JSInterop;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly IJSRuntime _jsRuntime;
    private string? _token;

    public AuthService(HttpClient httpClient, IJSRuntime jsRuntime)
    {
        _httpClient = httpClient;
        _jsRuntime = jsRuntime;
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

        var result = await response.Content.ReadFromJsonAsync<GraphQLResponse<RegisterData>>();
        return result?.Data?.Register;
    }

    public async Task<string?> Login(string email, string password)
    {
        var mutation = @"
        mutation ($email: String!, $password: String!) {
            login(email: $email, password: $password) {
                token
            }
        }";

        var variables = new { email, password };
        var response = await _httpClient.PostAsJsonAsync("/graphql", new { query = mutation, variables });
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<GraphQLResponse<LoginData>>();
        _token = result?.Data?.Login?.Token;
        if (!string.IsNullOrEmpty(_token))
        {
            await _jsRuntime.InvokeVoidAsync("sessionStorage.setItem", "authToken", _token);
        }
        return _token;
    }

    public async Task Logout()
    {
        _token = null;
        await _jsRuntime.InvokeVoidAsync("sessionStorage.removeItem", "authToken");
    }

    public async Task<string?> GetTokenFromSession()
    {
        _token = await _jsRuntime.InvokeAsync<string>("sessionStorage.getItem", "authToken");
        return _token;
    }

    public class GraphQLResponse<T>
    {
        public T? Data { get; set; }
    }

    public class RegisterData
    {
        public string Register { get; set; } = string.Empty;
    }

    public class LoginData
    {
        public LoginPayload? Login { get; set; }
    }

    public class LoginPayload
    {
        public string Token { get; set; } = string.Empty;
    }
}
