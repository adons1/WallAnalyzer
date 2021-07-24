using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using VkNet;
using VkNet.Model.Attachments;
using VkNet.Model.RequestParams;

namespace ConsoleApp5
{
    public partial class Program
    {
        static void GetAndAnalyzePhotos(VkApi api, long? id, PostRepostComment type, string[] key_words, List<string> photos)
        {
            var photosExtended = api.Photo.Get(new PhotoGetParams
            {
                OwnerId = id,
                AlbumId = VkNet.Enums.SafetyEnums.PhotoAlbumType.Wall,
                PhotoIds = photos
            });
            int count = 0;
            foreach (var photo in photosExtended)
            {
                using (var client = new WebClient())
                {
                    try { client.DownloadFile(photo.Sizes.Last().Url.AbsoluteUri, "userphotos/" + "count" + Convert.ToString(count) + ".jpg"); }
                    catch{ }
                }
                count++;
            }

            string result = PythonAnalisys();

            if (log)
                Console.WriteLine("\t'" + result + "'");

            if (log)
                Console.ForegroundColor = ConsoleColor.Red;
            switch (type)
            {
                case PostRepostComment.Post:
                    AnalyzeKeyWords(key_words, result, "Анализ фотографий поста:");
                    break;
                case PostRepostComment.Repost:
                    AnalyzeKeyWords(key_words, result, "\t" + "Анализ фотографий репоста: ");
                    break;
            }
            if (log)
                Console.ForegroundColor = ConsoleColor.White;

            count = 0;
            foreach (var photo in photosExtended)
            {
                File.Delete("userphotos/" + "count" + Convert.ToString(count) + ".jpg");
                count++;
            }
        }
        static void GetAndAnalyzeComments(VkApi api, long? id, string[] key_words, Post post)
        {
            var comments = api.Wall.GetComments(new WallGetCommentsParams
            {
                OwnerId = id,
                PostId = post.Id.Value
            });

            if (log)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine("\t" + comments.Items.Count.ToString() + " комментариев(я)");
                Console.ForegroundColor = ConsoleColor.White;
            }

            StringBuilder commentsBuilder = new StringBuilder();
            foreach (var comment in comments.Items)
            {
                if (log)
                    Console.WriteLine("\t\t" + comment.OwnerId.ToString() + "\t" + comment.Text + "\n");
                commentsBuilder.Append(comment.Text + " ");
                if (comment.Thread.Count > 0)
                {
                    var comments1 = api.Wall.GetComments(new WallGetCommentsParams
                    {
                        OwnerId = comment.OwnerId,
                        CommentId = comment.Id
                    });
                    foreach (var comment1 in comments1.Items)
                    {
                        if (log)
                            Console.WriteLine("\t\t\t" + comment1.OwnerId.ToString() + "\t" + comment1.Text + "\n");
                        commentsBuilder.Append(comment1.Text + " ");
                    }
                }
            }

            AnalyzeKeyWords(key_words, commentsBuilder.ToString(), "\tАнализ комментариев поста:");

            commentsBuilder.Clear();
        }
    }
}
