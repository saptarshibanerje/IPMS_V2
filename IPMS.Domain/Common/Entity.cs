using System;
using System.Collections.Generic;

namespace IPMS.Domain.Common
{
    /// <summary>
    /// EVERYTHING in our system that has its own identity (its own Id, and we care
    /// WHICH ONE it is, not just what values it holds) inherits from this class.
    /// Example: a Subject, a Batch, a Revision — each has an Id, and renaming or
    /// changing its data does NOT make it a "different" one. It's still "that one."
    /// (This is the DDD term "Entity".)
    /// </summary>
    public abstract class Entity<TId>
    {
        public TId Id { get; protected set; }
        private int? _cachedHashCode;
        // Audit columns — same idea as your existing CommonModel base class, just
        // moved onto this shared base so every Entity gets them automatically
        // instead of copy-pasting the same 6 fields into every class.
        public string CreatedBy { get; protected set; }
        public DateTime CreatedDate { get; protected set; }
        public string UpdatedBy { get; protected set; }
        public DateTime UpdatedDate { get; protected set; }

        // Used to detect "someone else edited this at the same time as me"
        // (this is what your existing RowVersion / timestamp column already does —
        // unchanged behaviour, just inherited instead of repeated).
        public byte[] RowVersion { get; protected set; } = Array.Empty<byte>();

        public bool IsDelete { get; protected set; }


        public bool IsTransient() => EqualityComparer<TId>.Default.Equals(Id, default);
        // Two Entities are considered "the same thing" if they have the same Id —
        // even if every other field on them is different. This is the OPPOSITE
        // rule from ValueObject.cs, which compares by content instead.
        public override bool Equals(object obj)
        {
            if (obj == null || obj is not Entity<TId>)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (GetType() != obj.GetType())
                return false;

            var item = (Entity<TId>)obj;

            if (item.IsTransient() || this.IsTransient())
                return false;

            return EqualityComparer<TId>.Default.Equals(item.Id, Id);


        }

        public override int GetHashCode()
        {
            if (!IsTransient())
            {
                if (!_cachedHashCode.HasValue)
                {
                    _cachedHashCode = EqualityComparer<TId>.Default.GetHashCode(Id) ^ 31;
                }

                return _cachedHashCode.Value;
            }

            return base.GetHashCode();
        }

        public static bool operator ==(Entity<TId> left, Entity<TId> right)
        {
            if (ReferenceEquals(left, null))
                return ReferenceEquals(right, null);

            return left.Equals(right);
        }

        public static bool operator !=(Entity<TId> left, Entity<TId> right)
        {
            return !(left == right);
        }
    }
}
