namespace SOPAL_GPI.MiddleWare
{
    public class ExceptionHandling(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await next(context);
            }
            catch (Exception ex)
            {
                context.Response.StatusCode = 500;
                await context.Response.WriteAsJsonAsync(new { error = ex.Message });
                Console.WriteLine(ex.ToString());
            }
        }
    }
}

