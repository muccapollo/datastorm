﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DataStorm.Web.Models
{
    public class TipologiaLavoro
    {
        public int Id { get; set; }
        public virtual string Codice { get; set; }
        public virtual string Descrizione { get; set; }
        
    }
}
