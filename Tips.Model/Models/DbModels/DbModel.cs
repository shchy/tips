using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models.DbModels
{
    public class DbUser : User
    {
    }

    public class DbProject 
    {
        public string Describe { get; set; }

        public int Id { get; set; }

        public string Name { get; set; }
    }

    public class DbSprint
    {
        public int Id { get; set; }

        public string Name { get; set; }

        [NotMapped]
        public DateTime? Left
        {
            get { return this.LeftTicks.ToDateTime(); }
            set { this.LeftTicks = value.ToTicks(); }
        }

        [NotMapped]
        public DateTime? Right
        {
            get { return this.RightTicks.ToDateTime(); }
            set { this.RightTicks = value.ToTicks(); }
        }

        [Column("Left")]
        public long? LeftTicks { get; set; }
        [Column("Right")]
        public long? RightTicks { get; set; }
    }

    public class DbTaskItem
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public double? Value { get; set; }
    }

    public class DbTaskComment 
    {
        [NotMapped]
        public DateTime Day
        {
            get { return this.DayTicks.ToDateTime(); }
            set { this.DayTicks = value.ToTicks(); }
        }
        [Column("Day")]
        public long DayTicks { get; set; }

        public int Id { get; set; }

        public string Text { get; set; }

        public string UserId { get; set; }
    }

    public class DbTaskRecord 
    {
        [NotMapped]
        public DateTime Day
        {
            get { return this.DayTicks.ToDateTime(); }
            set { this.DayTicks = value.ToTicks(); }
        }
        [Column("Day")]
        public long DayTicks { get; set; }

        public int Id { get; set; }

        public double Value { get; set; }

        public string UserId { get; set; }

        public double WorkValue { get; set; }

    }

    public class DbLinkTaskItemWithRecord
    {
        public int TaskItemId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int TaskRecordId { get; set; }
        public int IsDeleted { get; set; }

    }

    public class DbLinkTaskItemWithComment
    {
        public int TaskItemId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int TaskCommentId { get; set; }
        public int IsDeleted { get; set; }

    }

    public class DbLinkProjectWithSprint
    {
        public int ProjectId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int SprintId { get; set; }

        public int Sort { get; set; }
        public int IsDeleted { get; set; }
    }

    public class DbLinkSprintWithTask
    {
        public int SprintId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int TaskItemId { get; set; }

        public int Sort { get; set; }
        public int IsDeleted { get; set; }

    }

    public class DbLinkUserWithTaskItem
    {
        public string UserId { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        [Key]
        public int TaskItemId { get; set; }
        public int IsDeleted { get; set; }
    }


    public static class DbModelExtension
    {
        public static DbProject ToDbModel(this IProject @this)
        {
            return new DbProject
            {
                Id = @this.Id,
                Name = @this.Name,
                Describe = @this.Describe,
            };
        }

        public static DbSprint ToDbModel(this ISprint @this)
        {
            return new DbSprint
            {
                Id = @this.Id,
                Name = @this.Name,
                Left = @this.Left,
                Right = @this.Right,
            };
        }

        public static DbTaskItem ToDbModel(this ITaskItem @this)
        {
            return new DbTaskItem
            {
                Id = @this.Id,
                Name = @this.Name,
                Value = @this.Value,
            };
        }

        public static DbUser ToDbModel(this IUser @this)
        {
            return new DbUser
            {
                Id = @this.Id,
                Name = @this.Name,
                Password = @this.Password,
                Role = @this.Role,
                IconFile = @this.IconFile,
            };
        }

        public static DbTaskComment ToDbModel(this ITaskComment @this)
        {
            return new DbTaskComment
            {
                Id = @this.Id,
                Day = @this.Day,
                Text = @this.Text,
                UserId = @this.Who.Id,
            };
        }

        public static DbTaskRecord ToDbModel(this ITaskRecord @this)
        {
            return new DbTaskRecord
            {
                Id = @this.Id,
                Day = @this.Day,
                UserId = @this.Who.Id,
                Value = @this.Value,
                WorkValue = @this.WorkValue,
            };
        }



        public static DbLinkProjectWithSprint ToDbLink(this DbProject @this, DbSprint sprint, int order)
        {
            return new DbLinkProjectWithSprint
            {
                ProjectId = @this.Id,
                SprintId = sprint.Id,
                Sort = order,
            };
        }

        public static DbLinkSprintWithTask ToDbLink(this DbSprint @this, DbTaskItem taskitem, int order)
        {
            return new DbLinkSprintWithTask
            {
                SprintId = @this.Id,
                TaskItemId = taskitem.Id,
                Sort = order,
            };
        }

        public static DbLinkTaskItemWithComment ToDbLink(this DbTaskComment @this, int taskId)
        {
            return new DbLinkTaskItemWithComment
            {
                TaskItemId = taskId,
                TaskCommentId = @this.Id,
            };
        }

        public static DbLinkTaskItemWithRecord ToDbLink(this DbTaskRecord @this, int taskId)
        {
            return new DbLinkTaskItemWithRecord
            {
                TaskItemId = taskId,
                TaskRecordId = @this.Id,
            };
        }

        public static DbLinkUserWithTaskItem ToDbLink(this IUser @this, int taskId)
        {
            return new DbLinkUserWithTaskItem
            {
                TaskItemId = taskId,
                UserId = @this.Id,
            };
        }

        public static ISprint BuildModel(this DbSprint @this, IEnumerable<DbTaskItem> taskItems)
        {
            return
                @this.ToModel(taskItems.Select(x => x.ToModel()).ToArray());
        }

        public static IProject ToModel(this DbProject @this, IEnumerable<ISprint> sprints)
        {
            return new Project
            {
                Id = @this.Id,
                Name = @this.Name,
                Describe = @this.Describe,
                Sprints = sprints.ToArray(),
            };
        }


        public static ISprint ToModel(this DbSprint @this, IEnumerable<ITaskItem> taskItems)
        {
            return new Sprint
            {
                Id = @this.Id,
                Name = @this.Name,
                Left = @this.Left,
                Right = @this.Right,
                Tasks = taskItems.ToArray(),
            };
        }

        public static ITaskItem ToModel(this DbTaskItem @this)
        {
            return new TaskItem
            {
                Id = @this.Id,
                Name = @this.Name,
                Value = @this.Value,
            };
        }

        public static ITaskRecord ToModel(this DbTaskRecord @this, IUser user)
        {
            return new TaskRecord
            {
                Id = @this.Id,
                Day = @this.Day,
                Value = @this.Value,
                Who = user,
                WorkValue = @this.WorkValue,
            };
        }
        public static ITaskComment ToModel(this DbTaskComment @this, IUser user)
        {
            return new TaskComment
            {
                Id = @this.Id,
                Day = @this.Day,
                Text = @this.Text,
                Who = user,
            };
        }






        public static DateTime? ToDateTime(this long? ticks)
        {
            return 
                ticks.HasValue 
                ? new DateTime?(new DateTime(ticks.Value)) 
                : null;
        }

        public static long? ToTicks(this DateTime? datetime)
        {
            if (datetime.HasValue)
                return datetime.Value.Ticks;
            else
                return null;
        }

        public static DateTime ToDateTime(this long ticks)
        {
            return
                new DateTime(ticks);
        }

        public static long ToTicks(this DateTime datetime)
        {
            return datetime.Ticks;
        }
    }
}
