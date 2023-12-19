using System.Collections.Generic;
using TheBlogProject.Models;

namespace TheBlogProject.ViewModels
{
    public class PostDetailViewModel
    {
        public Post Post { get; set; }
        public List<string> Tags { get; set; } = new List<string>();
    }
}
