using BNG;
using HapticPatterns;
using Sirenix.OdinInspector;
using AEB.BNG;
using UnityEngine;

namespace AEB.IAIRoulette
{
    /// <summary>
    /// Represents a device in the IAI Roulette game. 
    /// A device is a type of <see cref="Machine"/> that can be grabbed, released,
    /// and provides optional haptic feedback during interaction.
    /// </summary>
    public class Device : Machine
    {
        #region Properties 

        /// <summary> 
        /// The Grabbable component that allows the item to be grabbed.  
        /// </summary>
        /// </summary>
        public Grabbable Grabbable => itemGrabbable;

        /// <summary> 
        /// The Unity events associated with the Grabbable component.  
        /// </summary>
        public GrabbableUnityEvents GrabbableEvents => itemGrabbableEvents;

        #endregion

        #region Fields

        /// <summary>
        /// The Grabbable component that allows the item to be grabbed.
        /// </summary>
        [FoldoutGroup(REFERENCES_GROUP), TitleGroup(ITEM_TITLE), SerializeField, Required, PropertyOrder(10)]
        protected Grabbable itemGrabbable;

        /// <summary>
        /// The Unity events associated with the Grabbable component.
        /// </summary>
        [FoldoutGroup(REFERENCES_GROUP), TitleGroup(ITEM_TITLE), SerializeField, Required, PropertyOrder(10)]
        protected GrabbableUnityEvents itemGrabbableEvents;

        /// <summary>
        /// Enables or disables haptic feedback.
        /// </summary>
        [FoldoutGroup(SETTINGS_GROUP), TitleGroup(HAPTIC_TITLE), SerializeField]
        protected bool hapticEnabled = true;

        /// <summary>
        /// The haptic pattern played when pulling the trigger.
        /// </summary>
        [FoldoutGroup(SETTINGS_GROUP), TitleGroup(HAPTIC_TITLE), SerializeField]
        protected HapticPattern hapticPattern;

        protected bool _hapticSafety = true;

        #endregion

        #region Unity

        protected override void OnEnable()
        {
            itemGrabbableEvents.onGrab.AddListener(HandleOnGrab);
            itemGrabbableEvents.onRelease.AddListener(HandleOnRelease);
        }

        protected override void OnDisable()
        {
            itemGrabbableEvents.onGrab.RemoveListener(HandleOnGrab);
            itemGrabbableEvents.onRelease.RemoveListener(HandleOnRelease);
        }

        #endregion

        #region Public

        /// <summary>
        /// Enables or disables the device.
        /// </summary>
        /// <param name="enabled">If true, the device becomes functional; otherwise, it is disabled.</param>
        public override void Enable(bool enabled)
        {
            Functional = enabled;
            base.Enable(enabled);
        }

        #endregion

        #region Protected

        protected virtual void PlayHaptic(ControllerHand handSide, float timePoint)
        {
            if (hapticPattern == null || !hapticEnabled || !_hapticSafety) return;

            HapticPatternBridge.PlayGradually(hapticPattern, timePoint, handSide);
        }

        protected virtual void PlayHaptic(ControllerHand handSide)
        {
            if (hapticPattern == null || !hapticEnabled || !_hapticSafety) return;

            HapticPatternBridge.PlayOverTime(hapticPattern, handSide);
        }

        #endregion

        #region Private

        void HandleOnRelease()
        {
            _hapticSafety = false;
        }

        void HandleOnGrab(Grabber grabber)
        {
            _hapticSafety = true;
        }

        #endregion

    }
}