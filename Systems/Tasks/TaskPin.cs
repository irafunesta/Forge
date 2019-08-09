// Copyright (C) 2017 Schroedinger Entertainment
// Distributed under the Schroedinger Entertainment EULA (See EULA.md for details)

using System;
using System.Collections.Generic;
using System.Threading;

namespace SE.Forge.Systems
{
    public class TaskPin : IDisposable
    {
        public readonly static TaskPin[] Empty = new TaskPin[0];

        int hasData;
        public bool HasData
        {
            get { return (Interlocked.CompareExchange(ref hasData, 0, 0) != 0); }
        }

        int disposed;
        public bool Disposed
        {
            get { return (Interlocked.CompareExchange(ref disposed, 0, 0) != 0); }
        }

        Task owner;
        public Task Owner
        {
            get { return owner; }
        }

        protected Type dataType;
        public Type DataType
        {
            get { return dataType; }
        }

        protected object data;
        public object Data
        {
            get { return data; }
            set 
            { 
                data = value;
                Interlocked.CompareExchange(ref hasData, 1, 0);
            }
        }

        protected TaskPin parent;
        public TaskPin Parent
        {
            get { return parent; }
            internal set { parent = value; }
        }

        protected bool locked;
        public bool Locked
        {
            get { return locked; }
            set { locked = value; }
        }

        public TaskPin(Task owner, Type dataType, object data)
        {
            this.owner = owner;
            this.dataType = dataType;
            this.data = data;
        }
        public TaskPin(Task owner, Type dataType)
            :this(owner, dataType, null)
        { }
        public TaskPin(Task owner, object data)
            : this(owner, data.GetType(), data)
        { }

        public virtual bool Accepts(TaskPin outputPin)
        {
            return (dataType.IsAssignableFrom(outputPin.dataType));
        }

        public void Dispose()
        {
            if (parent != null)
                parent = null;

            Interlocked.CompareExchange(ref disposed, 1, 0);
            data = null;
        }
    }
}
