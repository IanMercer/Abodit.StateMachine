using System;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Abodit.StateMachine
{
    /// <summary>
    /// An event that causes the state machine to transition to a new state - you can use any object
    /// that implements IEquatable of T.  You might also want to mark it Serializable.
    /// </summary>
    /// <remarks>
    /// Using this class you can create simple named events that have all of the required properties to work in the State Machine
    /// </remarks>
    [DebuggerDisplay("Event = {Name}")]
    [Serializable]
    public class Event : IEquatable<Event>
    {
        /// <summary>
        /// Events with the same name (within a state machine) are considered to be the same event
        /// so you don't need the specific same Event in order to fire it
        /// Unlike States where we do extra work to get a consistent stateDefinition
        /// </summary>
        [XmlAttribute]
        public string Name { get; set; } = "NOT SET";

        private Event()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="Event"/> class
        /// </summary>
        public Event(string name)
        {
            // Remove extra marks we used to have on Event.ToString()
            this.Name = name.TrimStart('~').TrimEnd('~');
        }

        /// <summary>
        /// ToString
        /// </summary>
        public override string ToString()
        {
            // MUST RETURN THE NAME EXACTLY AS THAT IS USE FOR SERIALIZATION AND DESERIALIZATION
            return this.Name;
        }

        /// <summary>
        /// Equals
        /// </summary>
        public override bool Equals(object? obj)
        {
            if (!(obj is Event)) return false;
            return this.Name.Equals(((Event)obj).Name);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return this.Name.GetHashCode();
        }

        /// <inheritdoc/>
        public static bool operator ==(Event a, Event b)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(a, b))
            {
                return true;
            }
            if (ReferenceEquals(a, null)) return false;
            return a.Equals(b);
        }

        /// <summary>
        /// Equals
        /// </summary>
        public static bool operator !=(Event a, Event b)
        {
            return !(a == b);
        }

        /// <inheritdoc/>
        public bool Equals(Event? other)
        {
            if (other is null) return false;
            else return this.Name.Equals(other.Name);
        }
    }
}