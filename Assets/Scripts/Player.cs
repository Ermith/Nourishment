using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;

public class Player : MonoBehaviour
{
    public int X = World.MAP_WIDTH / 2;
    public int Y = 0;
    public Camera Camera;

    private Tween playerTween;
    private Tween cameraTween;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Direction? movementDir = null;
        if (Input.GetKeyDown(KeyCode.W))
            movementDir = Direction.Up;
        if (Input.GetKeyDown(KeyCode.S))
            movementDir = Direction.Down;
        if (Input.GetKeyDown(KeyCode.A))
            movementDir = Direction.Left;
        if (Input.GetKeyDown(KeyCode.D))
            movementDir = Direction.Right;

        if (movementDir != null)
        {
            bool success = TryMove(movementDir.Value);
            if(success)
                Util.GetWorld().SimulationStep();
        }

        playerTween?.Kill();
        cameraTween?.Kill();

        playerTween = gameObject.transform.DOMove(new Vector3(X - World.MAP_WIDTH / 2 - 0.5f, Y - 0.5f, 0), 0.2f);
        cameraTween = Camera.transform.DOMoveY(Y - 0.5f, 0.4f);
    }

    bool TryMove(Direction direction)
    {
        var square = Util.GetWorld().GetSquare(X + direction.X(), Y + direction.Y());
        if (square == null)
            return false;
        if (!square.CanSpread(this))
            return false;
        X += direction.X();
        Y += direction.Y();
        return true;
    }
}
