﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace ECBack.Models
{
    public class Question//问题:问题的id，问题的内容，问题对应的商品。。。关于问题的时间啊订单啊我看那个表里面没有就先不加了
    {
        [Key]
        public int QuestionID { get; set; }

        [MaxLength(50)]
        public string Detail { get; set; }

        public ICollection<Reply> Replies { get; set; }

        [JsonIgnore]
        public int GoodEntityID { get; set; }

        [JsonIgnore]
        [ForeignKey("GoodentityID")]
        public GoodEntity GoodEntity { get; set; }




    }

    public class Reply//回复：回复的id，回复的内容，时间以及对应的问题
    {
        [Key]
        public int ReplyID { get; set; }

        [MaxLength(200)]
        public string ReplyDetail { get; set; }

        public DateTime UserReplyTime { get; set; }

        public Reply()
        {
            UserReplyTime = DateTime.Now;
        }

        [JsonIgnore]
        public int QuestionID { get; set; }

        [JsonIgnore]
        [ForeignKey("QuestionID")]
        public Question Question { get; set; }
    }
}