using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Kritner.Mtls.Core;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Kritner.Mtls
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddAuthentication(CertificateAuthenticationDefaults.AuthenticationScheme)
                .AddCertificate(options =>
                {
                    // Only allow chained certs, no self signed
                    options.AllowedCertificateTypes = CertificateTypes.Chained;
                    // Don't perform the check if a certificate has been revoked - requires an "online CA", which was not set up in our case.
                    options.RevocationMode = X509RevocationMode.NoCheck;
                    options.Events = new CertificateAuthenticationEvents()
                    {
                        OnAuthenticationFailed = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetService<ILogger<Startup>>();

                            logger.LogError(context.Exception, "Failed auth.");

                            return Task.CompletedTask;
                        },
                        OnCertificateValidated = context =>
                        {
                            var logger = context.HttpContext.RequestServices.GetService<ILogger<Startup>>();
                            logger.LogInformation("Within the OnCertificateValidated portion of Startup");

                            var caValidator = context.HttpContext.RequestServices.GetService<ICertificateAuthorityValidator>();
                            if (!caValidator.IsValid(context.ClientCertificate))
                            {
                                const string failValidationMsg = "The client certificate failed to validate";
                                logger.LogWarning(failValidationMsg);
                                context.Fail(failValidationMsg);
                            }

                            return Task.CompletedTask;
                        }
                    };
                });
            
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();
            app.UseHsts();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
