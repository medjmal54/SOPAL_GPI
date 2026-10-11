using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SOPAL_GPI.DTOs;
using SOPAL_GPI.Hubs;
using SOPAL_GPI.Models;
using SOPAL_GPI.Services;
using System.Security.Claims;

namespace SOPAL_GPI.Controllers
{
    [Route("api/v1/mouvements")]
    [ApiController]
    [Authorize]
    public class MouvementsController(
        IMouvementRepository mouvementRepository,
        IMaterialRepository materialRepository,
        IEmployeRepository employeRepository,
        ILocalisationRepository localisationRepository,
        IHubContext<NotificationHub> hubContext) : ControllerBase
    {
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Mouvement>>> GetAll()
        {
            return Ok(await mouvementRepository.GetAllAsync());
        }

        [HttpGet("search")]
        public async Task<ActionResult<MouvementSearchResult>> Search(
            string? q, int? typmov, int? localisationId, int? employeMat, int? typeMouvement, string? department, string? status,
            int page = 1, int pageSize = 12)
        {
            page = Math.Max(page, 1);
            pageSize = Math.Clamp(pageSize, 1, 100);
            return Ok(await mouvementRepository.SearchPaginatedAsync(q, typmov, localisationId, employeMat, typeMouvement, department, page, pageSize));
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(int id)
        {
            var mouvs = await mouvementRepository.GetByIdAsync(id);
            if (mouvs is null) return NotFound();
            return Ok(mouvs);
        }

        [HttpGet("{matid}/details")]
        public async Task<ActionResult<List<Mouvement>>> GetMouvmentsById(int matid)
        {
            var material = await materialRepository.GetByIdAsync(matid);
            if (material is null) return NotFound("MatÃ©riel introuvable!");
            return Ok(await mouvementRepository.GetMouvementsById(matid));
        }

        [HttpPost("{Matid}/affecter/{employeId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Affecter(int Matid, int employeId)
        {
            var material = await materialRepository.GetByIdAsync(Matid);
            var employe = await employeRepository.GetByIdAsync(employeId);
            if (material is null || employe is null) return NotFound("MatÃ©riel ou employÃ© introuvable.");
            if (material.EmployeMat != null) return BadRequest("DÃ©solÃ©, ce matÃ©riel est dÃ©jÃ  affectÃ©.");

            await mouvementRepository.AffecterEmp(material, employeId);
            return Ok(new { message = "Affectation validÃ©e" });
        }

        [HttpPost("{Matid}/transfert/{localid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Transfert(int Matid, int localid)
        {
            var material = await materialRepository.GetByIdAsync(Matid);
            var localisation = await localisationRepository.GetByIdAsync(localid);
            if (material is null || localisation is null) return NotFound("MatÃ©riel ou localisation introuvable.");

            await mouvementRepository.Transfert(material, localid);
            return Ok(new { message = "Transfert validÃ©" });
        }

        [HttpPost("{Matid}/retour")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Retour(int Matid)
        {
            var material = await materialRepository.GetByIdAsync(Matid);
            if (material is null) return NotFound("MatÃ©riel introuvable.");
            if (material.EmployeMat is null) return BadRequest("Le matÃ©riel n'est pas affectÃ©.");

            await mouvementRepository.Retour(material);
            return Ok(new { message = "Retour validÃ©" });
        }

        [HttpPost("Requests")]
        public async Task<ActionResult> AddRequest(string DescPanne, int Matid, int matricule)
        {
            var material = await materialRepository.GetByIdAsync(Matid);
            if (material is null) return NotFound("MatÃ©riel introuvable.");
            var employe = await employeRepository.GetByIdAsync(matricule);
            if (employe is null) return NotFound("EmployÃ© introuvable.");

            var mouvement = await mouvementRepository.AddRequestAsync(matricule, material, DescPanne);

            await hubContext.Clients.Group("Admins").SendAsync("ReceiveNewRequest", mouvement);

            return Ok("Demande d'intervention ajoutÃ©!");
        }

        [HttpGet("Requests")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<Mouvement>>> GetAllRequestsAsync(string? q)
        {
            return Ok(await mouvementRepository.GetIntrvRequests(q));
        }

        [HttpGet("Requests/count")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetPendingRequestCount()
        {
            var count = await mouvementRepository.GetPendingRequestCountAsync();
            return Ok(new { count });
        }

        [HttpGet("Requests/{matricule}")]
        public async Task<ActionResult<List<Mouvement>>> GetAllRequestsByUser(int matricule, string? q)
        {
            var checkemp = await employeRepository.GetByIdAsync(matricule);
            if (checkemp is null) return NotFound(new { message = "EmployÃ© invalide!" });
            return Ok(await mouvementRepository.GetRequetsByUserAsync(matricule, q));
        }

        [HttpPut("{id}/resolve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Resolve(int id)
        {
            var mouvement = await mouvementRepository.GetByIdAsync(id);
            if (mouvement is null) return NotFound("Demande introuvable.");
            if (mouvement.Typmov != 4) return BadRequest("Ce mouvement n'est pas une demande d'intervention.");

            await mouvementRepository.ResolveAsync(id);
            return Ok(new { message = "Demande marquÃ©e comme terminÃ©." });
        }

        [HttpPut("{id}/toggle-resolve")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleResolve(int id)
        {
            var mouvement = await mouvementRepository.GetByIdAsync(id);
            if (mouvement is null) return NotFound("Demande introuvable.");
            if (mouvement.Typmov != 4) return BadRequest("Ce mouvement n'est pas une demande d'intervention.");

            await mouvementRepository.ToggleResolveAsync(id);
            var updated = await mouvementRepository.GetByIdAsync(id);
            return Ok(new { message = "Statut mis Ã  jour.", statut = updated?.Statut });
        }

        [HttpPut("{id}/start-intervention")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> StartIntervention(int id)
        {
            var mouvement = await mouvementRepository.GetByIdAsync(id);
            if (mouvement is null) return NotFound("Demande introuvable.");
            if (mouvement.Typmov != 4) return BadRequest("Ce mouvement n'est pas une demande d'intervention.");
            if (mouvement.Statut != "Pas encore intervenu") return BadRequest("Cette demande est dÃ©jÃ  prise en charge.");

            var matricule = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            await mouvementRepository.StartInterventionAsync(id, matricule);
            return Ok(new { message = "Intervention dÃ©marrÃ©e.", statut = "En cours" });
        }

        [HttpGet("my-interventions")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetMyInterventions(string? q, bool? done)
        {
            var matricule = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);
            return Ok(await mouvementRepository.GetMyInterventionsAsync(matricule, q, done));
        }
    }
}



