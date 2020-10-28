using System;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Coravel;
using Coravel.Mailer.Mail;
using Coravel.Mailer.Mail.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace CoravelMailerBug
{
    public class Program
    {
        private static readonly object CoravelConfig = new
        {
            Coravel = new
            {
                Mail = new
                {
                    Driver = "FileLog"
                }
            }
        };

        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureHostConfiguration(configHost =>
                {
                    configHost.SetBasePath(Directory.GetCurrentDirectory());

                    configHost.AddJsonStream(
                        new MemoryStream(buffer:
                            JsonSerializer.SerializeToUtf8Bytes(CoravelConfig, typeof(object))
                        ));
                })
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddLogging();

                    services.AddRouting();
                    services.AddRazorPages();

                    services.AddMailer(hostContext.Configuration);
                    services.AddHostedService<Worker>();
                })
                .ConfigureWebHostDefaults(host =>
                {
                    host.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapRazorPages();
                        });
                    });
                });
    }

    public class Worker : IHostedService, IDisposable
    {
        private readonly IMailer _mailer;

        public Worker(IMailer mailer)
        {
            _mailer = mailer;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
            => await _mailer.SendAsync(
                new DemoMailable(
                    new MailableModel { MailBody = "LGTM" }));

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public void Dispose() {}
    }

    internal sealed class DemoMailable : Mailable<MailableModel>
    {
        private readonly MailableModel _model;

        public DemoMailable(MailableModel model)
        {
            _model = model;
        }

        public override void Build()
        {
            this.To("noreply@microsoft.com")
                .From("noreply@localhost.localdomain")
                .View("~/Views/Mail/DemoMail.cshtml", _model);
        }
    }

    public class MailableModel
    {
        public string MailBody { get; set; }
    }
}
