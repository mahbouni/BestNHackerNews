using BestNHackerNews;
using BestNHackerNews.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<HackerNewsOptions>(builder.Configuration.GetSection(HackerNewsOptions.SectionName));

builder.Services.AddHttpClient<IHackerNewsClient, HackerNewsClient>((sp, client) =>
{
    var options = sp.GetRequiredService<IOptions<HackerNewsOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(options.RequestTimeoutSeconds);
});

builder.Services.AddSingleton<IStoryCache, StoryCache>();
builder.Services.AddSingleton<IStoryRefresher, StoryRefresher>();
builder.Services.AddHostedService<StoryRefreshService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Eager fetch: warm the cache before the app starts accepting requests, so no
// caller ever sees an empty result. If this fails (e.g. Hacker News is unreachable),
// fail startup rather than silently serving an empty cache.
using (var scope = app.Services.CreateScope())
{
    var refresher = scope.ServiceProvider.GetRequiredService<IStoryRefresher>();
    var warmed = await refresher.RunOnceAsync(CancellationToken.None);
    if (!warmed)
    {
        throw new InvalidOperationException(
            "Failed to warm the story cache on startup; Hacker News may be unreachable.");
    }
}

app.Run();
