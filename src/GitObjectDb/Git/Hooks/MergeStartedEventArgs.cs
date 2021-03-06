using GitObjectDb.Models.Compare;
using System;
using System.ComponentModel;

namespace GitObjectDb.Git.Hooks
{
    /// <summary>
    /// Provides data for a pre-merge event.
    /// </summary>
    /// <seealso cref="System.ComponentModel.CancelEventArgs" />
    public class MergeStartedEventArgs : CancelEventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MergeStartedEventArgs"/> class.
        /// </summary>
        /// <param name="changes">The changes.</param>
        /// <exception cref="ArgumentNullException">message</exception>
        public MergeStartedEventArgs(ObjectRepositoryChanges changes)
        {
            Changes = changes ?? throw new ArgumentNullException(nameof(changes));
        }

        /// <summary>
        /// Gets the changes.
        /// </summary>
        public ObjectRepositoryChanges Changes { get; }
    }
}
