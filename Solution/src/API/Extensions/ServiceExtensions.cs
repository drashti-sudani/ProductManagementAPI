using System;
using Application.Interfaces;
using Application.Validators;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentValidation;
using FluentValidation.AspNetCore;
using AutoMapper;

namespace API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUnitOfWork, Infrastructure.UnitOfWork>();

        services.AddScoped<RefreshTokenService>();
        services.AddSingleton<JwtTokenGenerator>();

        return services;
    }

    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // FluentValidation
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<ProductCreateValidator>();

        // Application services
        services.AddScoped<IProductService, Application.Services.ProductService>();

        // AutoMapper profiles in Application assembly
        services.AddAutoMapper(typeof(Application.Mapping.MappingProfile).Assembly);

        return services;
    }
}
