﻿using MicroFocus.Adm.Octane.Api.Core.Entities;

namespace Hpe.Nga.Octane.VisualStudio.Common
{
    /// <summary>
    /// Utility class
    /// </summary>
    public static class Utility
    {
        /// <summary>
        /// Returns the child's property of the given entity
        /// </summary>
        /// <param name="entity">parent entity</param>
        /// <param name="child">child entity property name</param>
        /// <param name="property">property name to be retieved from the child entity</param>
        public static object GetPropertyOfChildEntity(BaseEntity entity, string child, string property)
        {
            var childEntity = (BaseEntity)entity.GetValue(child);
            return childEntity?.GetValue(property);
        }
    }
}