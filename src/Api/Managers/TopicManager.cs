using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System;
using Api.Models.Entity;
using Api.Models.User;
using Api.Models.Topic;
using Api.Utility;

namespace Api.Managers
{
    public class TopicManager : BaseManager
    {
        public TopicManager(CmsDbContext dbContext) : base(dbContext) { }

        public virtual PagedResult<TopicResult> GetAllTopics(string queryString, string status, DateTime? deadline, bool onlyParents, int page)
        {
            IQueryable<Topic> query = dbContext.Topics.Include(t => t.CreatedBy);
            if (!string.IsNullOrEmpty(queryString))
                query = query.Where(t => t.Title.Contains(queryString) || t.Description.Contains(queryString));

            if (!string.IsNullOrEmpty(status))
                query = query.Where(t => t.Status.Equals(status));

            if (deadline != null && deadline.HasValue)
                query = query.Where(t => DateTime.Compare(t.Deadline, deadline.Value) == 0);

            // only parents without parent.
            if (onlyParents)
            {
                var topicsWithParent = dbContext.AssociatedTopics.Select(at => at.ChildTopicId).ToList();
                query = query.Where(t => !topicsWithParent.Contains(t.Id));
            }

            int count = query.Count();
            var topics = query.Skip((page - 1) * Constants.PageSize).ToList().Select(t => new TopicResult(t));

            return new PagedResult<TopicResult>(topics, page, count);
        }

        public virtual PagedResult<TopicResult> GetTopicsForUser(int userId, int page)
        {
            var relatedTopicIds = dbContext.TopicUsers.Where(ut => ut.UserId == userId).ToList().Select(ut => ut.TopicId);

            var query = dbContext.Topics.Include(t => t.CreatedBy)
                 .Where(t => t.CreatedById == userId || relatedTopicIds.Contains(t.Id))
                 .Skip((page - 1) * Constants.PageSize).Take(Constants.PageSize).ToList();

            int count = query.Count();
            var topics = query.Skip((page - 1) * Constants.PageSize).Select(t => new TopicResult(t));

            return new PagedResult<TopicResult>(topics, page, count);
        }

        public virtual int GetTopicsCount()
        {
            return dbContext.Topics.Count();
        }

        /// <exception cref="InvalidOperationException">The input sequence contains more than one element. -or- The input sequence is empty.</exception>
        public virtual Topic GetTopicById(int topicId)
        {
            return dbContext.Topics.Include(t => t.CreatedBy).Single(t => t.Id == topicId);
        }

        public virtual IEnumerable<UserResult> GetAssociatedUsersByRole(int topicId, string role)
        {
            return dbContext.TopicUsers.Where(tu => (tu.Role.Equals(role) && tu.TopicId == topicId)).Include(tu => tu.User).ToList().Select(u => new UserResult(u.User));
        }

        public virtual bool ChangeAssociatedUsersByRole(int updaterId, int topicId, string role, int[] userIds)
        {

            Topic topic;
            try
            {
                topic = dbContext.Topics.Include(t => t.TopicUsers).Single(t => t.Id == topicId);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            var existingUsers = topic.TopicUsers.Where(tu => tu.Role == role).ToList();

            var newUsers = new List<TopicUser>();
            var removedUsers = new List<TopicUser>();

            if (userIds != null)
            {
                // new user?
                foreach (int userId in userIds)
                {
                    if (!existingUsers.Any(tu => (tu.UserId == userId && tu.Role == role)))
                        newUsers.Add(new TopicUser() { UserId = userId, Role = role });
                }
                // removed user?
                foreach (TopicUser existingUser in existingUsers)
                {
                    if (!userIds.Contains(existingUser.UserId))
                        removedUsers.Add(existingUser);
                }
            }

            topic.TopicUsers.AddRange(newUsers);
            topic.TopicUsers.RemoveAll(tu => removedUsers.Contains(tu));
            // Updated // TODO add user
            topic.UpdatedAt = DateTime.Now;
            // Notifications
            new NotificationProcessor(dbContext, topic, updaterId).OnUsersChanged(newUsers, removedUsers, role);

            dbContext.Update(topic);
            dbContext.SaveChanges();
            return true;
        }

        public virtual IEnumerable<Topic> GetSubTopics(int topicId)
        {
            return dbContext.AssociatedTopics.Include(at => at.ChildTopic).Where(at => at.ParentTopicId == topicId).Select(at => at.ChildTopic).ToList();
        }

        public virtual IEnumerable<Topic> GetParentTopics(int topicId)
        {
            return dbContext.AssociatedTopics.Include(at => at.ParentTopic).Where(at => at.ChildTopicId == topicId).Select(at => at.ParentTopic).ToList();
        }

        public virtual EntityResult AddTopic(int userId, TopicFormModel model)
        {
            try
            {
                var topic = new Topic(model);
                topic.CreatedById = userId;
                dbContext.Topics.Add(topic);
                dbContext.SaveChanges();
                new NotificationProcessor(dbContext, topic, userId).OnNewTopic();

                return EntityResult.Successfull(topic.Id);
            }
            catch (Exception e)
            {
                return EntityResult.Error(e.Message);
            }
        }

        public virtual bool UpdateTopic(int userId, int topicId, TopicFormModel model)
        {
            // Using Transactions to roobback Notifications on error.
            using (var transaction = dbContext.Database.BeginTransaction())
                try
                {
                    var topic = dbContext.Topics.Include(t => t.TopicUsers).Single(t => t.Id == topicId);
                    // REM: do before updating to estimate the changes
                    new NotificationProcessor(dbContext, topic, userId).OnUpdate(model);

                    // TODO  topic.UpdatedById = userId;
                    topic.Title = model.Title;
                    topic.Status = model.Status;
                    topic.Deadline = (DateTime)model.Deadline;
                    topic.Description = model.Description;
                    topic.Requirements = model.Requirements;

                    dbContext.SaveChanges();
                    transaction.Commit();
                    return true;
                }
                catch (InvalidOperationException)
                {
                    //Not found
                    return false;
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    Console.WriteLine(ex.ToString());
                    return false;
                }
        }

        public bool ChangeTopicStatus(int userId, int topicId, string status)
        {
            try
            {
                var topic = dbContext.Topics.Include(t => t.TopicUsers).Single(t => t.Id == topicId);
                topic.Status = status;
                dbContext.Update(topic);
                dbContext.SaveChanges();
                new NotificationProcessor(dbContext, topic, userId).OnStateChanged(status);
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public virtual bool DeleteTopic(int topicId, int userId)
        {
            try
            {
                var topic = dbContext.Topics.Include(t => t.TopicUsers).Single(u => u.Id == topicId);
                new NotificationProcessor(dbContext, topic, userId).OnDeleteTopic();
                dbContext.Remove(topic);
                dbContext.SaveChanges();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        public virtual EntityResult AssociateTopic(int parentId, int childId)
        {
            // TODO throw errors
            if (!dbContext.Topics.Any(t => t.Id == childId))
                return EntityResult.Error("Child not Found");
            if (!dbContext.Topics.Any(t => t.Id == parentId))
                return EntityResult.Error("Parent not Found");

            if (dbContext.AssociatedTopics.Any(at => at.ChildTopicId == childId && at.ParentTopicId == parentId))
                return EntityResult.Error("Allready exists");

            var relation = new AssociatedTopic() { ChildTopicId = childId, ParentTopicId = parentId };

            dbContext.Add(relation);
            dbContext.SaveChanges();
            return EntityResult.Successfull(null);
        }

        public virtual bool DeleteAssociated(int parentId, int childId)
        {
            try
            {
                var relation = dbContext.AssociatedTopics.Single(ta => (ta.ParentTopicId == parentId && ta.ChildTopicId == childId));
                dbContext.Remove(relation);
                dbContext.SaveChanges();
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }
}