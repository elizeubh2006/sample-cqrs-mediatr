using Microsoft.EntityFrameworkCore;
using Sample.MediatR.Application;
using Sample.MediatR.Application.UseCases.Product.Get;
using Sample.MediatR.Persistence.Context;
using Sample.MediatR.WebApi.Core.Extensions;
using Serilog;
using Nest;
using Sample.MediatR.Persistence.Elasticsearch;
using MassTransit;
using Sample.MediatR.Application.Consumers.SendEmail;
using Sample.MediatR.Application.Consumers.IndexClientProducts; // Certifique-se de importar o namespace correto

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configuração do Serilog
    Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

    builder.Host.UseSerilog();

    builder.Services.AddApiConfiguration();

    builder.Services.AddDbContext<ClientContext>(opt => opt.UseInMemoryDatabase("ClientContext"));
    builder.Services.AddMediatR(configuration => configuration.RegisterServicesFromAssemblyContaining(typeof(GetProductsQuery)));
    builder.Services.AddAutoMapper(typeof(MapperProfile));

    // Configuração do Elasticsearch
    var elasticUri = builder.Configuration["ElasticsearchSettings:uri"];
    var settings = new ConnectionSettings(new Uri(elasticUri))
    .DefaultIndex(builder.Configuration["ElasticsearchSettings:defaultIndex"]);
    var client = new ElasticClient(settings);
    builder.Services.AddSingleton<IElasticClient>(client);

    // Registro do repositório Elasticsearch
    //builder.Services.AddScoped(typeof(IBaseElasticRepository<>), typeof(BaseElasticRepository<>));
    builder.Services.AddElasticsearch(builder.Configuration);

    // Configuração do MassTransit com RabbitMQ
    builder.Services.AddMassTransit(x =>
    {
        x.AddDelayedMessageScheduler();
        x.SetKebabCaseEndpointNameFormatter();

        x.AddConsumer<SendEmailConsumerHandler>(typeof(SendEmailConsumerHandlerDefinition));
        x.AddConsumer<IndexClientProductConsumerHandler>(typeof(IndexClientProductConsumerDefinition));

        x.UsingRabbitMq((ctx, cfg) =>
        {
            cfg.UseDelayedMessageScheduler();
            cfg.Host(builder.Configuration.GetConnectionString("RabbitMQ"));
            cfg.ConfigureEndpoints(ctx, new KebabCaseEndpointNameFormatter("dev", false));
            cfg.UseMessageRetry(retry => { retry.Interval(3, TimeSpan.FromSeconds(5)); });
        });
    });

    var app = builder.Build();
    app.UseApiConfiguration();
    app.UseElasticApm(builder.Configuration);

    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
}
finally
{
    Log.Information("Server Shutting down...");
    Log.CloseAndFlush();
}
