﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StoresManagement.Data;
using StoresManagement.Models;
using StoresManagement.ViewModels;

namespace StoresManagement.Controllers
{
    [Authorize(Roles = "Manager,Administrator,Seller")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly int _entityId;

        public CustomersController(ApplicationDbContext context, IMapper mapper, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _mapper = mapper;
            _userManager = userManager;
            _signInManager = signInManager;
            _httpContextAccessor = httpContextAccessor;
            _entityId = GetEntityId();
        }

        private int GetEntityId()
        {
            var entityUsers = _context.EntityUsers
        .SingleOrDefault(m => m.UserId == _userManager.GetUserId(_httpContextAccessor.HttpContext.User));

            if (entityUsers == null)
                return 0;
            else
                return entityUsers.EntityId;
        }

        // GET: Customers/Search
        [HttpGet]
        public ActionResult Search(string term)
        {
            var customerList = _context.Customers
                .Where(r => r.EntityId == _entityId
            && (r.Name.Contains(term) || r.Surname.Contains(term)))
                              .Select(r => new
                              {
                                  id = r.Id,
                                  label = r.FullName
                              }).ToArray();

            return Json(customerList);
        }

        // GET: Customers
        public async Task<IActionResult> Index()
        {
            var customers = await _context.Customers
                .Where(m => m.EntityId == _entityId)
                .Include(b => b.Contact)
                .Include(b => b.Entity)
                .ToListAsync();

            return View(_mapper.Map<IEnumerable<CustomerFormViewModel>>(customers));
        }

        // GET: Customers/ListCustomers/5
        public async Task<IActionResult> ListCustomers(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customers = await _context.Customers
                .Where(m => m.EntityId == id)
                .Include(b => b.Entity)
                .Include(b => b.Contact)
                .ToListAsync();

            return View(_mapper.Map<IEnumerable<CustomerFormViewModel>>(customers));
        }

        // GET: Customers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(b => b.Contact)
                .Include(b => b.Entity)
                .SingleOrDefaultAsync(m => m.EntityId == _entityId && m.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            return View(_mapper.Map<CustomerFormViewModel>(customer));
        }

        // GET: Customers/Create
        public IActionResult Create()
        {
            var customerVM = new CustomerFormViewModel
            {
                Entities = _context.Entities
                .Where(m => m.Id == _entityId)
                .ToList()
            };

            return View(customerVM);
        }

        // POST: Customers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CustomerFormViewModel customerVM)
        {
            if (ModelState.IsValid)
            {
                var customer = _mapper.Map<Customer>(customerVM);

                _context.Add(customer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            customerVM.Entities = _context.Entities
                .Where(m => m.Id == _entityId)
                .ToList();

            return View(customerVM);
        }

        // GET: Customers/Edit/5
        [Authorize(Roles = "Manager,Administrator")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(b => b.Contact)
                .Include(b => b.Entity)
                .SingleOrDefaultAsync(m => m.EntityId == _entityId && m.Id == id);

            if (customer == null)
            {
                return NotFound();
            }

            var customerVM = _mapper.Map<CustomerFormViewModel>(customer);

            customerVM.Entities = _context.Entities
                .Where(m => m.Id == _entityId)
                .ToList();

            return View(customerVM);
        }

        // POST: Customers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager,Administrator")]
        public async Task<IActionResult> Edit(int id, CustomerFormViewModel customerVM)
        {
            if (id != customerVM.Id)
            {
                return BadRequest();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var customer = _mapper.Map<Customer>(customerVM);

                    _context.Update(customer);
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await BranchExists(customerVM.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            customerVM.Entities = _context.Entities
                .Where(m => m.Id == _entityId)
                .ToList();

            return View(customerVM);
        }

        // GET: Customers/Delete/5
        [Authorize(Roles = "Manager,Administrator")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var customer = await _context.Customers
                .Include(b => b.Contact)
                .Include(b => b.Entity)
                .SingleOrDefaultAsync(m => m.Id == _entityId && m.Id == id);
            if (customer == null)
            {
                return NotFound();
            }

            return View(_mapper.Map<CustomerFormViewModel>(customer));
        }

        // POST: Customers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager,Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var customer = await _context.Customers
                .FindAsync(id);

            _context.Customers.Remove(customer);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> BranchExists(int id)
        {
            return await _context.Customers.AnyAsync(e => e.Id == id);
        }
    }
}