﻿using AutoMapper;
using DataStorm.Web.Data;
using DataStorm.Web.Models;
using DataStorm.Web.Models.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web.Http;

namespace DataStorm.Web.Controllers.API
{
    public class WebApiController : ApiController
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<Utente> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public WebApiController(ApplicationDbContext db, UserManager<Utente> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;
            _userManager = userManager;
            _roleManager = roleManager;

            db.Seed(userManager, roleManager);
        }
        private async Task<string> InvokeRequestResponseService(string numeroDiPiani, string proprieta, string annoDiCostruzione, string costruzione, string percentualeUtilizzo, string uso, string posizione, string cateneCordoli, string comune)
        {
            using (var client = new HttpClient())
            {
                var scoreRequest = @"{
  ""Inputs"": {
    ""input1"": {
      ""ColumnNames"": [
        ""NumeroDiPiani"",
        ""Proprieta"",
        ""AnnoDiCostruzione"",
        ""Costruzione"",
        ""PercentualeUtilizzo"",
        ""Uso"",
        ""Posizione"",
        ""CateneCordoli"",
        ""Agibilita"",
        ""Comune""
      ],
      ""Values"": [
        [
          """+numeroDiPiani+@""",
          """+proprieta+@""",
          """+annoDiCostruzione+ @""",
          """+costruzione+ @""",
          """+percentualeUtilizzo+ @""",
          """+uso+ @""",
          """+posizione+ @""",
          """+cateneCordoli+ @""",
          null,
          """+comune+@"""
        ]
      ]
    }
  },
  ""GlobalParameters"": {}
}";

                const string apiKey = "vWUGp6GZ80V4VeMoY/j1DXYpiqRB6GUZkS+mTLkAOX8ma+S8UVCSv9/7iHXkbggq+YB6ee8vEBNvLQL3P67naA=="; // Replace this with the API key for the web service
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

                client.BaseAddress = new Uri("https://ussouthcentral.services.azureml.net/workspaces/9da97d27abde423ab8df23c86c5ce635/services/0f6a6bf2c33f4471991d6c203d2b9398/execute?api-version=2.0&details=true");

                // WARNING: The 'await' statement below can result in a deadlock if you are calling this code from the UI thread of an ASP.Net application.
                // One way to address this would be to call ConfigureAwait(false) so that the execution does not attempt to resume on the original context.
                // For instance, replace code such as:
                //      result = await DoSomeTask()
                // with the following:
                //      result = await DoSomeTask().ConfigureAwait(false)

                HttpResponseMessage response =await  client.PostAsync("", new StringContent(scoreRequest, System.Text.Encoding.UTF8, "application/json"));
                if (response.IsSuccessStatusCode)
                {
                    string result = await response.Content.ReadAsStringAsync();
                    return result.Trim('{').Trim('}').Trim(']').Split(',').Last().Trim('\"');
                }
                else
                {
                    throw new Exception("Richiesta non valida");
                }
            }
        }
        private async Task<TipoAgibilita> RichiediValutazione(string numeroDiPiani, string proprieta, string annoDiCostruzione, string costruzione, string percentualeUtilizzo, string uso, string posizione, string cateneCordoli, string comune)
        {
            var risposta = await InvokeRequestResponseService(numeroDiPiani, proprieta, annoDiCostruzione, costruzione, percentualeUtilizzo, uso, posizione, cateneCordoli, comune);
            TipoAgibilita agibilita;
            switch (risposta)
            {
                case "A":
                    agibilita = TipoAgibilita.A_Agibile;
                    break;
                case "B":
                    agibilita = TipoAgibilita.B_AgibileConProntoIntervento;
                    break;
                case "C":
                    agibilita = TipoAgibilita.C_ParzialmenteInagibile;
                    break;
                case "D":
                    agibilita = TipoAgibilita.D_TemporaneamenteInagibile;
                    break;
                case "E":
                    agibilita = TipoAgibilita.E_Inagibile;
                    break;
                case "F":
                    agibilita = TipoAgibilita.F_InagibilePerRischioEsterno;
                    break;
                default:
                    agibilita = TipoAgibilita.InformazioneMancante;
                    break;
            }
            return agibilita;
        }

        [Authorize]
        [Route("api/putimmobile")]
        [HttpPut]
        public async Task<HttpResponseMessage> PutImmobile(ImmobileDTO immobile)
        {
            try
            {
                if(string.IsNullOrEmpty(immobile.Indirizzo)||string.IsNullOrEmpty(immobile.Comune))
                {
                    throw new Exception("Immettere i dati necessari");
                }
                var utente = await _userManager.FindByNameAsync(User.Identity.Name);
                Immobile nuovoImmobile = Mapper.Map<ImmobileDTO, Immobile>(immobile);
                nuovoImmobile.UtenteAppartenenza = utente;
                var valutazione=await RichiediValutazione(nuovoImmobile.NumeroDiPiani, nuovoImmobile.Proprieta, nuovoImmobile.AnnoDiCostruzione, nuovoImmobile.Costruzione, nuovoImmobile.PercentualeUtilizzo, nuovoImmobile.Uso,nuovoImmobile.Posizione,nuovoImmobile.CateneCordoli,nuovoImmobile.Comune);
                _db.Immobili.Add(
                    nuovoImmobile
                    );
                nuovoImmobile.TipoAgibilita = valutazione;
                await _db.SaveChangesAsync();

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
        [HttpPost]
        public async Task<ActionResult> EditImmobile(ImmobileDTO immobile)
        {
          
            var utente = await _userManager.FindByNameAsync(User.Identity.Name);

            Immobile immobileCoinvolto = _db.Immobili.Single(i => i.Id == immobile.Id);
            if (immobileCoinvolto.UtenteAppartenenza.Id != utente.Id)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return InternalServerError(new Exception("Immobile non valido"));
            }
            Mapper.Map(immobile, immobileCoinvolto);
            await _db.SaveChangesAsync();
            return Ok();
           
        }
        [HttpDelete]
        public async Task<ActionResult> DeleteImmobile(int Id)
        {
            var utente = await _userManager.FindByNameAsync(User.Identity.Name);

            Immobile immobileCoinvolto = _db.Immobili.Single(i => i.Id == Id);
            if (immobileCoinvolto.UtenteAppartenenza.Id != utente.Id)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                return InternalServerError(new Exception("Immobile non valido"));
            }
            else
            {
                _db.Immobili.Remove(immobileCoinvolto);
            }
            return Ok();
        }
        
        [HttpGet]
        [Route("api/immobili/{id}")]
        public async Task<ImmobileDTO> GetImmobile(int Id)
        {
            try
            {
                var utente = await _userManager.FindByNameAsync(User.Identity.Name);
                var immobile = await _db.Immobili.SingleAsync(im => im.Id == Id);

                if (immobile.UtenteAppartenenza.Id != utente.Id)
                {
                    throw new Exception("Immobile non trovato");

                }
                else
                {
                    var result = Mapper.Map<ImmobileDTO>(immobile);
                    var rnd = new Random();
                    var rand = rnd.Next(1, 6);
                    result.TipoAgibilita = ((TipoAgibilita)rand).ToString();
                    return result;
                }
            }
            catch
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nella richiesta immobile");
            }
        }
        [Authorize]
        [Route("api/immobili")]
        public async Task<IEnumerable<ImmobileDTO>> GetImmobili()
        {
            try
            {
                var utente = await _userManager.FindByNameAsync(User.Identity.Name);
                if (_db.Immobili.Count() > 0)
                {
                    var immobili = _db.Immobili
                        .Where(i => i.UtenteAppartenenza == utente).ToList();
                    return immobili.Select(i =>
                            Mapper.Map<ImmobileDTO>(i)
                        );
                }
                else
                {
                    return new List<ImmobileDTO>();
                }
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nella richiesta immobili");
            }
        }

        [Route("api/immobili/tipologie")]
        public async Task<IEnumerable<KeyValuePair<int, string>>> GetTipologieImmobili()
        {
            try
            {
                await Task.FromResult(0);
                var tipologie = Enum.GetValues(typeof(TipologiaImmobile)).Cast<TipologiaImmobile>().ToArray();
                return tipologie.Select(t => new KeyValuePair<int, string>((int)t, t.ToString()));
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nella richiesta tipologie immobili");
            }
        }

        [HttpPut]
        [Route("api/avviso")]
        public async Task<HttpResponseMessage> PutAvviso(AvvisoDTO avviso)
        {
            try
            {
                _db.Avvisi.Add(new Avviso
                {
                    Titolo = avviso.Titolo,
                    Descrizione = avviso.Descrizione,
                    ImmaginiAvviso = avviso.ImmaginiAvviso == null ? new List<ImmagineAvviso> { } : avviso.ImmaginiAvviso.Select(i => new ImmagineAvviso
                    {
                        TitoloImmagine = i.TitoloImmagine,
                        UrlImmagine = i.UrlImmagine,
                        Larghezza = i.Larghezza,
                        Altezza = i.Altezza
                    }).ToList(),
                    Links = avviso.Links == null ? new List<LinkAvviso> { } : avviso.Links.Select(l => new LinkAvviso
                    {
                        Titolo = l.Titolo,
                        Url = l.Url
                    }).ToList(),
                    AreeMappe = avviso.AreeMappe == null ? new List<AreaMappa> { } : avviso.AreeMappe.Select(a => new AreaMappa
                    {
                        TipoMappa = (TipoAreaMappa)Enum.Parse(typeof(TipoAreaMappa), a.TipoMappa),
                        PuntiMappa = a.PuntiMappa.Select(p => new PuntoMappa
                        {
                            LatitudinePunto = p.LatitudinePunto,
                            LongitudinePunto = p.LongitudinePunto
                        }).ToList()
                    }).ToList(),
                    AvvisiTopics = new List<AvvisoTopic> { }
                });

                await _db.SaveChangesAsync();
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch (Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nell'inserimento dell'avviso");
            }
        }

        [HttpGet]
        [Route("api/avvisi")]
        public async Task<IEnumerable<AvvisoDTO>> GetAvvisi()
        {
            try
            {
                var avvisi = _db.Avvisi.Select(av => Mapper.Map<AvvisoDTO>(av));
                await Task.FromResult(0);
                return avvisi;
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nella richiesta degli avvisi");
            }
            //return await _db.Avvisi.Select(a => a.ToDTO()).ToListAsync();
        }

        [Route("api/avvisi/{id}")]
        [HttpGet]
        public async Task<AvvisoDTO> GetAvviso(int Id)
        {
            try
            {
                var avviso = await _db.Avvisi.SingleAsync(a => a.Id == Id);
                return Mapper.Map<AvvisoDTO>(avviso);
                //return await _db.Avvisi.First(a => a.Id == ID).ToDTO();
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nella richiesta dell'avviso");
            }
        }

        [HttpPut]
        [Route("api/segnalazione")]
        public async Task<HttpResponseMessage> PutSegnalazione(SegnalazioneDTO segnalazione)
        {
            try
            {
                var utente = await _userManager.FindByNameAsync(User.Identity.Name);
                _db.Segnalazioni.Add(new Segnalazione
                {
                    Descrizione = segnalazione.Descrizione,
                    TipoSegnalazione = segnalazione.TipoSegnalazione,
                    UtenteSegnalazione = utente
                });
                await _db.SaveChangesAsync();

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nell'inserimento della segnalazione");
            }
        }

        [Route("api/topics")]
        public async Task<IEnumerable<TopicDTO>> GetTopics(string ricerca)
        {
            try
            {
                var result = _db.Topics.Where(t => t.Codice.Contains(ricerca)).Select(t => Mapper.Map<TopicDTO>(t));

                await Task.FromResult(0);
                return result;
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nella ricerca dei topic");
            }
        }
        [Route("api/topics/{id}")]

        public async Task<TopicDTO> GetTopic(int Id)
        {
            try
            {
                var topic = await _db.Topics.SingleAsync(t => t.Id == Id);
                return Mapper.Map<TopicDTO>(topic);
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nella richiesta del topic");
            }
        }
        [HttpPut]
        [Route("api/topic/addtopic")]
        public async Task<IActionResult> PutTopic(string topic)
        {
            try
            {
                if (topic.Length >= 2)
                {
                    var utente = await _userManager.FindByNameAsync(User.Identity.Name);
                    var roles = await _userManager.GetRolesAsync(utente);
                    if (roles.Contains(Ruolo.PA.ToString()) || roles.Contains(Ruolo.ProtezioneCivile.ToString()))
                    {
                        return InternalServerError(new Exception("Sessione scaduta"));
                    }
                    Topic NuovoTopic = new Topic();
                    NuovoTopic.Codice = topic;
                    Topic topicPresente = await _db.Topics.SingleOrDefaultAsync(t => t.Codice == topic);
                    if (topicPresente != null)
                    {
                        throw new Exception("Topic già presente");
                    }
                    else
                    {
                        _db.Topics.Add(NuovoTopic);
                        _db.SaveChanges();
                    }
                    return Ok();
                }
                else
                {
                    return new EmptyResult();
                }
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nell'inserimento del topic");
            }
        }
        [HttpDelete]
        [Route("api/topic/delete")]
        public async Task<IActionResult> RemoveTopic(int Id)
        {
            try
            {
                var utente = await _userManager.FindByNameAsync(User.Identity.Name);
                var roles = await _userManager.GetRolesAsync(utente);
                if (roles.Contains(Ruolo.PA.ToString()) || roles.Contains(Ruolo.ProtezioneCivile.ToString()))
                {
                    ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    return InternalServerError(new Exception("Sessione scaduta"));
                }
                var topic = _db.Topics.Single(t => t.Id == Id);
                _db.Topics.Remove(topic);
                return Ok();
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Impossibile rimuovere il topic");
            }
        }
        [HttpGet]
        [Route("api/avvisi/topic")]
        public async Task<IEnumerable<AvvisoDTO>> GetAvvisiByTopic(string ricerca)
        {
            try
            {
                await Task.FromResult(0);
                var avvisi = _db.Avvisi.Where(av => av.AvvisiTopics.Any(avt => avt.TopicRiferimento.Codice == ricerca));
                return avvisi.Select(av => Mapper.Map<AvvisoDTO>(av));
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Impossibile ottenere gli avvisi");
            }
        }
        [Route("api/richiesta")]
        public async Task PostRichiesta()
        {
            
            await Task.FromResult(0);
        }

        [Route("api/richieste")]
        public async Task<IEnumerable<string>> GetRichieste()
        {
            await Task.FromResult(0);
            return null;
        }

        

        [Route("api/elementi-mappa")]
        public async Task<IEnumerable<AreaMappaDTO>> GetAreeMappa(PuntoMappaDTO puntoNordEst,PuntoMappaDTO puntoSudOvest)
        {
            await Task.FromResult(0);
            try
            {
                var areeMappa = _db.AreeMappa.Where(am =>
                  am.PuntiMappa.All(
                      pm => pm.LatitudinePunto >= puntoNordEst.LatitudinePunto
                      && pm.LongitudinePunto <= puntoNordEst.LongitudinePunto
                      &&pm.LatitudinePunto<=puntoSudOvest.LongitudinePunto
                      &&pm.LongitudinePunto>=puntoSudOvest.LongitudinePunto
                ));
                return areeMappa.Select(am => Mapper.Map<AreaMappaDTO>(am));
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nella richiesta per le aree mappa");
            }
        }
        [Route("api/areamappa/{id}")]
        [HttpGet]
        public async Task<AreaMappaDTO> GetAreaMappa(int Id)
        {
            try
            {
                var areaMappa =await _db.AreeMappa.SingleAsync(am => am.Id == Id);
                var result = Mapper.Map<AreaMappaDTO>(areaMappa);
                return result;
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nella richiesta per l'area mappa");
            }
        }
        [Route("api/editamappa")]
        [HttpPost]
        public async Task EditMappa(AreaMappaDTO areaMappa)
        {
            await Task.FromResult(0);
            try
            {
                var utente = await _userManager.FindByNameAsync(User.Identity.Name);
                var roles=await _userManager.GetRolesAsync(utente);
                if (!roles.Contains(Ruolo.PA.ToString()) && !roles.Contains(Ruolo.ProtezioneCivile.ToString()))
                {
                    throw new Exception("Sessione scaduta");
                }
                var mappa = _db.AreeMappa.Single(am => am.Id == areaMappa.Id);
                mappa = Mapper.Map(areaMappa, mappa);
            }
            catch(Exception ex)
            {
                ControllerContext.HttpContext.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                throw new Exception("Errore nell'aggiornamento dell'area mappa");
            }
        }

        [Route("api/gps")]
        public async Task PostGPS()
        {
            await Task.FromResult(0);
        }

        [Route("api/aziende/{pageNumber:int?}")]
        [HttpGet]
        public async Task<PagedResult<AziendaDTO>> GetAziende(int? pageNumber)
        {
            await Task.FromResult(0);
            int PageSize = 10;
            
            var skipValue = (pageNumber.GetValueOrDefault(1) - 1) * PageSize;
            var aziende = _db.Aziende.Skip(skipValue).Take(PageSize);

            var aziendeDTO=aziende.Select(az => Mapper.Map<AziendaDTO>(az));
            PagedResult<AziendaDTO> result = new PagedResult<AziendaDTO>();
            result.Risultati = aziendeDTO;
            result.PageNumber = pageNumber.GetValueOrDefault(1);
            return result;
        }
        [Route("api/aziende/{id}")]
        [HttpGet]
        public async Task<AziendaDTO> GetAzienda(int idAzienda)
        {
            var azienda = await _db.Aziende.SingleAsync(az => az.Id == idAzienda);
            return Mapper.Map<Azienda, AziendaDTO>(azienda);
        }
    }
}
