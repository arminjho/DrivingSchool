using DotNetEnv;
using eAutoskola.Worker;
using eAutoskola.Worker.Configuration;
using eAutoskola.Worker.Mail;
using eAutoskola.Worker.Messaging;
using Microsoft.Extensions.Hosting;

// .env must be loaded before any *Options class constructor reads an environment variable.
Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(new RabbitMqOptions());
builder.Services.AddSingleton(new SmtpOptions());
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddScoped<IMailSender, SmtpMailSender>();

builder.Services.AddHostedService<MailQueueConsumerWorker>();

// An unrecoverable connection failure should stop the container rather than leave a zombie
// process - compose restarts it (`restart: unless-stopped`).
builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

var host = builder.Build();
host.Run();
