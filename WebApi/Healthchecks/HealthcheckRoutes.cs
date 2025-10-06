namespace WebApi.Healthchecks;

public static class HealthcheckRoutes
{
    extension(IEndpointRouteBuilder api)
    {
        public IEndpointRouteBuilder MapHealthEndpoints()
        {
            var healthApi = api.MapGroup("/health");

            healthApi.MapGet("/", () => Results.Ok(new { Healthy = true }));

            return api;
        }
    }
}