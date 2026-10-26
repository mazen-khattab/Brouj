# Part 8 API deployment configuration

The approved browser topology is `https://brouj.com` to `https://api.brouj.com`. Development permits `https://localhost:5173`; the existing HTTPS launch profile listens at `https://localhost:7249`. URLs, origins and proxy addresses remain configuration inputs.

## Required host and request configuration

| Key | Purpose / validation |
|---|---|
| `Api:BaseUrl` | Canonical external API HTTPS origin, without a trailing slash/path/query. Production defaults to the approved API origin; Development to the existing HTTPS launch address. Supplies the permitted host and HTTPS redirect port. Keep it aligned with the local launch profile / `ASPNETCORE_URLS`. |
| `AllowedHosts` | Framework Host filtering. Defaults to `api.brouj.com`, or `localhost` in Development. The API also validates the host against `Api:BaseUrl` before HTTPS redirects. |
| `Api:AllowedOrigins` | Exact frontend HTTPS origins, without trailing slashes, paths, credentials or wildcards. Production contains only `https://brouj.com`; Development overrides it with `https://localhost:5173`. |
| `Api:TrustedProxies` | Explicit proxy IP addresses. Empty by default. No proxy address or network is invented, and even loopback forwarding is disabled unless explicitly configured. |
| `Api:ForwardLimit` | Maximum trusted forwarding hops, positive; framework-compatible default of one. Set for the actual proxy chain. |
| `Api:JsonBodyLimitBytes` | Required positive total request-body budget for endpoints other than uploads. No production or development traffic estimate is committed. |
| `Api:MultipartBodyLimitBytes` | Required total multipart request budget. Must exceed the approved 5 MiB individual file cap to leave room for envelope headers/fields. The same budget applies when Content-Length is absent. |
| `Api:MaximumJsonDepth` | Required JSON nesting budget, positive and no greater than the framework safety cap of 64. Select for the documented DTO shapes. |
| `AuthenticationCookies:SameSite` | `Strict` for the approved HTTPS topology. The production subdomains share a site; HTTPS localhost ports also share a site. The existing cookie names use `__Host-`, `Secure`, `HttpOnly`, path `/`, and no Domain attribute. |

The approved origins are explicit environment defaults; proxy trust lists remain empty. All traffic and payload budgets are intentionally absent from application settings. Missing/invalid budgets stop startup in every environment, including Production, before the server accepts traffic. Validated API/rate configuration is captured at startup; changes require restarting the API.

Environment variable keys use double underscores, for example `Api__JsonBodyLimitBytes`, `Api__AllowedOrigins__0` and `Api__TrustedProxies__0`. Supply arrays completely for the selected environment; configuration providers merge array indexes rather than replacing entire arrays automatically.

JWT signing configuration, `ConnectionStrings:DefaultConnection`, an absolute safe `FileStorage:RootPath`, and the existing cache/storage configuration are still required by Infrastructure. Secrets belong in environment/secret providers. This Part adds no credentials or packages and changes no database schema.

## Required rate configuration

Every category requires both a positive integer `PermitLimit` and a positive integer `WindowSeconds`:

| Category prefix | Endpoints / purpose |
|---|---|
| `RateLimits:PublicReads` | Catalogue, other safe reads, health and Development OpenAPI. |
| `RateLimits:Authentication` | CSRF bootstrap, registration, login, refresh, logout, password changes and customer profile changes. |
| `RateLimits:ReservationWrites` | Customer reservation creation and cancellation. |
| `RateLimits:AdminWrites` | Administrative mutations except uploads. |
| `RateLimits:FileUploads` | Project, unit and stage-item image creation. |

For example, the environment key for the authentication permit budget is `RateLimits__Authentication__PermitLimit`. This document supplies no guessed production numbers. Synthetic budgets exist only in automated test configuration. Choose deployment values from measured traffic and load testing.

Fixed windows reject immediately without queueing. Buckets use the server-observed client IP after trusted forwarding, independently for each category. Header values from untrusted proxies cannot choose a bucket. Rejections return 429, a safe Problem Details body and Retry-After. These are per-instance controls; a deployment serving multiple instances needs matching gateway/WAF controls and must account for shared client addresses and instance count when measuring budgets. This Part does not claim a distributed quota or a throughput benchmark.

## Browser request sequence

1. Fetch `GET /api/v1/auth/csrf` with `credentials: "include"` over HTTPS. Read `data.requestToken` from its no-store JSON response; the framework also writes a Secure/HttpOnly antiforgery cookie.
2. Send that proof in `X-CSRF-TOKEN` on every POST/PATCH/PUT/DELETE, including registration/login/refresh/logout. Send `credentials: "include"` so the browser carries API host cookies.
3. Fetch a new proof after login/logout or any authenticated-identity change. Proofs are bound to the identity seen by authentication. Refresh can bootstrap while the access cookie is expired, using its refresh cookie and an anonymous proof.
4. Read session metadata from response DTOs; access and refresh credentials are never returned in JSON. The existing reader gives an explicit Authorization header precedence over cookies.

CORS permits credentials only for configured exact origins, approved HTTP methods and Content-Type / X-CSRF-TOKEN / Authorization headers. Disallowed preflights receive no permission headers. CORS is not a replacement for the required antiforgery proof. Keep both browser origins on HTTPS, including local development.

Before serving browser traffic from multiple instances, configure a shared, protected ASP.NET Core Data Protection key ring through the deployment host. This Part reuses the existing antiforgery services and does not provision a key store. Configure proxy/CDN request and upload budgets consistently with the API, keep upload storage outside executable directories, and retain Infrastructure's shared-storage requirements when scaling.

## Pipeline and production exposure

The explicit order is forwarded headers → security response headers → safe request logging → sanitized exception handling/status pages → canonical host check → HSTS outside Development → HTTPS redirection → routing → CORS → endpoint rate limiting → request byte limits → authentication → existing CSRF validation → authorization → controllers.

Forwarding processes only client IP and scheme, with no forwarded Host consumption. Rate policies depend on routing metadata and client IP, so they execute after routing/CORS and before JWT/handler work. Byte limits execute before CSRF can inspect a form. CSRF executes after authentication so proofs are checked against the correct identity, and before authorization/endpoints so login and expired-access refresh cannot bypass it. These choices were checked against the [ASP.NET Core proxy middleware guidance](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/proxy-load-balancer?view=aspnetcore-10.0) and exercised by actual WebApplicationFactory pipeline tests.

Responses receive no-store, nosniff, frame denial, a restrictive API CSP, no-referrer and disabled browser sensor permissions. Security-sensitive logs contain route templates, status/error codes, timing and server trace IDs. They exclude raw paths/query strings, bodies, exception messages, credentials and identity numbers. Framework request/antiforgery diagnostics that could echo those inputs are suppressed; EF/JWT sanitization from earlier Parts remains active. Do not enable request-body/header logging, EF sensitive-data logging or IdentityModel PII logging in deployment.

OpenAPI is mapped only in Development. Production exposes no schema or Swagger UI. `/health/live` returns fixed liveness only and does not query SQL Server, report readiness or disclose dependency/configuration details. Authenticated/private DTOs and Set-Cookie responses are never cached; public read caching remains an explicit future deployment/application decision.

The Development Swagger UI is served at `/index.html` and loads `/swagger/v1/swagger.json`. Swagger middleware is registered once inside the Development condition. Only Development documentation paths receive the CSP exception for same-origin scripts/styles/API requests and Swagger's inline initialization; API responses and all Production responses retain the restrictive CSP. Other security headers remain active on the documentation UI.
