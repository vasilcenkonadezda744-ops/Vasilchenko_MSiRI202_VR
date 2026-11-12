using UnityEngine;
using Valve.VR.InteractionSystem;

[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class TrainingBlank : MonoBehaviour
{
    public MachineTrainer Trainer;
    public bool disableGravityOnGrab = true;

    private Interactable _interactable;
    private Rigidbody _rigidbody;
    private bool _isHeld = false;
    private bool _grabBlocked = false;

    public bool IsHeld => _isHeld;

    private void Awake()
    {
        _interactable = GetComponent<Interactable>();
        _rigidbody = GetComponent<Rigidbody>();

        if (Trainer == null)
            Trainer = FindObjectOfType<MachineTrainer>();
    }

    public void SetGrabBlocked(bool blocked)
    {
        _grabBlocked = blocked;
    }

    private void OnAttachedToHand(Hand hand)
    {
        if (_grabBlocked)
        {
            hand.DetachObject(gameObject);
            return;
        }

        _isHeld = true;

        if (_rigidbody != null)
        {
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
            _rigidbody.useGravity = !disableGravityOnGrab;
            _rigidbody.isKinematic = true;
        }
    }

    private void OnDetachedFromHand(Hand hand)
    {
        _isHeld = false;

        if (_rigidbody != null)
        {
            _rigidbody.isKinematic = false;
            _rigidbody.useGravity = true;
            _rigidbody.linearVelocity = Vector3.zero;
            _rigidbody.angularVelocity = Vector3.zero;
        }

        if (Trainer != null)
            Trainer.OnBlankReleased();
    }

    private void HandHoverUpdate(Hand hand)
    {
        if (_grabBlocked)
        {
            hand.HideGrabHint();
            return;
        }

        GrabTypes grabType = hand.GetGrabStarting(GrabTypes.None);
        if (grabType != GrabTypes.None && !_isHeld)
        {
            hand.AttachObject(gameObject, grabType, Hand.defaultAttachmentFlags);
        }
    }
}
