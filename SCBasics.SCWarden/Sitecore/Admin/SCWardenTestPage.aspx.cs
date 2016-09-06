using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SCBasics.SCWarden.Sitecore.Admin
{
    public partial class SCWardenTestPage : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
        }

        protected void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                SCWarden.Service.WardenService wardenService = new Service.WardenService();
                wardenService.StartAsync();
                Response.Write("Done");
            }
            catch (Exception ex)
            {
                Response.Write("{ex.Message}");
            }
        }

        private void MyMethod(Func<object, Task<object>> p)
        {
            throw new NotImplementedException();
        }
    }
}