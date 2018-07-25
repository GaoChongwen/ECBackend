﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Description;
using ECBack.Filters;
using ECBack.Models;

namespace ECBack.Controllers
{
    public class CommentsQuery
    {
        public int GoodID { get; set; }
        public int? Pn { get; set; }
    }

    public class CommentQuery
    {
        public int GoodID { get; set; }
        public int UserID { get; set; }
    }
    public class CommentsController : ApiController
    {
        private OracleDbContext db = new OracleDbContext();
        private const int PageDataNumber = 15;
        /// <summary>
        /// 获取特定商品一页评论
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("api/Comments")]
        public HttpResponseMessage GetRelatedComments([FromUri] CommentsQuery data)
        {
            int pn = data.Pn ?? 1;
            IQueryable<Comment> Comments;
            var cate = db.SaleEntities.Find(data.GoodID);
            if (cate == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest, "GoodID not found");
            }
            db.Entry(cate).Collection(c => c.Comments).Load();
            Comments = cate.Comments.AsQueryable();

            var rs = Comments.Skip((pn - 1) * PageDataNumber).Take(PageDataNumber).ToList();
            int res= Comments.Count() / PageDataNumber;
            if (Comments.Count() % PageDataNumber != 0)
                res = res + 1;
            return Request.CreateResponse(HttpStatusCode.OK,
                new
                {
                    ResultNum = res,
                    Comments = rs,
                    PageNum = pn
                });
        }
        /// <summary>
        /// 单个评论
        /// </summary>
        /// <param name=""></param>
        /// <returns></returns>
        [AuthenticationFilter]
        [ResponseType(typeof(Comment))]
        [Route("api/Comments/find")]
        public HttpResponseMessage GetComment([FromUri] int GoodID)
        {
            if (HttpContext.Current.User == null)
            {
                // 无权
                System.Diagnostics.Debug.WriteLine("Get Favorites Null");
                return Request.CreateResponse((HttpStatusCode)403);
            }
            User requestUser = (User)HttpContext.Current.User;
            int user_id = requestUser.UserID;

            IQueryable<Comment> Comments;
            Comments = db.Comments;
            List<Comment> tt = Comments.ToList();

            
            Comment comment = null;
            foreach (var VARIABLE in tt)
            {
                if (VARIABLE.UserID == user_id&&VARIABLE.SaleEntityID==GoodID)
                    comment = VARIABLE;
            }
            if (comment == null)
            {
                return Request.CreateResponse(HttpStatusCode.BadRequest,"comment not found");
            }

            return Request.CreateResponse(HttpStatusCode.OK,comment);
        }

        // PUT: api/Comments/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutComment(int id, Comment comment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != comment.CommentID)
            {
                return BadRequest();
            }

            db.Entry(comment).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CommentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return StatusCode(HttpStatusCode.NoContent);
        }

        // POST: api/Comments
        [HttpPost]
        [Route("api/Comments")]
        public IHttpActionResult PostComment([FromBody] Comment comment)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            IQueryable<User> users = db.Users;
            IQueryable<SaleEntity> sales = db.SaleEntities;
            if (users.Where(d => d.UserID == comment.UserID).Count() == 0)
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "UserID not found"));
            }
            if (sales.Where(d => d.SaleEntityID == comment.SaleEntityID).Count() == 0)
            {
                return ResponseMessage(Request.CreateResponse(HttpStatusCode.BadRequest, "SaleEntityID not found"));
            }
            db.Comments.Add(comment);
            db.SaveChanges();

            return ResponseMessage(Request.CreateResponse(HttpStatusCode.OK,"oka"));
        }

        // DELETE: api/Comments/5
        [ResponseType(typeof(Comment))]
        public IHttpActionResult DeleteComment(int id)
        {
            Comment comment = db.Comments.Find(id);
            if (comment == null)
            {
                return NotFound();
            }

            db.Comments.Remove(comment);
            db.SaveChanges();

            return Ok(comment);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CommentExists(int id)
        {
            return db.Comments.Count(e => e.CommentID == id) > 0;
        }
    }
}