using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TheBlogProject.Data;
using TheBlogProject.Models;

namespace TheBlogProject.Controllers
{
    public class CommentsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<BlogUser> _userManager;
        public CommentsController(ApplicationDbContext context, UserManager<BlogUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        //GET: Comments

        public async Task<IActionResult> OriginalIndex()
        {
            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";
            var originalComments = await _context.Comments.Where(c => c.Moderated == null).Where(c => c.Deleted == null).Include(c => c.BlogUser).Include(c => c.Moderator).Include(c => c.Post).ToListAsync();
            return View("Index", originalComments);
        }
        public async Task<IActionResult> ModeratedIndex()
        {
            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";
            var moderatedComments = await _context.Comments.Where(c => c.Moderated != null).Include(c => c.BlogUser).Include(c => c.Moderator).Include(c => c.Post).ToListAsync();
            return View("Index", moderatedComments);
        }
        public async Task<IActionResult> DeletedIndex()
        {
            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";
            var deletedComments = await _context.Comments.Where(c => c.Deleted != null).Include(c => c.BlogUser).Include(c => c.Post).ToListAsync();
            return View("Index", deletedComments);
        }

        // GET: Comments/Details/5


        // GET: Comments/Create
        //public IActionResult Create()
        //{
        //    ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id");
        //    ViewData["ModeratorId"] = new SelectList(_context.Users, "Id", "Id");
        //    ViewData["PostId"] = new SelectList(_context.Posts, "Id", "Abstract");
        //    return View();
        //}

        // POST: Comments/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PostId,Body")] Comment comment)
        {
            if (ModelState.IsValid)
            {
                


                comment.BlogUserId = _userManager.GetUserId(User);
                comment.Created = DateTime.UtcNow;

                _context.Add(comment);
                await _context.SaveChangesAsync();
                var commentList = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.PostId == comment.PostId);
                return RedirectToAction("Details", "Posts", new { slug = commentList.Post.Slug }, "commentSection");
            }


            return View(comment);
        }

        // GET: Comments/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Comments == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments.FindAsync(id);
            if (comment == null)
            {
                return NotFound();
            }
            ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id", comment.BlogUserId);
            ViewData["ModeratorId"] = new SelectList(_context.Users, "Id", "Id", comment.ModeratorId);
            ViewData["PostId"] = new SelectList(_context.Posts, "Id", "Abstract", comment.PostId);
            return View(comment);
        }

        // POST: Comments/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Body")] Comment comment)
        {
            if (id != comment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var newComment = await _context.Comments.Include(c=> c.Post).FirstOrDefaultAsync(c => c.Id == comment.Id);
                try
                {
                    newComment.Body = comment.Body;
                    newComment.Updated = DateTime.UtcNow;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Details", "Posts", new {slug = newComment.Post.Slug}, "commentSection");
            }
            ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id", comment.BlogUserId);
            ViewData["ModeratorId"] = new SelectList(_context.Users, "Id", "Id", comment.ModeratorId);
            ViewData["PostId"] = new SelectList(_context.Posts, "Id", "Abstract", comment.PostId);
            return View(comment);
        }


        //Moderate
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Moderate(int id, [Bind("Id,Body,ModeratedBody,ModerationType")] Comment comment)
        {
            if (id != comment.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var newComment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == comment.Id);

                try
                {
                    newComment.ModeratedBody = comment.ModeratedBody;
                    newComment.ModerationType = comment.ModerationType;

                    newComment.Moderated = DateTime.UtcNow;
                    newComment.ModeratorId = _userManager.GetUserId(User);

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CommentExists(comment.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("Details", "Posts", new { slug = newComment.Post.Slug }, "commentSection");
            }

            return View(comment);
        }


        public async Task<IActionResult> SoftDelete(int id)
        {
            if (id == null || _context.Comments == null)
            {
                return NotFound();
            }
            var newComment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == id);
            newComment.Deleted= DateTime.UtcNow;
            newComment.Moderated = null;
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Posts", new { slug = newComment.Post.Slug }, "commentSection");

        }

        public async Task<IActionResult> SoftDeleteMenu(int id)
        {
            if (id == null || _context.Comments == null)
            {
                return NotFound();
            }
            var newComment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == id);
            newComment.Deleted = DateTime.UtcNow;
            newComment.Moderated = null;
            await _context.SaveChangesAsync();

            return RedirectToAction("DeletedIndex", "Comments");

        }

        public async Task<IActionResult> UndoDelete(int id)
        {
            if (id == null || _context.Comments == null)
            {
                return NotFound();
            }
            var newComment = await _context.Comments.Include(c => c.Post).FirstOrDefaultAsync(c => c.Id == id);
            newComment.Deleted = null;
            if(newComment.ModeratedBody != null)
            {
                newComment.Moderated= DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return RedirectToAction("ModeratedIndex", "Comments");
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("OriginalIndex", "Comments");

        }



        // GET: Comments/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Comments == null)
            {
                return NotFound();
            }

            var comment = await _context.Comments
                .Include(c => c.BlogUser)
                .Include(c => c.Moderator)
                .Include(c => c.Post)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (comment == null)
            {
                return NotFound();
            }

            return View(comment);
        }

        // POST: Comments/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, string slug)
        {
            var comment = await _context.Comments.FindAsync(id);
            _context.Comments.Remove(comment);
            
            await _context.SaveChangesAsync();
            return RedirectToAction("DeletedIndex", "Comments");
        }

        private bool CommentExists(int id)
        {
          return (_context.Comments?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
