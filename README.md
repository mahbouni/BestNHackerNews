# BestNHackerNews

An ASP.NET Core Web API that returns the top n Hacker News "best stories", ranked by score descending.

## Running it

Requires the .NET 10 SDK.

```
dotnet run --project src/BestNHackerNews
```

On startup the app fetches the current best-stories list from Hacker News and populates its cache before it starts accepting requests, so the first call never sees an empty result. This takes a few seconds.

Once it says `Application started`, the console output tells you which ports it's listening on (e.g. `https://localhost:7100` and `http://localhost:5080` — check the actual `Now listening on:` lines.

From there:

- `GET https://localhost:7100/api/stories/best?n=10` — the endpoint itself
- `https://localhost:7100/swagger` — interactive Swagger UI

### Tests

```
dotnet test
```

## API

**`GET /api/stories/best?n={n}`**

Returns the top n best stories by score, descending:

```
[
  {
    "title": "QBittorrent breaks out of sandbox to commit crimes",
    "uri": "https://beige.party/@intransitivelie/117057396732763183",
    "postedBy": "mraniki",
    "time": "2026-09-06T13:02:41+00:00",
    "score": 1236,
    "commentCount": 272
  }
]
```

- n must be a positive integer, or the API returns 400 Bad Request.
- If n exceeds the number of stories currently available, the API returns all available stories rather than erroring.

## How it works

Hacker News' beststories.json only returns IDs, not scores - there's no way to know which n are best without fetching every story's details first, and no bulk endpoint to do that in one call. So instead of hitting Hacker News on every incoming request (which would scale load with caller traffic), a background service refreshes an in-memory cache on a fixed interval (default 60s, configurable), and all API requests are served from that cache:

1. Fetch the current best-story IDs.
2. Fetch details for each one, with a concurrency cap (default 20 in flight at once) so a refresh cycle doesn't fan out hundreds of simultaneous requests to Hacker News.
3. Build a fresh, complete snapshot off to the side, then atomically swap it in - readers never see a partially updated cache, and never block while the next snapshot is being built.

A few resilience details:
- **Cold start*: the first refresh runs synchronously before the app starts accepting traffic.
- **Overlapping refreshes**: if a cycle is still running when the next tick fires, that tick is skipped rather than running concurrently.
- **Failures**: if any part of a refresh cycle fails (Hacker News unreachable, a request times out, etc.), the whole cycle is abandoned and the previous good snapshot stays live — the next tick tries again. Stale-but-consistent is preferred over serving a partially-refreshed result.

## Assumptions

- The exact size of the best-stories list isn't assumed or hardcoded anywhere (observed to currently be ~200, but the code doesn't rely on that).
- A story missing a url (e.g. a "Ask Hacker News" text post) falls back to its Hacker News discussion page, configurable via `HackerNews:DiscussionUrlTemplate`.
- Single-instance deployment — the cache lives in-process, so this doesn't share state across multiple running instances.

## Given more time

- **Distributed cache** (e.g. Redis) instead of an in-process snapshot, so the app could scale to multiple instances without each one independently polling Hacker News.
- **Tiered refresh cadence**: the ID-list check is cheap (one call); re-fetching every cached item's details is the expensive part. These currently run together on one cadence. Splitting them (e.g. check membership every 60s, refresh item details every few minutes) would reduce Hacker News load further at larger scale.
- **Retry/circuit-breaker policies** (e.g. via Polly) around the Hacker News calls, instead of relying on the next scheduled tick as a coarse retry.
- **Partial-cycle tolerance**: currently a single failed item fetch aborts the whole refresh cycle. An alternative is to build the snapshot from whatever succeeded and only skip the failed items, trading some completeness for freshness.
