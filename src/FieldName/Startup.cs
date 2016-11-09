using System;
using System.Threading.Tasks;
using FieldName.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FieldName
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc(options => options.ModelBinderProviders.Insert(0, new ModelModelBinderProvider()));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));
            loggerFactory.AddDebug();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        private class ModelModelBinder : IModelBinder
        {
            private readonly IModelBinder _binder;

            public ModelModelBinder(IModelBinder binder)
            {
                if (binder == null)
                {
                    throw new ArgumentNullException(nameof(binder));
                }

                _binder = binder;
            }

            public async Task BindModelAsync(ModelBindingContext bindingContext)
            {
                if (bindingContext == null)
                {
                    throw new ArgumentNullException(nameof(bindingContext));
                }

                var fieldName = nameof(Model.Value);
                var modelName = ModelNames.CreatePropertyModelName(bindingContext.ModelName, fieldName);
                ModelBindingResult valueResult;
                using (bindingContext.EnterNestedScope(
                    bindingContext.ModelMetadata.Properties[fieldName],
                    fieldName,
                    modelName,
                    model: null))
                {
                    await _binder.BindModelAsync(bindingContext);
                    valueResult = bindingContext.Result;
                }

                if (!valueResult.IsModelSet)
                {
                    return;
                }

                var model = new Model
                {
                    FieldName = bindingContext.FieldName,
                    Value = (string)valueResult.Model,
                };

                bindingContext.Result = ModelBindingResult.Success(model);
            }
        }

        private class ModelModelBinderProvider : IModelBinderProvider
        {
            public IModelBinder GetBinder(ModelBinderProviderContext context)
            {
                if (context == null)
                {
                    throw new ArgumentNullException(nameof(context));
                }

                if (context.Metadata.ModelType == typeof(Model))
                {
                    var metadata = context.Metadata.Properties[nameof(Model.Value)];
                    var binder = context.CreateBinder(metadata);

                    return new ModelModelBinder(binder);
                }

                return null;
            }
        }
    }
}
