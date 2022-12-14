using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Projekt_zaliczeniowy.Models;

namespace Projekt_zaliczeniowy.Controllers
{
    public class TicketController : Controller
    {
        private readonly AppDbContext _context;
        IDictionary<string, int> price = new Dictionary<string, int>() { 
            {"Normal", 80},
            {"Student", 60},
            {"Senior", 50},
            {"Child", 20},
        };

    public TicketController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Ticket
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Index()
        {
            var currentUser = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var appDbContext = _context.Tickets.Where(t=>t.UserId==currentUser).Include(t => t.Match);
            return View(await appDbContext.ToListAsync());
        }

        // GET: Ticket/Details/5
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Match)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // GET: Ticket/Create
        [Authorize(Roles = "Admin,User")]
        public IActionResult Create(int? id)
        {
            Match match = _context.Matches.Find(id);
            Ticket ticket = new Ticket();
            ticket.MatchId = match.Id;
            ticket.totalPrice = match.Price;
            ticket.Status = "In process";
            return View(ticket);
        }

        // POST: Ticket/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("howManyPeople,Seats,totalPrice,Status,MatchId")] Ticket ticket)
        {
            if (ModelState.IsValid)
            {
                ticket.Status = "Completed";
                ticket.totalPrice *= ticket.howManyPeople;
                ticket.UserId= User.FindFirstValue(ClaimTypes.NameIdentifier);
                SubtractTicket(ticket.MatchId);
                _context.Add(ticket);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(ticket);
        }

        // GET: Ticket/Edit/5
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            return View(ticket);
        }

        // POST: Ticket/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [Authorize(Roles = "Admin,User")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,howManyPeople,Seats,totalPrice,Status,MatchId,UserId")] Ticket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var _match = _context.Matches.Find(ticket.MatchId);
                    ticket.totalPrice = ticket.howManyPeople*_match.Price;
                    ticket.Status = "Edited";
                    _context.Update(ticket);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TicketExists(ticket.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MatchId"] = new SelectList(_context.Matches, "Id", "Id", ticket.MatchId);
            return View(ticket);
        }

        // GET: Ticket/Delete/5
        [Authorize(Roles = "Admin,User")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Tickets == null)
            {
                return NotFound();
            }

            var ticket = await _context.Tickets
                .Include(t => t.Match)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (ticket == null)
            {
                return NotFound();
            }

            return View(ticket);
        }

        // POST: Ticket/Delete/5
        [Authorize(Roles = "Admin,User")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Tickets == null)
            {
                return Problem("Entity set 'AppDbContext.Tickets'  is null.");
            }
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket != null)
            {
                _context.Tickets.Remove(ticket);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TicketExists(int id)
        {
          return _context.Tickets.Any(e => e.Id == id);
        }

        private void SubtractTicket(int matchId)
        {
            var match = _context.Matches.Find(matchId);
            match.Tickets_amount -= 1;
        }
    }
}
