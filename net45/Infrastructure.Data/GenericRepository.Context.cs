using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Data
{
    public partial class GenericRepository
    {
        public bool LazyLoadingEnabled 
        {
            get { return _context.Configuration.LazyLoadingEnabled; }
            set { _context.Configuration.LazyLoadingEnabled = value; }
        }

        public bool ProxyCreationEnabled
        {
            get { return _context.Configuration.ProxyCreationEnabled; }
            set { _context.Configuration.ProxyCreationEnabled = value; }
        }
    }
}
