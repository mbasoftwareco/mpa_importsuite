using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(PANGEA.IMPORTSUITE.WebApp.Startup))]
namespace PANGEA.IMPORTSUITE.WebApp
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {            
            ConfigureAuth(app);
        }
    }
}
