using DotNetEnv;
using eAutoskola.Contracts.Messaging;
using eAutoskola.Worker.Configuration;
using eAutoskola.Worker.Handlers;
using eAutoskola.Worker.Mail;
using eAutoskola.Worker.Messaging;

// .env must be loaded before any *Options class constructor reads an environment variable.
Env.TraversePath().Load();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton(new RabbitMqOptions());
builder.Services.AddSingleton(new SmtpOptions());
builder.Services.AddSingleton<RabbitMqConnectionProvider>();
builder.Services.AddScoped<IMailSender, SmtpMailSender>();

// One IMessageHandler<TMessage> per message type - the actual work (building + sending the
// e-mail). Registered Scoped since handlers are resolved from a fresh scope per message.
builder.Services.AddScoped<IMessageHandler<PasswordResetRequestedMessage>, PasswordResetRequestedHandler>();
builder.Services.AddScoped<IMessageHandler<LessonConfirmedMessage>, LessonConfirmedHandler>();
builder.Services.AddScoped<IMessageHandler<LessonRejectedMessage>, LessonRejectedHandler>();
builder.Services.AddScoped<IMessageHandler<LessonCancelledMessage>, LessonCancelledHandler>();
builder.Services.AddScoped<IMessageHandler<LessonCompletedMessage>, LessonCompletedHandler>();
builder.Services.AddScoped<IMessageHandler<PaymentReceivedMessage>, PaymentReceivedHandler>();
builder.Services.AddScoped<IMessageHandler<RefundIssuedMessage>, RefundIssuedHandler>();

// One BackgroundService per queue (REFERENCE_ANALYSIS.md §2.7 pattern) - the generic
// MessageConsumerWorker<TMessage> owns connect/declare/consume/retry/ack/dead-letter, sharing
// the single RabbitMqConnectionProvider connection via its own channel each.
builder.Services.AddHostedService<MessageConsumerWorker<PasswordResetRequestedMessage>>();
builder.Services.AddHostedService<MessageConsumerWorker<LessonConfirmedMessage>>();
builder.Services.AddHostedService<MessageConsumerWorker<LessonRejectedMessage>>();
builder.Services.AddHostedService<MessageConsumerWorker<LessonCancelledMessage>>();
builder.Services.AddHostedService<MessageConsumerWorker<LessonCompletedMessage>>();
builder.Services.AddHostedService<MessageConsumerWorker<PaymentReceivedMessage>>();
builder.Services.AddHostedService<MessageConsumerWorker<RefundIssuedMessage>>();

// An unrecoverable connection failure should stop the container rather than leave a zombie
// process - compose restarts it (`restart: unless-stopped`).
builder.Services.Configure<HostOptions>(options =>
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost);

var host = builder.Build();
host.Run();
