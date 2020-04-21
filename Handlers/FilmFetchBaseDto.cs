using System;

namespace S3ResumeTest.Handlers
{
    public class FilmFetchBaseDto
    {
        /// <summary>
        /// Gets the id
        /// </summary>
        /// <value></value>
        public string Id { get; set; } // Mvdh : removed protected. Problem with nested objects, for example film > content containers
        /// <summary>
        /// Gets the date/time when this item was created
        /// </summary>
        /// <value>Date/time when this item was created</value>
        public DateTime Created { get; protected set; }
        /// <summary>
        /// Gets the date/time when this item was last changed
        /// </summary>
        /// <value>Date/time when this item was last changed</value>
        public DateTime Changed { get; protected set; }

        /// <summary>
        /// Gets or sets a value indicating whether this item is active.
        /// </summary>
        /// <value><c>true</c> if is active; otherwise, <c>false</c>.</value>
        public bool IsActive { get; set; }
    }
}