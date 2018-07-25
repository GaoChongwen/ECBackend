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
using ECBack.Models;
using ECBack.Filters;
using Newtonsoft.Json.Linq;
namespace ECBack.Controllers
{
    public class CouponsController : ApiController
    {
        private OracleDbContext db = new OracleDbContext();

        // GET: api/Coupons
        public IQueryable<Coupons> GetCoupons()
        {
            return db.Coupons;
        }

        [HttpGet]
        [Route("api/GoodEntities/{GoodEntityID:int}/Coupons")]
        public IHttpActionResult GetRelatedCoupons(int GoodEntityID)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            //IQueryable<Coupons> coupons;

            IList<ICollection<Coupons>> coupons = new List<ICollection<Coupons>>();

            var good = db.GoodEntities.Find(GoodEntityID);
            var list = good.Categories;
            foreach (var c in list) {
                IList<Coupons> coupon = new List<Coupons>();
                coupons.Add(c.Coupons);
            }
            return Ok(coupons);
        }

        [AuthenticationFilter]
        [HttpPost]
        [Route("api/Coupons")]
        public HttpResponseMessage PostCoupons([FromUri] int CouponsID)
        {
            if (HttpContext.Current.User == null)
            {
                // 无权
                System.Diagnostics.Debug.WriteLine("Not Login");
                return Request.CreateResponse((HttpStatusCode)403);
            }
            User requestUser = (User)HttpContext.Current.User;
            int user_id = requestUser.UserID;
            int coupon_id = CouponsID;

            HttpResponseMessage response;

            User user = db.Users.Find(user_id);
            Coupons coupon = db.Coupons.Find(coupon_id);
            
            if (user == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "the user not exists");
            }
            else if (coupon == null)
            {
                return Request.CreateResponse(HttpStatusCode.NotFound, "the Coupon not exists");
            }
            else
            {
                user.Coupons.Add(coupon);   
            }
            db.SaveChanges();
            return Request.CreateResponse(HttpStatusCode.OK, "Created");
        }
      

        [AuthenticationFilter]
        [HttpGet]
        [Route("api/Orderforms/{OrderformID:int}/Coupons")]
        public IHttpActionResult GetDiscount(int OrderformID)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (HttpContext.Current.User == null)
            {
                // 无权
                System.Diagnostics.Debug.WriteLine("Not Login");
                return BadRequest();
            }

            User requestUser = (User)HttpContext.Current.User;
            int user_id = requestUser.UserID;

            var user = db.Users.Find(user_id);
            var orderform = db.Orderforms.Find(OrderformID);

            if (orderform==null||user==null)
            {
                return NotFound();
            }

            if (orderform.UserID != user_id)
            {
                // 无权
                System.Diagnostics.Debug.WriteLine("orderform与user不匹配");
                return BadRequest();
            }

            if (user.Coupons.Count == 0)
            {
                return Ok(0);
            }

            // 初始化优惠券的所有类别的ID，无重复
            List<int> coupon_categories_ID = new List<int>();
            foreach (var c in user.Coupons)
            {
                if (getIndex(c.CategoryID, coupon_categories_ID) == -1)
                {
                    coupon_categories_ID.Add(c.CategoryID);
                }
                
            }

            // 初始化每一类的总价List与可降价List，均为0
            List<decimal> prices_by_category = new List<decimal>();
            List<decimal> discount_by_category = new List<decimal>();
            List<int> indexes_coupon = new List<int>();
            List<Coupons> max_coupons = new List<Coupons>(); 
            for (int i = 0; i < coupon_categories_ID.Count; i++)
            {
                prices_by_category.Add(0);
                discount_by_category.Add(0);
                indexes_coupon.Add(0);
            }

            // 得到所有类别的总价
            var SErecord = orderform.SERecords;

            if (SErecord ==null )
            {
                // 无权
                System.Diagnostics.Debug.WriteLine("No SERecord");
                return BadRequest();
            }

            foreach (var record in SErecord)
            {
                //得到该商品的所有类别
                GoodEntity good = db.GoodEntities.Find(record.SaleEntity.GoodEntityID);
                var good_categories = good.Categories; 
                foreach( var cate in good_categories)
                {
                    //更新总价List
                    int index = getIndex(cate.CategoryID, coupon_categories_ID);
                    if (index != -1)
                    {
                        prices_by_category[index]+= record.SaleEntity.Price;
                    }
                }
            }

            // 得到所有类别的最高降价
            for (int i = 0; i < coupon_categories_ID.Count; i++)
            {
                var total_price = prices_by_category[i];
                var cate_id = coupon_categories_ID[i];
                Coupons max_coupon = user.Coupons.First();
                discount_by_category[i] = get_max_discount_by_category(total_price, cate_id, user.Coupons,ref max_coupon);
                max_coupons.Add(max_coupon);
            }

            Coupons valid_coupon = max_coupons[0];
            var max_discount = get_max_discount(discount_by_category, max_coupons, ref valid_coupon);
            user.Coupons.Remove(valid_coupon);
            db.SaveChanges();
            return Ok(max_discount);
        }

        int getIndex(int value, List<int> cate_IDs)
        {
            for( int i = 0;i<cate_IDs.Count;i++)
            {
                if (value == cate_IDs[i])
                {
                    return i;
                }
            }
            return -1;
        }

        decimal get_max_discount_by_category(decimal total_price,int cate_ID,ICollection<Coupons> coupons,ref Coupons max_coupon)
        {
            decimal max = 0;
            foreach (var coupon in coupons)
            {
                if (coupon.CategoryID == cate_ID && total_price > coupon.Min && coupon.Decrease>max)
                {
                    max = coupon.Decrease;
                    max_coupon = coupon;
                }
    
            }
            return max;
        }

        decimal get_max_discount(List<decimal> discount_by_category, List<Coupons> max_coupons, ref Coupons valid_coupon)
        {
            decimal max = discount_by_category[0];
            for (int i = 0; i < discount_by_category.Count; i++)
            {
                if (max < discount_by_category[i])
                {
                    valid_coupon = max_coupons[i];
                    max = discount_by_category[i]; 
                }
            }
            return max;
        }

        // PUT: api/Coupons/5
        [ResponseType(typeof(void))]
        public IHttpActionResult PutCoupons(int id, Coupons coupons)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != coupons.CouponID)
            {
                return BadRequest();
            }

            db.Entry(coupons).State = EntityState.Modified;

            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!CouponsExists(id))
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

        // POST: api/Coupons
        [ResponseType(typeof(Coupons))]
        public IHttpActionResult PostCoupons(Coupons coupons)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            db.Coupons.Add(coupons);
            db.SaveChanges();

            return CreatedAtRoute("DefaultApi", new { id = coupons.CouponID }, coupons);
        }

        // DELETE: api/Coupons/5
        [ResponseType(typeof(Coupons))]
        public IHttpActionResult DeleteCoupons(int id)
        {
            Coupons coupons = db.Coupons.Find(id);
            if (coupons == null)
            {
                return NotFound();
            }

            db.Coupons.Remove(coupons);
            db.SaveChanges();

            return Ok(coupons);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }

        private bool CouponsExists(int id)
        {
            return db.Coupons.Count(e => e.CouponID == id) > 0;
        }
    }
}