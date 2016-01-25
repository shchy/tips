using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tips.Model.Models
{
    public interface INameable
    {
        string Name { get; }
    }




    public interface IResource : IIdentity<int>, INameable
    {
        /// <summary>
        /// V/H
        /// </summary>
        double Cost { get; }
    }

    public interface IRange<T>
    {
        T Left { get; }
        T Right { get; }
    }

    public interface ITask : IIdentity<int>, INameable
    {
        double Value { get; }
    }

    public interface ITaskRecord : IIdentity<int>
    {
        int TaskId { get; }
        DateTime Day { get; }
        double Value { get; }
        double WorkValue { get; }
        IResource Who { get; }
    }

    //public interface ITaskWithRecord 
    //{
    //    ITask Task { get; }
    //    IEnumerable<ITaskRecord> Records { get;}
    //}

    public interface IPlan : IIdentity<int>
    {
        DateTime Day { get; }
        double Value { get; }
        IResource Who { get; }
    }

    public interface IItelator : IIdentity<int>, INameable
    {
        IEnumerable<ITask> Tasks { get; }
        IEnumerable<ITaskRecord> Records { get; }
        IEnumerable<IPlan> Plans { get; }
    }

    public interface IProject : IIdentity<int>, INameable
    {
        IEnumerable<IItelator> Itelators { get; }
    }






}
