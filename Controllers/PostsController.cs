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
using TheBlogProject.Services;
using TheBlogProject.Enums;
using X.PagedList;
using TheBlogProject.ViewModels;
using Microsoft.Extensions.Hosting;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Authorization;

namespace TheBlogProject.Controllers
{
    public class PostsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ISlugService _slugService;
        private readonly IImageService _imageService;
        private readonly UserManager<BlogUser> _userManager;
        private readonly BlogSearchService _blogSearchService;

        public PostsController(ApplicationDbContext context, ISlugService slugService, IImageService imageService, UserManager<BlogUser> userManager, BlogSearchService blogSearchService)
        {
            _context = context;
            _slugService = slugService;
            _imageService = imageService;
            _userManager = userManager;
            _blogSearchService = blogSearchService;
        }


        public async Task<IActionResult> SearchIndex(int? page, string searchTerm)
        {
            ViewData["SearchTerm"] = searchTerm;
            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";
            var pageNumber = page ?? 1;
            var pageSize = 6;

            var posts = _blogSearchService.Search(searchTerm);

            return View(await posts.ToPagedListAsync(pageNumber,pageSize));

        }


        // GET: Posts
        public async Task<IActionResult> Index()
        {
            var applicationDbContext = _context.Posts.Include(p => p.Blog).Include(p => p.BlogUser);
            return View(await applicationDbContext.ToListAsync());
        }

        //BlogPostIndex
        public async Task<IActionResult> BlogPostIndex (int? id, int? page) 
        {
            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";
            var blog = await _context.Blogs.FindAsync(id);
            ViewData["blogName"] = blog.Name;
            if (id is null) 
            {
                return NotFound();
            }

            var pageNumber = page ?? 1;
            var pageSize = 6;

            //var posts = _context.Posts.Where(p=> p.BlogId == id).ToList();

            var posts = await _context.Posts
                .Where(p => p.BlogId == id && p.ReadyStatus == ReadyStatus.ProductionReady)
                .OrderByDescending(p=> p.Created)
                .ToPagedListAsync(pageNumber, pageSize);

            return View(posts);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> PostProductionIndex( int? page)
        {
            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";
            var pageNumber = page ?? 1;
            var pageSize = 5;

            var blogs = _context.Blogs.Include(b => b.BlogUser)
                .Where(b=> !b.Posts.Any() ||  b.Posts.Any(p => p.ReadyStatus != Enums.ReadyStatus.ProductionReady))
                .OrderByDescending(b => b.Created)
                .ToPagedListAsync(pageNumber, pageSize);


            return View(await blogs);
        }

        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> PostPreviewIndex(int? id, int? page)
        {
            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";
            var blog = await _context.Blogs.FindAsync(id);
            ViewData["blogName"] = blog.Name;
            ViewData["BlogId"] = blog.Id;
            if (id is null)
            {
                return NotFound();
            }

            var pageNumber = page ?? 1;
            var pageSize = 6;

            //var posts = _context.Posts.Where(p=> p.BlogId == id).ToList();

            var posts = await _context.Posts
                .Where(p => p.BlogId == id && p.ReadyStatus != ReadyStatus.ProductionReady)
                .OrderByDescending(p => p.Created)
                .ToPagedListAsync(pageNumber, pageSize);

            return View(posts);
        }

        //Tag Index
        public async Task<IActionResult> TagIndex(string tag, int? page)
        {
            if (tag is null)
            {
                return NotFound();
            }

            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";

            var pageNumber = page ?? 1;  // null coelescing operator
            var pageSize = 5;  // amount per page

            var tagPosts = await _context.Tags
                .Where(p => p.Text == tag)
                .Include(p => p.Post)
                .Include(u => u.BlogUser)
                .OrderByDescending(p => p.Post.Created)
                .ToPagedListAsync(pageNumber, pageSize);

            ViewData["TagName"] = tag;




            TempData["CurrentPage"] = page;

            return View(tagPosts);

        }

        // GET: Posts/Details/5
        public async Task<IActionResult> Details(string slug)
        {

            ViewData["Title"] = "Post Details Page";
            if (string.IsNullOrEmpty(slug))
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Blog)
                .Include(p => p.BlogUser)
                .Include(p=> p.Tags)
                .Include(p => p.Comments)
                .ThenInclude(c=> c.BlogUser)
                .Include(p => p.Comments)
                .ThenInclude(c => c.Moderator)
                .FirstOrDefaultAsync(m => m.Slug == slug);
            if (post == null)
            {
                return NotFound();
            }

            @ViewData["HeaderImage"] = _imageService.DecodeImage(post.ImageDate, post.ContentType);
            @ViewData["MainText"] = post.Title;
            @ViewData["SubText"] = post.Abstract;

            return View(post);
        }

        // GET: Posts/Create
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create(int? id)
        {
            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Name");
            ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id");
            var blog = await _context.Blogs.FindAsync(id);
            if(blog is not null)
            {
                ViewData["BlogName"] = blog.Name;
                ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Name",blog.Id);
            }
            else
            {
                ViewData["BlogName"] = null;
            }
 

            return View();
        }

        // POST: Posts/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Create([Bind("BlogId,Title,Abstract,Content,ReadyStatus,Image")] Post post, List<string> tagValues)
        {
            if (ModelState.IsValid)
            {
                post.Created = DateTime.UtcNow;
                var authorId = _userManager.GetUserId(User);
                post.BlogUserId=authorId;


                //Use the _imageService to stor the incoming user specified image
                post.ImageDate = await _imageService.EncodeImageAsync(post.Image);
                post.ContentType = _imageService.ContentType(post.Image);

                //Create the slug and determine if it is unique
                var slug = _slugService.UrlFriendly(post.Title);

                //create a variable to store whether an error has occured
                var validationError = false;

                if (string.IsNullOrEmpty(slug)) 
                {
                    validationError = true;
                    ModelState.AddModelError("", "The Title you provided cannot be used as it results in an empty slug.");
                }

                if(!_slugService.IsUnique(slug))
                {
                    validationError = true;
                    ModelState.AddModelError("Title", "The Title you provided cannot be used as it results in a duplicate slug.");
                }

                if (validationError) 
                {
                    ViewData["TagValues"] = string.Join(",", tagValues);
                    return View(post);
                }

                post.Slug = slug;


                _context.Add(post);
                await _context.SaveChangesAsync();

                foreach(var tagText in tagValues)
                {
                    _context.Add(new Tag()
                    {
                        PostId = post.Id,
                        BlogUserId = authorId,
                        Text = tagText
                    });
                }

                await _context.SaveChangesAsync();

                return RedirectToAction("BlogPostIndex", "Posts", new { id = post.BlogId });
            }
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Description", post.BlogId);


            return View(post);
        }

        // GET: Posts/Edit/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(string slug)
        {
            @ViewData["HeaderImage"] = "/images/home-bg.jpg";
            @ViewData["MainText"] = "Wesley's Blog Project";
            @ViewData["SubText"] = "Slowly Getting Better at Coding!";
            if (slug == null)
            {
                return NotFound();
            }

            var post = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Slug == slug); 
            if (post == null)
            {
                return NotFound();
            }
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Name", post.BlogId);
            ViewData["TagValues"] = string.Join(",", post.Tags.Select(t => t.Text));
            ViewData["oldBlog"] = post.BlogId;

            return View(post);
        }

        // POST: Posts/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,BlogId,Title,Abstract,Content,ReadyStatus")] Post post, IFormFile? newImage, List<string> tagValues)
        {
            if (id != post.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {

                try
                {
                    //get the old post
                    var newPost = await _context.Posts.Include(p => p.Tags).FirstOrDefaultAsync(p => p.Id == post.Id);

                    newPost.BlogId = post.BlogId;
                    newPost.Updated = DateTime.UtcNow;
                    newPost.Title = post.Title;
                    newPost.Abstract = post.Abstract;
                    newPost.Content = post.Content;
                    newPost.ReadyStatus = post.ReadyStatus;



                    var newSlug = _slugService.UrlFriendly(post.Title);
                    if(newSlug != newPost.Slug)
                    {
                        if (_slugService.IsUnique(newSlug)) 
                        {
                            newPost.Title = post.Title;
                            newPost.Slug = newSlug;
                        }
                        else 
                        {
                            ModelState.AddModelError("Title", "The Title you provided cannot be used as it results in a duplicate slug.");
                            ViewData["TagValues"] = string.Join(",", tagValues);
                            return View(post);
                        }
                    }


                    if (newImage is not null) 
                    {
                        newPost.ImageDate = await _imageService.EncodeImageAsync(newImage);
                        newPost.ContentType = _imageService.ContentType(newImage);
                    }

                    //Remove all Tags previously associated with this post
                    _context.Tags.RemoveRange(newPost.Tags);

                    //Add in the new Tags from the edit form
                    foreach(var tagText in tagValues) 
                    {
                        _context.Add(new Tag()
                        {
                            PostId = post.Id,
                            BlogUserId = newPost.BlogUserId,
                            Text = tagText
                        });
                    }

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!PostExists(post.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }

                return RedirectToAction("BlogPostIndex", "Posts", new { id = post.BlogId });
            }
            ViewData["BlogId"] = new SelectList(_context.Blogs, "Id", "Description", post.BlogId);
            ViewData["BlogUserId"] = new SelectList(_context.Users, "Id", "Id", post.BlogUserId);
            return View(post);
        }

        // GET: Posts/Delete/5
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> Delete(string slug)
        {
            if (slug == null)
            {
                return NotFound();
            }

            var post = await _context.Posts
                .Include(p => p.Blog)
                .Include(p => p.BlogUser)
                .FirstOrDefaultAsync(m => m.Slug == slug);
            if (post == null)
            {
                return NotFound();
            }

            return View(post);
        }

        // POST: Posts/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Posts == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Posts'  is null.");
            }
            var post = await _context.Posts.FindAsync(id);
            if (post != null)
            {
                var comments = await _context.Comments.Where(c => c.PostId == id).ToListAsync();
                foreach (var comment in comments)
                {
                    _context.Comments.Remove(comment);
                }
                _context.Posts.Remove(post);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction("BlogPostIndex", "Posts", new { id = post.BlogId });
        }

        private bool PostExists(int id)
        {
          return (_context.Posts?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
