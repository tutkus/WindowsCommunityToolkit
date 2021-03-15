﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Diagnostics.Contracts;
using Windows.Foundation.Collections;
using Windows.UI.Composition;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Hosting;

namespace Microsoft.Toolkit.Uwp.UI.Animations
{
    /// <summary>
    /// A collection of implicit animations that can be grouped together. This type represents a composite animation
    /// (such as <see cref="Windows.UI.Xaml.Media.Animation.Storyboard"/>) that is executed on a given element.
    /// </summary>
    public sealed class ImplicitAnimationSet : DependencyObjectCollection
    {
        /// <summary>
        /// Raised whenever any configuration change occurrs within the current <see cref="ImplicitAnimationSet"/> instance.
        /// </summary>
        internal event EventHandler? AnimationsChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImplicitAnimationSet"/> class.
        /// </summary>
        public ImplicitAnimationSet()
        {
            VectorChanged += this.ImplicitAnimationSetVectorChanged;
        }

        /// <summary>
        /// Registers <see cref="RaiseAnimationsChanged(object, EventArgs)"/> for every added animation.
        /// </summary>
        /// <param name="sender">The current vector of animations.</param>
        /// <param name="event">The <see cref="IVectorChangedEventArgs"/> instance for the current event.</param>
        private void ImplicitAnimationSetVectorChanged(IObservableVector<DependencyObject> sender, IVectorChangedEventArgs @event)
        {
            if (@event.CollectionChange == CollectionChange.ItemInserted ||
                @event.CollectionChange == CollectionChange.ItemChanged)
            {
                IInternalImplicitAnimation item = (IInternalImplicitAnimation)sender[(int)@event.Index];

                item.AnimationPropertyChanged -= RaiseAnimationsChanged;
                item.AnimationPropertyChanged += RaiseAnimationsChanged;
            }

            AnimationsChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Raisess the <see cref="AnimationsChanged"/> event.
        /// </summary>
        /// <param name="sender">The instance raising the event.</param>
        /// <param name="e">The empty <see cref="EventArgs"/> for the event.</param>
        private void RaiseAnimationsChanged(object sender, EventArgs e)
        {
            AnimationsChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Gets or sets the weak reference to the parent that owns the current implicit animation collection.
        /// </summary>
        internal WeakReference<UIElement>? ParentReference { get; set; }

        /// <summary>
        /// Creates a <see cref="CompositionAnimationGroup"/> for the current collection.
        /// This can be used to be assigned to show/hide implicit composition animations.
        /// </summary>
        /// <returns>The <see cref="CompositionAnimationGroup"/> instance to use.</returns>
        [Pure]
        internal CompositionAnimationGroup GetCompositionAnimationGroup()
        {
            UIElement parent = GetParent();
            Compositor compositor = ElementCompositionPreview.GetElementVisual(parent).Compositor;
            CompositionAnimationGroup animations = compositor.CreateAnimationGroup();

            foreach (IImplicitTimeline timeline in this)
            {
                animations.Add(timeline.GetAnimation(parent, out _));
            }

            return animations;
        }

        /// <summary>
        /// Creates an <see cref="ImplicitAnimationCollection"/> for the current collection.
        /// This can be used to be assigned to implicit composition animations.
        /// </summary>
        /// <returns>The <see cref="ImplicitAnimationCollection"/> instance to use.</returns>
        [Pure]
        internal ImplicitAnimationCollection GetImplicitAnimationCollection()
        {
            UIElement parent = GetParent();
            Compositor compositor = ElementCompositionPreview.GetElementVisual(parent).Compositor;
            ImplicitAnimationCollection animations = compositor.CreateImplicitAnimationCollection();

            foreach (IImplicitTimeline timeline in this)
            {
                CompositionAnimation animation = timeline.GetAnimation(parent, out string? target);

                target ??= animation.Target;

                if (!animations.ContainsKey(target))
                {
                    animations[target] = animations.Compositor.CreateAnimationGroup();
                }

                ((CompositionAnimationGroup)animations[target]).Add(animation);
            }

            return animations;
        }

        /// <summary>
        /// Gets the current parent <see cref="UIElement"/> instance.
        /// </summary>
        /// <returns>The <see cref="UIElement"/> reference from <see cref="ParentReference"/>.</returns>
        /// <exception cref="InvalidOperationException">Thrown if there is no parent available.</exception>
        [Pure]
        private UIElement GetParent()
        {
            UIElement? parent = null;

            if (ParentReference?.TryGetTarget(out parent) != true)
            {
                ThrowInvalidOperationException();
            }

            return parent!;

            static void ThrowInvalidOperationException() => throw new InvalidOperationException("The current ImplicitAnimationSet object isn't bound to a parent UIElement instance.");
        }
    }
}
