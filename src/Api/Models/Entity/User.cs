﻿using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models.Entity
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        [Required]
        public string Role { get; set; }

        public string ProfilePicture { get; set; }

        public List<TopicAttatchment> Attatchments { get; set; }

        public List<TopicUser> TopicUsers { get; set; }

        #region Utility Methods

        [NotMapped]
        public string Picture
        {
            get
            {
                if (!HasProfilePicture())
                    return "default.jpg";
                return ProfilePicture;
            }
        }

        public bool HasProfilePicture()
        {
            return !(ProfilePicture == null || ProfilePicture.Length == 0);
        }

        public string FullName
        {
            get
            {
                return FirstName + ' ' + LastName;
            }
        }

        public bool IsAdministrator()
        {
            return Role == Api.Models.Role.Administrator;
        }

        public bool IsSupervisor()
        {
            return Role == Api.Models.Role.Supervisor;
        }

        public bool IsStudent()
        {
            return Role == Api.Models.Role.Student;
        }

        #endregion
    }
}