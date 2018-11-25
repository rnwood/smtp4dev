// <copyright file="Parameter.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.Globalization;

    /// <summary>
    /// Defines the <see cref="Parameter" />
    /// </summary>
    public class Parameter : IEquatable<Parameter>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Parameter"/> class.
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="value">The value<see cref="string"/></param>
        public Parameter(string name, string value)
        {
            this.Name = name;
            this.Value = value;
        }

        /// <summary>
        /// Gets the Name
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Gets the Value
        /// </summary>
        public string Value { get; private set; }

        /// <summary>
        /// Determines whether the specified <see cref="object" />, is equal to this instance.
        /// </summary>
        /// <param name="obj">The obj<see cref="object" /></param>
        /// <returns>
        /// The <see cref="bool" />
        /// </returns>
        public override bool Equals(object obj)
        {
            if (obj is null)
            {
                return false;
            }

            if (ReferenceEquals(this, obj))
            {
                return true;
            }

            if (obj.GetType() != typeof(Parameter))
            {
                return false;
            }

            return this.Equals((Parameter)obj);
        }

        /// <summary>
        /// Indicates whether the current object is equal to another object of the same type.
        /// </summary>
        /// <param name="other">The other<see cref="Parameter" /></param>
        /// <returns>
        /// The <see cref="bool" />
        /// </returns>
        public bool Equals(Parameter other)
        {
            if (other is null)
            {
                return false;
            }

            if (ReferenceEquals(this, other))
            {
                return true;
            }

            return string.Equals(other.Name, this.Name, StringComparison.OrdinalIgnoreCase)
                && string.Equals(other.Value, this.Value, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// The <see cref="int" />
        /// </returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((this.Name != null ? this.Name.ToUpperInvariant().GetHashCode() : 0) * 397) ^ (this.Value != null ? this.Value.GetHashCode() : 0);
            }
        }
    }
}
