using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
            get { return this.LeftTicks.ToDateTime(); }
            set { this.LeftTicks = value.ToTicks(); }
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

        public double Value { get; set; }
    }

    public class DbLinkProjectWithSprint
    {
        public int ProjectId { get; set; }
        [Key]
        public int SprintId { get; set; }
    }

    public class DbLinkSprintWithTask
    {
        public int SprintId { get; set; }
        [Key]
        public int TaskItemId { get; set; }
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
            };
        }

        public static DbLinkProjectWithSprint ToDbLink(this DbProject @this, DbSprint sprint)
        {
            return new DbLinkProjectWithSprint
            {
                ProjectId = @this.Id,
                SprintId = sprint.Id,
            };
        }

        public static DbLinkSprintWithTask ToDbLink(this DbSprint @this, DbTaskItem taskitem)
        {
            return new DbLinkSprintWithTask
            {
                SprintId = @this.Id,
                TaskItemId = taskitem.Id,
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
    }
}
