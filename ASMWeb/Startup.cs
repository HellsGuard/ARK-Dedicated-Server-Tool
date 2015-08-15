using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(ASMWeb.Startup))]
namespace ASMWeb
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}
