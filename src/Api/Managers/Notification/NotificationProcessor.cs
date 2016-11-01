﻿using System;
using System.Collections.Generic;
using Api.Data;
using Api.Models.Entity;
using Api.Models;
using Api.Models.Notifications;
using System.Linq;

namespace Api.Managers
{
    public class NotificationProcessor : BaseManager
    {

        private List<int> notifiedUsers = new List<int>();
        private Topic topic;
        private int currentUser;

        public NotificationProcessor(CmsDbContext dbContext, Topic currentTopic, int currentUser) : base(dbContext)
        {
            this.topic = currentTopic;
            this.currentUser = currentUser;
            // Don not notify yourself
            notifiedUsers.Add(currentUser);
        }

        public void OnNewTopic()
        {
            NotifyAll(NotificationType.TOPIC_CREATED);
            finnish();
        }

        public void OnDeleteTopic()
        {
            NotifyAll(NotificationType.TOPIC_DELETED, topic.Title);
            finnish();
        }

        public void OnStateChanged(String state)
        {
            NotifyAll(NotificationType.TOPIC_STATE_CHANGED, state);
            finnish();
        }

        public void OnAttachmetAdded(String name)
        {
            NotifyAll(NotificationType.TOPIC_ATTACHMENT_ADDED, name);
            finnish();
        }

        private void NotifyAll(NotificationType type, string data = null)
        {
            topic.TopicUsers.ForEach(tu => createNotification(tu.UserId, type, data));
        }

        #region OnUpdate

        public void OnUpdate(TopicFormModel changes)
        {

            // Roles Changed
            checkChangedUsers(changes.Students, Role.Student);
            checkChangedUsers(changes.Supervisors, Role.Supervisor);
            checkChangedUsers(changes.Reviewers, Role.Reviewer);
            // Deadline Changed
            if (changes.Deadline != topic.Deadline)
                NotifyAll(NotificationType.TOPIC_DEADLINE_CHANGED, changes.Deadline.ToString());
            else if ((changes.Status != topic.Status))
                NotifyAll(NotificationType.TOPIC_STATE_CHANGED, changes.Status);
            else
                NotifyAll(NotificationType.TOPIC_UPDATED);

            finnish();
        }

        private void checkChangedUsers(int[] userIds, String role)
        {
            var existingUsers = topic.TopicUsers.Where(tu => tu.Role == role).ToList();
            foreach (int userId in userIds)
            {
                if (!existingUsers.Any(tu => (tu.UserId == userId && tu.Role == role)))
                    createNotification(userId, NotificationType.TOPIC_ASSIGNED_TO, role);
            }
            // removed user?
            foreach (TopicUser existingUser in existingUsers)
            {
                if (!userIds.Contains(existingUser.UserId))
                    createNotification(existingUser.UserId, NotificationType.TOPIC_REMOVED_FROM, role);
            }
        }

        #endregion

        #region createNotification

        private void createNotification(int userId, NotificationType type, string data = null)
        {
            if (notifiedUsers.Contains(userId))
                return;

            Notification not = new Notification() { UpdaterId = currentUser, Type = type, UserId = userId };
            if (topic != null)
                not.TopicId = topic.Id;
            if (data != null)
                not.Data = data;

            notifiedUsers.Add(userId);
            dbContext.Notifications.Add(not);
        }

        private void finnish()
        {
            dbContext.SaveChanges();
        }
        #endregion
    }
}