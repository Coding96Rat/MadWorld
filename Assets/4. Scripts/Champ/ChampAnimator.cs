using UnityEngine;
using UnityEngine.Rendering;

public class ChampAnimator : MonoBehaviour
{
    private Transform _modelParent;

    private Animator _animator;
    private int _moveAnimParameter;
    private bool _isFacingRight = true;


    private void Awake()
    {
        _animator = GetComponent<Animator>();
        _moveAnimParameter = Animator.StringToHash("Move");
    }

    private void Start()
    {
        _modelParent = transform.parent.transform;
    }


    public void FlipCharacter(float xDirection)
    {
        if (xDirection > 0 && !_isFacingRight)
        {
            _isFacingRight = true;
            _modelParent.localScale = new Vector3(_modelParent.localScale.x * -1f, _modelParent.localScale.y, _modelParent.localScale.z);
        }
        else if (xDirection < 0 && _isFacingRight)
        {
            _isFacingRight = false;
            _modelParent.localScale = new Vector3(_modelParent.localScale.x * -1f, _modelParent.localScale.y, _modelParent.localScale.z);
        }
    }

    public void MoveAnimation(bool isMoving)
    {
        _animator.SetBool(_moveAnimParameter, isMoving);
    }
}
