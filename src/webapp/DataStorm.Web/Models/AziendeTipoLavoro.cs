﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataStorm.Web.Models
{
    public class AziendeTipoLavoro
    {
        public virtual int Id { get; set; }
        public virtual Azienda AziendaLavoro { get; set; }
        public virtual TipologiaLavoro TipoLavoro { get; set; }
    }
}
