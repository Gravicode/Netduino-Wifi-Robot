using Microsoft.Owin;
using Owin;

namespace Robot.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.MapSignalR();
        }
    }
}
