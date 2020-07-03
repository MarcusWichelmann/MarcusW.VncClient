using System;
using System.Collections.Generic;

namespace MarcusW.VncClient.Utils
{
    /// <summary>
    /// Represents a parameters object that can be made immutable.
    /// </summary>
    public abstract class FreezableParametersObject
    {
        private volatile bool _isFrozen;

        /// <summary>
        /// Gets whether this object is frozen.
        /// </summary>
        public bool IsFrozen => _isFrozen;

        /// <summary>
        /// Validates the parameters of this object and throws a <see cref="ConnectParametersValidationException"/> for the first error found.
        /// </summary>
        public abstract void Validate();

        /// <summary>
        /// Returns all referenced <see cref="FreezableParametersObject"/> descendants of this object or <see langword="null"/>, when there aren't any.
        /// </summary>
        protected abstract IEnumerable<FreezableParametersObject?>? GetDescendants();

        /// <summary>
        /// Validates the parameters of this object and all descendants and throws a <see cref="ConnectParametersValidationException"/> for the first error found.
        /// </summary>
        public void ValidateRecursively()
        {
            IEnumerable<FreezableParametersObject?>? descendants = GetDescendants();
            if (descendants != null)
            {
                foreach (FreezableParametersObject? descendant in descendants)
                    descendant?.ValidateRecursively();
            }

            Validate();
        }

        /// <summary>
        /// Validates this object and all descendants and makes them immutable in case the validation succeeded.
        /// When this happens, all following write operations will fail.
        /// </summary>
        public void ValidateAndFreezeRecursively()
        {
            // Freeze this object and all descendants so no changes are possible during validation
            SetFrozenRecursively(true);

            try
            {
                ValidateRecursively();
            }
            catch
            {
                // When any validation failed, unfreeze everything so it can be fixed.
                SetFrozenRecursively(false);

                throw;
            }
        }

        /// <summary>
        /// Throws a <see cref="InvalidOperationException"/> if this object is frozen.
        /// </summary>
        protected void ThrowIfFrozen()
        {
            if (_isFrozen)
                throw new InvalidOperationException("The object is frozen and now immutable.");
        }

        /// <summary>
        /// Throws a <see cref="InvalidOperationException"/> if this object is frozen, otherwise <paramref name="action"/> is executed.
        /// </summary>
        /// <param name="action">The action to execute, if the object was not frozen.</param>
        protected void ThrowIfFrozen(Action action)
        {
            if (_isFrozen)
                throw new InvalidOperationException("The object is frozen and now immutable.");
            action.Invoke();
        }

        private void SetFrozen(bool frozen) => _isFrozen = frozen;

        private void SetFrozenRecursively(bool frozen)
        {
            IEnumerable<FreezableParametersObject?>? descendants = GetDescendants();
            if (descendants != null)
            {
                foreach (FreezableParametersObject? descendant in descendants)
                    descendant?.SetFrozenRecursively(frozen);
            }

            SetFrozen(frozen);
        }
    }
}
