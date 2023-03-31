using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PANGEA.IMPORTSUITE.ErpFactory.MPA_WS
{
    public class MPA_WS : ConnectFactory
    {

        public override IProcessService ErpService()
        {
            return new ErpService();
        }


    }
}
