﻿using System;
using System.Linq;
using PaderbornUniversity.SILab.Hip.CmsApi.Data;
using PaderbornUniversity.SILab.Hip.CmsApi.Models.Entity;
using Microsoft.EntityFrameworkCore;

namespace PaderbornUniversity.SILab.Hip.CmsApi.Managers
{
    public class BaseManager
    {
        protected readonly CmsDbContext DbContext;

        protected BaseManager(CmsDbContext dbContext)
        {
            DbContext = dbContext;
        }


        protected static void DeleteFile(string path)
        {
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }


        // Could be needed at every controller!
        /// <exception cref="InvalidOperationException">The input sequence contains more than one element. -or- The input sequence is empty.</exception>
        public User GetUserByEmail(string email)
        {
            return DbContext.Users.Include(u => u.StudentDetails).Single(u => u.Email == email);
        }

        public User GetUserByIdentity(string identity)
        {
            return DbContext.Users.Include(u => u.StudentDetails).Single(u => u.UId == identity);
        }

        public int GetIdByIdentity(string identity)
        {
            return DbContext.Users.Single(u => u.UId == identity).Id;
        }
    }
}
