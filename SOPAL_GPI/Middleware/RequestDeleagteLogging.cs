namespace SOPAL_GPI.MiddleWare
{
    public class RequestDeleagteLogging(RequestDelegate next)
    {
        public async Task InvokeAsync(HttpContext context)
        {
            var start = DateTimeOffset.Now;
            await next(context);
            Console.WriteLine($"Request response : {(DateTimeOffset.Now - start).TotalMilliseconds} ms");
        }
    }
}