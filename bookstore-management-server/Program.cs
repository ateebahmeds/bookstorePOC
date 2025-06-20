
using bookstore_management_server.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using HotChocolate.AspNetCore;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Execution.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "server=localhost;database=bookstoredb;user=root;password=yourpassword";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddIdentity<IdentityUser, IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

var jwtKey = "super_secret_key_123!"; // Should be in config
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

builder.Services.AddAuthorization();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services
    .AddGraphQLServer()
    .AddQueryType(d => d.Name("Query"))
    .AddTypeExtension<BookQueries>()
    .AddMutationType(d => d.Name("Mutation"))
    .AddTypeExtension<BookMutations>()
    .AddTypeExtension<AuthMutations>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.UseCors();
app.MapGraphQL();

app.Run();

public class BookQueries : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Query");

        descriptor.Field("books")
            .ResolveWith<BookResolvers>(t => t.GetBooks(default!))
            .Type<ListType<ObjectType<bookstore_management_shared.Models.Book>>>();

        descriptor.Field("authors")
            .ResolveWith<BookResolvers>(t => t.GetAuthors(default!))
            .Type<ListType<ObjectType<bookstore_management_shared.Models.Author>>>();
    }
}

public class BookMutations : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Mutation");

        descriptor.Field("addBook")
            .Authorize() // Require authentication
            .Argument("title", a => a.Type<NonNullType<StringType>>())
            .Argument("isbn", a => a.Type<NonNullType<StringType>>())
            .Argument("authorId", a => a.Type<NonNullType<IntType>>())
            .Argument("price", a => a.Type<NonNullType<FloatType>>())
            .Argument("stock", a => a.Type<NonNullType<IntType>>())
            .ResolveWith<BookResolvers>(t => t.AddBook(default!, default!, default!, default!, default!, default!))
            .Type<ObjectType<bookstore_management_shared.Models.Book>>();
    }
}

public class AuthMutations : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        descriptor.Name("Mutation");

        descriptor.Field("register")
            .Argument("email", a => a.Type<NonNullType<StringType>>())
            .Argument("password", a => a.Type<NonNullType<StringType>>())
            .Resolve(async ctx =>
            {
                var email = ctx.ArgumentValue<string>("email");
                var password = ctx.ArgumentValue<string>("password");
                var userManager = ctx.Service<UserManager<IdentityUser>>();

                var user = new IdentityUser { UserName = email, Email = email };
                var result = await userManager.CreateAsync(user, password);

                if (!result.Succeeded)
                {
                    throw new GraphQLException(result.Errors.Select(e => new Error(e.Description)));
                }

                return "User registered successfully";
            });

        descriptor.Field("login")
            .Argument("email", a => a.Type<NonNullType<StringType>>())
            .Argument("password", a => a.Type<NonNullType<StringType>>())
            .Resolve(async ctx =>
            {
                var email = ctx.ArgumentValue<string>("email");
                var password = ctx.ArgumentValue<string>("password");
                var userManager = ctx.Service<UserManager<IdentityUser>>();
                var signInManager = ctx.Service<SignInManager<IdentityUser>>();

                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    throw new GraphQLException(new Error("Invalid login attempt"));
                }

                var result = await signInManager.CheckPasswordSignInAsync(user, password, false);
                if (!result.Succeeded)
                {
                    throw new GraphQLException(new Error("Invalid login attempt"));
                }

                var jwtKey = "super_secret_key_123!";
                var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var tokenDescriptor = new SecurityTokenDescriptor
                {
                    Subject = new ClaimsIdentity(new Claim[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id),
                        new Claim(ClaimTypes.Name, user.UserName)
                    }),
                    Expires = DateTime.UtcNow.AddHours(1),
                    SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(keyBytes), SecurityAlgorithms.HmacSha256Signature)
                };

                var token = tokenHandler.CreateToken(tokenDescriptor);
                var tokenString = tokenHandler.WriteToken(token);

                return tokenString;
            });
    }
}

public class BookResolvers
{
    public IQueryable<bookstore_management_shared.Models.Book> GetBooks(ApplicationDbContext context)
    {
        return context.Books.Include(b => b.Author);
    }

    public IQueryable<bookstore_management_shared.Models.Author> GetAuthors(ApplicationDbContext context)
    {
        return context.Authors;
    }

    public async Task<bookstore_management_shared.Models.Book> AddBook(
        string title,
        string isbn,
        int authorId,
        decimal price,
        int stock,
        ApplicationDbContext context)
    {
        var book = new bookstore_management_shared.Models.Book
        {
            Title = title,
            ISBN = isbn,
            AuthorId = authorId,
            Price = price,
            Stock = stock
        };
        context.Books.Add(book);
        await context.SaveChangesAsync();
        return book;
    }
}
